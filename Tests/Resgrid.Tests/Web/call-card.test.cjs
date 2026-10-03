// Incident card renderer (A3) — the expanded active-calls row: dispatch time,
// type-ID, address, the units strip and the comment timeline inline.
//
// The fixture is a real call captured from the bravo instance (26-83,
// 2026-10-03), shaped exactly like DispatchController.GetCallCard returns it.
// The renderer is pure, so these cases pin the layout without a server.
const assert = require('node:assert/strict');
const path = require('node:path');
const { chromium } = require('./browser-launch.cjs').playwright();

const root = path.resolve(__dirname, '../../..');
const script = path.join(root, 'Web/Resgrid.Web/wwwroot/js/app/internal/dispatch/resgrid.dispatch.callcard.js');

const realCall = {
    CallId: 160,
    Number: '26-83',
    Name: 'CHESTPN-Chest Pain',
    Type: 'CHESTPN',
    Address: '7312 HIGHWAY 60 HAMILTON COUNTY, TN 37336',
    Priority: 'High',
    LoggedOn: '10/03/2026 10:43:04 AM',
    Units: [
        { UnitId: 20, Name: 'Rescue 2', State: 'Responding', StateColor: 'label-success' },
        { UnitId: 5, Name: 'Engine 42', State: 'On Scene', StateColor: '#e31e24' }
    ],
    Notes: [
        { Name: 'Tammy Adams', Timestamp: '10/03/2026 10:44:00 AM', Note: 'Dispatched' },
        { Name: 'Kyle Boran', Timestamp: '10/03/2026 10:46:12 AM', Note: 'En route' }
    ]
};

async function render(browser, card) {
    const page = await browser.newPage();
    await page.setContent('<div id="host"></div>');
    await page.addScriptTag({ path: script });
    await page.evaluate((data) => {
        document.getElementById('host').innerHTML = resgrid.dispatch.callcard.render(data, {});
    }, card);
    return page;
}

(async () => {
    const browser = await chromium.launch(require('./browser-launch.cjs').launchOptions());
    try {
        // 1. The real call renders every element the plan asks for.
        let page = await render(browser, realCall);
        const text = await page.locator('#host').innerText();
        assert.ok(text.includes('10/03/2026 10:43:04 AM'), 'dispatch time missing');
        assert.ok(text.includes('CHESTPN'), 'type-ID missing');
        assert.ok(text.includes('26-83'), 'call number missing');
        assert.ok(text.includes('7312 HIGHWAY 60'), 'address missing');
        assert.ok(text.includes('Rescue 2 — Responding'), 'unit chip missing');
        assert.ok(text.includes('Tammy Adams'), 'comment author missing');
        assert.ok(text.includes('Dispatched'), 'comment text missing');

        // The two color forms stay in their own lanes: a class is a class, a hex
        // is a style. Mixing them was the invisible-chip bug on the A2 panel.
        const classChip = await page.locator('#host .call-card-chip').first().getAttribute('class');
        const styleChip = await page.locator('#host .call-card-chip').nth(1).getAttribute('style');
        assert.ok(classChip.includes('label-success'), 'class color was not kept as a class');
        assert.ok(styleChip.includes('background-color:#e31e24'), 'hex color was not kept as a style');
        await page.close();

        // 2. A call with no comments says so.
        page = await render(browser, Object.assign({}, realCall, { Notes: [] }));
        assert.ok((await page.locator('#host').innerText()).includes('No comments yet.'), 'empty timeline note missing');
        await page.close();

        // 3. Comments are text, never markup.
        page = await render(browser, Object.assign({}, realCall, {
            Notes: [{ Name: 'X', Timestamp: 't', Note: '<script>window.__pwned = true;</script><b>bold</b>' }]
        }));
        assert.equal(await page.evaluate(() => window.__pwned === true), false, 'a note script executed');
        assert.equal(await page.locator('#host script').count(), 0, 'a note script was injected');
        assert.ok((await page.locator('#host').innerText()).includes('<script>'), 'a note no longer shows as text');
        await page.close();

        console.log('Incident card render, empty timeline and escaping passed.');
    } finally {
        await browser.close();
    }
})().catch((error) => { console.error(error); process.exit(1); });
