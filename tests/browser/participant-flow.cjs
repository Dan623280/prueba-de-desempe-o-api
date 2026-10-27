// Runs against the disposable integration API; never uses the main assessment database.
const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, ...(process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {}) });
  try {
    const base = process.env.SKILLHUB_URL || 'http://localhost:5081';
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, acceptDownloads: true });
    const page = await context.newPage();
    const pageErrors = [];
    page.on('pageerror', error => pageErrors.push(error.message));
    const authResponse = await context.request.post(base + '/api/v1/auth/login', { data: { email: 'andres.mejia@correo.com', password: 'Instructor2026*' } });
    assert.equal(authResponse.status(), 200);
    const auth = await authResponse.json();
    const title = 'Browser workshop ' + Date.now();
    const created = await context.request.post(base + '/api/v1/workshops', { headers: { Authorization: 'Bearer ' + auth.accessToken }, data: {
      code: 'UI-' + Date.now(), title, description: 'A two-day photography workshop.', instructorEmail: 'andres.mejia@correo.com',
      startTime: '2032-12-01T23:00:00-05:00', endTime: '2032-12-02T01:00:00-05:00', capacity: 3
    } });
    assert.equal(created.status(), 201);
    await page.goto(base);
    await page.locator('.card').first().waitFor();
    await page.screenshot({ path: '/tmp/skillhub-desktop.png', fullPage: true });
    await page.locator('#signin').click();
    await page.locator('#switch-auth').click();
    await page.locator('#fullname').fill('Browser Participant');
    const email = `browser-${Date.now()}@skillhub.test`;
    await page.locator('#email').fill(email);
    await page.locator('#password').fill('Browser2026*');
    await page.locator('#auth-submit').click();
    await page.locator('#auth-dialog').waitFor({ state: 'hidden' });
    await page.waitForFunction(() => document.getElementById('account-name').textContent === 'Browser Participant');
    await page.locator('#search').fill(title);
    await page.waitForFunction(() => document.querySelectorAll('.card').length === 1);
    const schedule = await page.locator('.meta').first().innerText();
    await page.getByRole('button', { name: 'Save my seat' }).click();
    await page.waitForFunction(() => document.getElementById('toast').textContent.includes('Your seat is saved'));
    await page.locator('#mine-tab').click();
    await page.getByRole('button', { name: 'Download PDF' }).waitFor();
    const downloadEvent = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Download PDF' }).click();
    const download = await downloadEvent;
    assert.match(download.suggestedFilename(), /\.pdf$/);
    assert.equal(await download.failure(), null);
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.locator('.status.cancelled').waitFor();
    await page.locator('#signout').click();
    await page.locator('#signin').waitFor({ state: 'visible' });
    await page.locator('#signin').click();
    if (await page.locator('#fullname-label').isVisible()) await page.locator('#switch-auth').click();
    await page.locator('#email').fill(email);
    await page.locator('#password').fill('Browser2026*');
    await page.locator('#auth-submit').click();
    await page.locator('#auth-dialog').waitFor({ state: 'hidden' });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({ path: '/tmp/skillhub-mobile.png', fullPage: true });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true, 'Mobile layout must not overflow');
    assert.deepEqual(pageErrors, [], 'Client must not produce JavaScript errors');
    assert.match(schedule, /Dec 1, 2032/);
    assert.match(schedule, /Dec 2, 2032/, 'Overnight workshops must show their end date');
    console.log('Browser participant flow passed: register, enroll, PDF, cancel, sign out, sign in, mobile layout and overnight dates.');
    await context.close();
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
