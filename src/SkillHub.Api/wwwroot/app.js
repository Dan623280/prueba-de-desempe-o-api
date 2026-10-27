"use strict";
const $ = (id) => document.getElementById(id);
let session;
try { session = JSON.parse(sessionStorage.getItem("skillhub-session") || "null"); } catch { session = null; }
let view = "browse", workshops = [], enrollments = [], registering = false, toastTimer;
const esc = (value) => String(value ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const date = (value) => new Intl.DateTimeFormat("en-US", { timeZone: "America/Bogota", month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
const time = (value) => new Intl.DateTimeFormat("en-US", { timeZone: "America/Bogota", hour: "numeric", minute: "2-digit" }).format(new Date(value));
function notify(message, error = false) {
  clearTimeout(toastTimer); $("toast").textContent = message; $("toast").className = error ? "toast error" : "toast";
  $("toast").hidden = false; toastTimer = setTimeout(() => $("toast").hidden = true, 6000);
}
function saveSession(value) {
  session = value;
  if (value) sessionStorage.setItem("skillhub-session", JSON.stringify(value)); else sessionStorage.removeItem("skillhub-session");
  $("signin").hidden = !!value; $("signout").hidden = !value;
  $("mine-tab").hidden = value?.user.role !== "Participant";
  $("account-name").textContent = value ? value.user.fullName : "";
}
async function api(path, options = {}, binary = false) {
  if (session && new Date(session.expiresAt) <= new Date()) saveSession(null);
  const headers = { ...options.headers };
  if (options.body) headers["Content-Type"] = "application/json";
  if (session) headers.Authorization = `Bearer ${session.accessToken}`;
  const response = await fetch(`/api/v1${path}`, { ...options, headers });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    if (response.status === 401) saveSession(null);
    const details = error.errors ? Object.values(error.errors).flat().join(" ") : error.title;
    throw new Error(details || `The request failed (${response.status}).`);
  }
  return binary ? response.blob() : response.json();
}
function workshopCard(w, enrollment) {
  const code = w.code.split("-")[0];
  const symbols = { FOT: "◎", COC: "◒", ORA: "❝", DIS: "✳", TEC: "⌘" };
  const active = enrollment?.status === "Active";
  const endDate = date(w.startTime) === date(w.endTime) ? "" : `${date(w.endTime)} · `;
  const canCancel = active && new Date(w.startTime) - Date.now() >= 86400000;
  return `<article class="card"><div class="card-art"><span class="category">${esc(w.instructor.specialty.toUpperCase())}</span><span class="card-symbol" aria-hidden="true">${symbols[code] || "✳"}</span></div><div class="card-body">${enrollment ? `<span class="status ${active ? "" : "cancelled"}">${esc(enrollment.status)} · enrolled ${esc(date(enrollment.enrolledAt))}</span>` : ""}<h3>${esc(w.title)}</h3><p class="description">${esc(w.description)}</p><p class="meta"><span aria-hidden="true">◷</span>${esc(date(w.startTime))} · ${esc(time(w.startTime))} – ${esc(endDate)}${esc(time(w.endTime))}</p><p class="meta">${esc(w.code)} · In person</p><p class="instructor">With ${esc(w.instructor.fullName)}</p><div class="card-bottom">${enrollment ? `<div class="enrollment-actions">${active ? `<button class="button" data-action="certificate" data-id="${esc(enrollment.id)}">Download PDF ↓</button>${canCancel ? `<button class="button secondary" data-action="cancel" data-id="${esc(enrollment.id)}">Cancel</button>` : `<span class="seats">Cancellation deadline passed</span>`}` : `<span class="seats">${enrollment.cancelledAt ? `Cancelled ${esc(date(enrollment.cancelledAt))}` : "Cancelled"}</span>`}</div>` : `<span class="seats"><b>${w.availableSeats}</b> / ${w.capacity} seats left</span><button class="button secondary" data-action="enroll" data-id="${esc(w.id)}" ${w.availableSeats <= 0 ? "disabled" : ""}>${w.availableSeats <= 0 ? "Full" : "Save my seat ↗"}</button>`}</div></div></article>`;
}
function render() {
  const mine = view === "mine";
  $("hero").hidden = mine; $("search-label").hidden = mine;
  $("section-title").innerHTML = mine ? "Your learning journey<span>.</span>" : "Upcoming workshops<span>.</span>";
  $("section-note").textContent = mine ? "Your seats, your schedule. Download a certificate or manage an enrollment here." : "In-person sessions. Experienced instructors. A seat with your name on it.";
  $("browse-tab").classList.toggle("active", !mine); $("mine-tab").classList.toggle("active", mine);
  const query = $("search").value.toLowerCase();
  const items = mine ? enrollments : workshops.filter(w => `${w.title} ${w.code} ${w.instructor.fullName} ${w.instructor.specialty}`.toLowerCase().includes(query));
  $("results-count").textContent = `${items.length} ${mine ? "enrollment" : "workshop"}${items.length === 1 ? "" : "s"}`;
  $("content").innerHTML = items.length ? items.map(item => mine ? workshopCard(item.workshop, item) : workshopCard(item)).join("") : `<div class="empty">${mine ? "Your next skill is waiting. Explore the workshops and save your first seat." : "No upcoming workshops match your search. Check back soon for new sessions."}</div>`;
}
async function load() {
  $("content").innerHTML = '<div class="empty">Loading…</div>';
  try {
    if (view === "mine") enrollments = await api("/enrollments/mine"); else workshops = await api("/workshops");
    render();
  } catch (error) {
    $("content").innerHTML = '<div class="empty">Unable to load this page. Please try again.</div>';
    notify(error.message, true);
  }
}
function openAuth() { $("auth-error").textContent = ""; $("auth-dialog").showModal(); }
$("signin").onclick = openAuth;
$("close-dialog").onclick = () => $("auth-dialog").close();
$("signout").onclick = () => { saveSession(null); view = "browse"; load(); notify("You have signed out."); };
$("browse-tab").onclick = () => { view = "browse"; load(); };
$("mine-tab").onclick = () => { view = "mine"; load(); };
$("search").oninput = render;
$("switch-auth").onclick = () => {
  registering = !registering; $("fullname-label").hidden = !registering; $("fullname").required = registering;
  $("password-help").hidden = !registering; $("password").minLength = registering ? 10 : 1;
  $("password").autocomplete = registering ? "new-password" : "current-password";
  $("auth-title").textContent = registering ? "Stay curious." : "Welcome back.";
  $("auth-subtitle").textContent = registering ? "Create your participant account." : "Sign in to save your seat.";
  $("auth-submit").textContent = registering ? "Create account ↗" : "Sign in ↗";
  $("switch-auth").textContent = registering ? "Already registered? Sign in" : "New here? Create an account";
  $("auth-error").textContent = "";
};
$("auth-form").onsubmit = async event => {
  event.preventDefault(); $("auth-submit").disabled = true; $("auth-error").textContent = "";
  try {
    const payload = { email: $("email").value, password: $("password").value };
    if (registering) payload.fullName = $("fullname").value;
    saveSession(await api(registering ? "/auth/register" : "/auth/login", { method: "POST", body: JSON.stringify(payload) }));
    $("auth-dialog").close(); $("password").value = ""; view = "browse"; await load();
    notify(session.user.role === "Employee" ? "Signed in. Employee operations are available in Swagger." : "Welcome! Find your next workshop.");
  } catch (error) { $("auth-error").textContent = error.message; }
  finally { $("auth-submit").disabled = false; }
};
$("content").onclick = async event => {
  const button = event.target.closest("button[data-action]"); if (!button) return;
  const { action, id } = button.dataset;
  if (!session) { openAuth(); return; }
  if (action === "enroll" && session.user.role !== "Participant") { notify("Employee operations are available in Swagger. Use a participant account to enroll.", true); return; }
  if (action === "cancel" && !confirm("Cancel this enrollment and release your seat?")) return;
  button.disabled = true;
  try {
    if (action === "certificate") {
      const blob = await api(`/enrollments/${id}/certificate`, {}, true);
      const url = URL.createObjectURL(blob), anchor = document.createElement("a");
      anchor.href = url; anchor.download = `SkillHub-enrollment-${id}.pdf`; anchor.click(); setTimeout(() => URL.revokeObjectURL(url), 10000);
    } else if (action === "cancel") {
      await api(`/enrollments/${id}/cancel`, { method: "PATCH" }); await load(); notify("Enrollment cancelled. Your seat has been released.");
    } else {
      await api("/enrollments", { method: "POST", body: JSON.stringify({ workshopId: id }) }); await load(); notify("Your seat is saved! Find the certificate in My enrollments.");
    }
  } catch (error) { notify(error.message, true); }
  finally { button.disabled = false; }
};
saveSession(session); load();
