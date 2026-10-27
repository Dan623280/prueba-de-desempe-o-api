"""End-to-end checks against a running SkillHub API and a disposable PostgreSQL database."""
import concurrent.futures
import datetime as dt
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import threading
import time
import unittest
import urllib.request
import urllib.error
import uuid
import zipfile
from xml.sax.saxutils import escape

BASE = os.environ.get("SKILLHUB_URL", "http://localhost:5081")
ROOT = Path(__file__).resolve().parents[1]
PASSWORD = "Testing2026*"
INSTRUCTOR = "andres.mejia@correo.com"


def send(method, path, data=None, token=None, raw=None, content_type=None):
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    if data is not None:
        raw = json.dumps(data).encode()
        content_type = "application/json"
    if content_type:
        headers["Content-Type"] = content_type
    req = urllib.request.Request(BASE + path, data=raw, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            return response.status, response.read(), response.headers
    except urllib.error.HTTPError as error:
        return error.code, error.read(), error.headers


def request(method, path, data=None, token=None, expected=200):
    status, body, _ = send(method, path, data, token)
    assert status == expected, (method, path, status, body.decode(errors="replace"))
    return json.loads(body) if body else None


def participant():
    return request("POST", "/api/v1/auth/register", {
        "fullName": "Integration Participant", "email": f"it-{uuid.uuid4().hex}@skillhub.test", "password": PASSWORD,
        "role": "Employee"  # An untrusted client must never grant itself employee access.
    }, expected=201)


def workbook(rows, headers=("ParticipantEmail", "WorkshopCode")):
    def row_xml(number, values):
        cells = "".join(f'<c r="{chr(65 + i)}{number}" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>' for i, value in enumerate(values))
        return f'<row r="{number}">{cells}</row>'
    sheet = '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
    sheet += row_xml(1, headers)
    sheet += "".join(row_xml(index, row) for index, row in enumerate(rows, 2))
    sheet += '</sheetData></worksheet>'
    output = io.BytesIO()
    with zipfile.ZipFile(ROOT / "data/SkillHub_Data.xlsx") as source, zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as target:
        for item in source.infolist():
            target.writestr(item, sheet.encode() if item.filename == "xl/worksheets/sheet4.xml" else source.read(item.filename))
    return output.getvalue()


def upload(content, token, expected=200):
    boundary = "SkillHub" + uuid.uuid4().hex
    body = (f'--{boundary}\r\nContent-Disposition: form-data; name="File"; filename="enrollments.xlsx"\r\nContent-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n'.encode()
            + content + f'\r\n--{boundary}--\r\n'.encode())
    status, data, _ = send("POST", "/api/v1/enrollments/import", token=token, raw=body, content_type="multipart/form-data; boundary=" + boundary)
    assert status == expected, (status, data.decode(errors="replace"))
    return json.loads(data)


class ApiTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.employee = request("POST", "/api/v1/auth/login", {"email": INSTRUCTOR, "password": "Instructor2026*"})["accessToken"]

    def workshop(self, **overrides):
        start = dt.datetime.now(dt.timezone.utc) + dt.timedelta(days=10)
        data = {"code": "IT-" + uuid.uuid4().hex[:15], "title": "Integration photography", "description": "Practice light and composition.",
                "instructorEmail": INSTRUCTOR, "startTime": start.isoformat(), "endTime": (start + dt.timedelta(hours=2)).isoformat(), "capacity": 5}
        data.update(overrides)
        return request("POST", "/api/v1/workshops", data, self.employee, expected=201)

    def enroll(self, user, workshop, expected=201):
        return request("POST", "/api/v1/enrollments", {"workshopId": workshop["id"]}, user["accessToken"], expected=expected)

    def race(self, calls):
        barrier = threading.Barrier(len(calls))
        def run(call):
            barrier.wait()
            return call()
        with concurrent.futures.ThreadPoolExecutor(max_workers=len(calls)) as executor:
            return list(executor.map(run, calls))

    def test_01_public_catalog_and_protected_routes(self):
        catalog = request("GET", "/api/v1/workshops")
        self.assertGreaterEqual(len(catalog), 15)
        for w in catalog:
            self.assertEqual(w["availableSeats"], w["capacity"] - w["activeEnrollments"])
            self.assertGreater(dt.datetime.fromisoformat(w["startTime"]), dt.datetime.now(dt.timezone.utc))
        for method, path in [("GET", "/api/v1/enrollments/mine"), ("GET", "/api/v1/reports/top-workshops"),
                             ("POST", "/api/v1/workshops"), ("POST", "/api/v1/enrollments"),
                             ("POST", "/api/v1/enrollments/import"), ("GET", "/api/v1/instructors"),
                             ("PATCH", f"/api/v1/enrollments/{uuid.uuid4()}/cancel"),
                             ("GET", f"/api/v1/enrollments/{uuid.uuid4()}/certificate")]:
            request(method, path, expected=401)
        self.assertEqual(len(request("GET", "/api/v1/instructors", token=self.employee)), 6)

    def test_registration_ignores_employee_role_and_restricts_access(self):
        p = participant()
        self.assertEqual(p["user"]["role"], "Participant")
        token = p["accessToken"]
        request("POST", "/api/v1/workshops", {}, token, expected=403)
        request("GET", "/api/v1/instructors", token=token, expected=403)
        request("GET", "/api/v1/reports/top-workshops", token=token, expected=403)
        request("POST", "/api/v1/enrollments/import", token=token, expected=403)
        request("POST", "/api/v1/auth/register", {"fullName": "Duplicate", "email": p["user"]["email"].upper(), "password": PASSWORD}, expected=409)
        request("POST", "/api/v1/auth/login", {"email": p["user"]["email"], "password": "wrong"}, expected=401)
        result = request("POST", "/api/v1/auth/login", {"email": p["user"]["email"].upper(), "password": PASSWORD})
        self.assertEqual(result["user"]["id"], p["user"]["id"])
        request("POST", "/api/v1/enrollments", {"workshopId": str(uuid.uuid4())}, self.employee, expected=403)
        request("GET", "/api/v1/enrollments/mine", token=self.employee, expected=403)

    def test_invalid_registration_and_tampered_token(self):
        request("POST", "/api/v1/auth/register", {"fullName": " ", "email": "invalid", "password": "short"}, expected=400)
        request("POST", "/api/v1/auth/register", {"fullName": "Test User", "email": f"{uuid.uuid4().hex}@skillhub.test", "password": "lowercaseonly"}, expected=400)
        p = participant()
        parts = p["accessToken"].split(".")
        parts[2] = ("a" if parts[2][0] != "a" else "b") + parts[2][1:]
        request("GET", "/api/v1/enrollments/mine", token=".".join(parts), expected=401)
        request("GET", "/api/v1/enrollments/mine", token="not-a-token", expected=401)

    def test_workshop_validation_and_unique_code(self):
        w = self.workshop()
        now = dt.datetime.now(dt.timezone.utc)
        base = {"code": "IT-" + uuid.uuid4().hex[:12], "title": "Title", "description": "Description", "instructorEmail": INSTRUCTOR,
                "startTime": (now + dt.timedelta(days=5)).isoformat(), "endTime": (now + dt.timedelta(days=5, hours=1)).isoformat(), "capacity": 2}
        request("POST", "/api/v1/workshops", dict(base, code=w["code"].lower()), self.employee, expected=409)
        request("POST", "/api/v1/workshops", dict(base, instructorEmail="missing@skillhub.test"), self.employee, expected=400)
        request("POST", "/api/v1/workshops", dict(base, startTime=(now - dt.timedelta(hours=1)).isoformat()), self.employee, expected=409)
        request("POST", "/api/v1/workshops", dict(base, endTime=base["startTime"]), self.employee, expected=409)
        request("POST", "/api/v1/workshops", dict(base, capacity=0), self.employee, expected=400)
        request("GET", f'/api/v1/workshops/{uuid.uuid4()}', expected=404)

    def test_enrollment_ownership_cancellation_reenrollment_and_certificate(self):
        owner, other = participant(), participant()
        w = self.workshop(capacity=1)
        enrollment = self.enroll(owner, w)
        eid = enrollment["id"]
        self.assertEqual(enrollment["status"], "Active")
        self.assertEqual(request("GET", f'/api/v1/workshops/{w["id"]}')["availableSeats"], 0)
        self.enroll(owner, w, expected=409)
        self.enroll(other, w, expected=409)
        request("PATCH", f"/api/v1/enrollments/{eid}/cancel", token=other["accessToken"], expected=403)
        request("GET", f"/api/v1/enrollments/{eid}/certificate", token=other["accessToken"], expected=403)
        request("GET", f'/api/v1/workshops/{w["id"]}/enrollments', token=owner["accessToken"], expected=403)
        self.assertEqual(request("GET", "/api/v1/enrollments/mine", token=other["accessToken"]), [])
        mine = request("GET", "/api/v1/enrollments/mine", token=owner["accessToken"])
        self.assertEqual(mine[0]["id"], eid)
        for token in [owner["accessToken"], self.employee]:
            status, pdf, headers = send("GET", f"/api/v1/enrollments/{eid}/certificate", token=token)
            self.assertEqual(status, 200)
            self.assertEqual(headers.get_content_type(), "application/pdf")
            self.assertTrue(pdf.startswith(b"%PDF-"))
            with tempfile.NamedTemporaryFile(suffix=".pdf") as file:
                file.write(pdf); file.flush()
                text = subprocess.check_output(["pdftotext", file.name, "-"]).decode()
                for value in ["SkillHub", "Constancia de inscripción", owner["user"]["fullName"], owner["user"]["email"], w["code"], "Andrés", "Enrollment date:", "Document generated:"]:
                    self.assertIn(value, text)
        cancelled = request("PATCH", f"/api/v1/enrollments/{eid}/cancel", token=owner["accessToken"])
        self.assertEqual(cancelled["cancelledById"], owner["user"]["id"])
        self.assertIsNotNone(cancelled["cancelledAt"])
        self.assertEqual(cancelled["status"], "Cancelled")
        request("PATCH", f"/api/v1/enrollments/{eid}/cancel", token=owner["accessToken"], expected=409)
        request("GET", f"/api/v1/enrollments/{eid}/certificate", token=owner["accessToken"], expected=409)
        self.assertEqual(request("GET", f'/api/v1/workshops/{w["id"]}')["availableSeats"], 1)
        again = self.enroll(owner, w)
        self.assertNotEqual(again["id"], eid)
        listed = request("GET", f'/api/v1/workshops/{w["id"]}/enrollments', token=self.employee)
        self.assertEqual(len(listed), 2)

    def test_schedule_overlap_and_adjacent_workshops(self):
        p = participant()
        start = dt.datetime.now(dt.timezone.utc) + dt.timedelta(days=20)
        a = self.workshop(startTime=start.isoformat(), endTime=(start + dt.timedelta(hours=2)).isoformat())
        b = self.workshop(startTime=(start + dt.timedelta(hours=1)).isoformat(), endTime=(start + dt.timedelta(hours=3)).isoformat())
        c = self.workshop(startTime=a["endTime"], endTime=(start + dt.timedelta(hours=4)).isoformat())
        e = self.enroll(p, a)
        self.enroll(p, b, expected=409)
        self.enroll(p, c)
        request("PATCH", f'/api/v1/enrollments/{e["id"]}/cancel', token=p["accessToken"])
        # Workshop b still overlaps c: cancellation only releases its own reservation.
        self.enroll(p, b, expected=409)

    def test_employee_cancellation_override_and_started_workshop(self):
        p = participant()
        start = dt.datetime.now(dt.timezone.utc) + dt.timedelta(hours=2)
        w = self.workshop(startTime=start.isoformat(), endTime=(start + dt.timedelta(hours=1)).isoformat())
        e = self.enroll(p, w)
        request("PATCH", f'/api/v1/enrollments/{e["id"]}/cancel', token=p["accessToken"], expected=409)
        cancelled = request("PATCH", f'/api/v1/enrollments/{e["id"]}/cancel', token=self.employee)
        self.assertNotEqual(cancelled["cancelledById"], p["user"]["id"])
        start = dt.datetime.now(dt.timezone.utc) + dt.timedelta(seconds=3)
        started = self.workshop(startTime=start.isoformat(), endTime=(start + dt.timedelta(hours=1)).isoformat())
        current = self.enroll(p, started)
        time.sleep(max(0, (start - dt.datetime.now(dt.timezone.utc)).total_seconds()) + 0.05)
        self.enroll(participant(), started, expected=409)
        request("PATCH", f'/api/v1/enrollments/{current["id"]}/cancel', token=self.employee, expected=409)
        ids = [x["id"] for x in request("GET", "/api/v1/workshops")]
        self.assertNotIn(started["id"], ids)

    def test_concurrent_requests_cannot_overbook(self):
        w = self.workshop(capacity=1)
        people = [participant() for _ in range(4)]
        results = self.race([lambda p=p: send("POST", "/api/v1/enrollments", {"workshopId": w["id"]}, p["accessToken"])[0] for p in people])
        self.assertEqual(sorted(results), [201, 409, 409, 409])
        self.assertEqual(request("GET", f'/api/v1/workshops/{w["id"]}')["activeEnrollments"], 1)

    def test_concurrent_requests_cannot_duplicate_or_cross_schedules(self):
        p = participant()
        w = self.workshop()
        results = self.race([lambda: send("POST", "/api/v1/enrollments", {"workshopId": w["id"]}, p["accessToken"])[0]] * 3)
        self.assertEqual(sorted(results), [201, 409, 409])
        p = participant()
        a = self.workshop()
        b = self.workshop(startTime=a["startTime"], endTime=a["endTime"])
        results = self.race([lambda w=w: send("POST", "/api/v1/enrollments", {"workshopId": w["id"]}, p["accessToken"])[0] for w in [a, b]])
        self.assertEqual(sorted(results), [201, 409])

    def test_excel_partial_success_physical_rows_and_reenrollment(self):
        p, second = participant(), participant()
        w = self.workshop(capacity=2)
        rows = [(p["user"]["email"], w["code"]), ("", ""), (p["user"]["email"], w["code"]),
                ("missing@skillhub.test", w["code"]), (second["user"]["email"], "MISSING"),
                (second["user"]["email"].upper(), w["code"].lower())]
        result = upload(workbook(rows), self.employee)
        self.assertEqual((result["totalRows"], result["enrolledRows"], result["rejectedRows"]), (5, 2, 3))
        self.assertEqual([r["rowNumber"] for r in result["rejections"]], [4, 5, 6])
        self.assertTrue(all(r["reason"] for r in result["rejections"]))
        self.assertEqual(request("GET", f'/api/v1/workshops/{w["id"]}')["availableSeats"], 0)

    def test_excel_invalid_files_headers_empty_and_limits(self):
        upload(b"not-an-excel-file", self.employee, expected=400)
        upload(workbook([], headers=("Email", "Code")), self.employee, expected=400)
        upload(workbook([]), self.employee, expected=400)
        upload(workbook([("a@b.com", "X")] * 5001), self.employee, expected=400)
        upload(b"X" * (5 * 1024 * 1024 + 1), self.employee, expected=400)

    def test_excel_formula_row_is_rejected_without_evaluation_and_other_rows_commit(self):
        first, second = participant(), participant()
        w = self.workshop()
        original = workbook([(first["user"]["email"], w["code"]), (second["user"]["email"], w["code"])])
        output = io.BytesIO()
        with zipfile.ZipFile(io.BytesIO(original)) as source, zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as target:
            for item in source.infolist():
                data = source.read(item.filename)
                if item.filename == "xl/worksheets/sheet4.xml":
                    text = data.decode()
                    old = f'<c r="A2" t="inlineStr"><is><t>{first["user"]["email"]}</t></is></c>'
                    text = text.replace(old, '<c r="A2"><f>A2</f></c>')
                    data = text.encode()
                target.writestr(item, data)
        result = upload(output.getvalue(), self.employee)
        self.assertEqual((result["enrolledRows"], result["rejectedRows"]), (1, 1))
        self.assertEqual(result["rejections"][0]["rowNumber"], 2)
        self.assertIn("formula", result["rejections"][0]["reason"].lower())

    def test_pdf_wraps_multiline_titles_without_overlapping_fields(self):
        p = participant()
        title = "\n".join(f"TITLELINE{i:02}" for i in range(1, 17))
        w = self.workshop(title=title)
        e = self.enroll(p, w)
        status, pdf, _ = send("GET", f'/api/v1/enrollments/{e["id"]}/certificate', token=p["accessToken"])
        self.assertEqual(status, 200)
        with tempfile.NamedTemporaryFile(suffix=".pdf") as file:
            file.write(pdf); file.flush()
            text = subprocess.check_output(["pdftotext", "-layout", file.name, "-"]).decode()
            for i in range(1, 17):
                self.assertIn(f"TITLELINE{i:02}", text)
            lines = text.splitlines()
            # No field can occupy a line containing workshop title content.
            self.assertFalse(any("TITLELINE" in line and "Instructor:" in line for line in lines))
            self.assertGreater(text.index("Instructor:"), text.index("TITLELINE16"))

    def test_pdf_preserves_maximum_length_names_and_emails(self):
        name = "W" * 150
        email = "a" * 64 + "@" + "b" * 63 + "." + "c" * 63 + "." + "d" * 57 + ".com"
        self.assertEqual(len(email), 254)
        p = request("POST", "/api/v1/auth/register", {"fullName": name, "email": email, "password": PASSWORD}, expected=201)
        w = self.workshop()
        e = self.enroll(p, w)
        status, pdf, _ = send("GET", f'/api/v1/enrollments/{e["id"]}/certificate', token=p["accessToken"])
        self.assertEqual(status, 200)
        with tempfile.NamedTemporaryFile(suffix=".pdf") as file:
            file.write(pdf); file.flush()
            text = subprocess.check_output(["pdftotext", file.name, "-"]).decode()
            normalized = "".join(text.split())
            self.assertIn(name, normalized)
            self.assertIn(email, normalized)

    def test_top_workshops_report_and_seed_login(self):
        report = request("GET", "/api/v1/reports/top-workshops", token=self.employee)
        counts = [row["activeEnrollments"] for row in report]
        self.assertEqual(counts, sorted(counts, reverse=True))
        for row in report:
            self.assertEqual(row["availableSeats"], row["capacity"] - row["activeEnrollments"])
        seeded = request("POST", "/api/v1/auth/login", {"email": "jorge.perez@correo.com", "password": "Skillhub2026*"})
        self.assertEqual(seeded["user"]["role"], "Participant")
        # Pending Excel enrollment rows must not be imported at startup.
        self.assertEqual(request("GET", "/api/v1/enrollments/mine", token=seeded["accessToken"]), [])

    def test_swagger_and_participant_client(self):
        status, body, _ = send("GET", "/swagger/v1/swagger.json")
        self.assertEqual(status, 200)
        spec = json.loads(body)
        self.assertIn("Bearer", spec["components"]["securitySchemes"])
        self.assertIn("/api/v1/enrollments/import", spec["paths"])
        self.assertEqual(spec["paths"]["/api/v1/workshops"]["get"].get("security", []), [])
        for path in ["/", "/app.js", "/styles.css", "/swagger/index.html"]:
            status, body, _ = send("GET", path)
            self.assertEqual(status, 200, path)
            self.assertGreater(len(body), 100)


if __name__ == "__main__":
    unittest.main(verbosity=2)
