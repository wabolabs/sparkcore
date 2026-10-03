// Station strips (A5/A6) — apparatus in/out of service, weather alerts and
// upcoming events. Pure render functions, pinned against fixtures shaped like
// Dispatch/GetStationStrips returns.
const assert = require('node:assert/strict');
const path = require('node:path');
const { chromium } = require('./browser-launch.cjs').playwright();

const root = path.resolve(__dirname, '../../..');
const script = path.join(root, 'Web/Resgrid.Web/wwwroot/js/app/internal/dispatch/resgrid.dispatch.strips.js');

async function render(browser, call) {
    const page = await browser.newPage();
    await page.setContent('<div id="host"></div>');
    await page.addScriptTag({ path: script });
    await page.evaluate((expr) => {
        document.getElementById('host').innerHTML = eval(expr);
    }, call);
    return page;
}

(async () => {
    const browser = await chromium.launch(require('./browser-launch.cjs').launchOptions());
    try {
        // 1. Apparatus: the two counts and the named out-of-service unit.
        let page = await render(browser,
            "resgrid.dispatch.strips.renderApparatus({ Total: 29, OutOfService: 1, InService: 28, OutOfServiceUnits: ['Brush 97'] }, {})");
        let text = await page.locator('#host').innerText();
        assert.ok(text.includes('28') && text.includes('in service'), 'in-service count missing');
        assert.ok(text.includes('1') && text.includes('out of service'), 'out-of-service count missing');
        assert.ok(text.includes('Brush 97'), 'out-of-service unit name missing');
        await page.close();

        // 2. Weather: severity, event and headline; empty says so.
        page = await render(browser,
            "resgrid.dispatch.strips.renderWeather({ Alerts: [{ Event: 'Flood Watch', Severity: 'Moderate', Headline: 'in effect until 10 PM' }] }, {})");
        text = await page.locator('#host').innerText();
        assert.ok(text.includes('Moderate'), 'severity missing');
        assert.ok(text.includes('Flood Watch'), 'event missing');
        assert.ok(text.includes('in effect until 10 PM'), 'headline missing');
        await page.close();

        page = await render(browser, "resgrid.dispatch.strips.renderWeather({ Alerts: [] }, {})");
        assert.ok((await page.locator('#host').innerText()).includes('No active alerts'), 'empty weather note missing');
        await page.close();

        // 3. Events: title and local start; empty says so.
        page = await render(browser,
            "resgrid.dispatch.strips.renderEvents({ Items: [{ Title: 'Training night', Start: '10/05/2026 7:00:00 PM', End: '10/05/2026 9:00:00 PM', AllDay: false }] }, {})");
        text = await page.locator('#host').innerText();
        assert.ok(text.includes('Training night'), 'event title missing');
        assert.ok(text.includes('10/05/2026 7:00:00 PM'), 'event start missing');
        await page.close();

        page = await render(browser, "resgrid.dispatch.strips.renderEvents({ Items: [] }, {})");
        assert.ok((await page.locator('#host').innerText()).includes('No upcoming events'), 'empty events note missing');
        await page.close();

        // 4. Strip content is text, never markup.
        page = await render(browser,
            "resgrid.dispatch.strips.renderWeather({ Alerts: [{ Event: 'X', Severity: 'Severe', Headline: '<script>window.__pwned = true;</script>' }] }, {})");
        assert.equal(await page.evaluate(() => window.__pwned === true), false, 'a headline script executed');
        assert.equal(await page.locator('#host script').count(), 0, 'a headline script was injected');
        assert.ok((await page.locator('#host').innerText()).includes('<script>'), 'headline no longer shows as text');
        await page.close();

        console.log('Apparatus, weather and events strips (and escaping) passed.');
    } finally {
        await browser.close();
    }
})().catch((error) => { console.error(error); process.exit(1); });
