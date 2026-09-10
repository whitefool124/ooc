const { chromium } = require('C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const fs = require('node:fs');
const assert = require('node:assert/strict');
const task = path.resolve(__dirname, '..');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.goto(pathToFileURL(path.join(task, '视觉迭代评审.html')).href);
    await page.waitForFunction(() => [...document.images].every(i => i.complete && i.naturalWidth > 0));
    assert.equal(await page.locator('.cell').count(), 108);
    assert.equal(await page.locator('.water').count(), 5);
    assert.equal(await page.locator('.vine').count(), 10);
    assert.equal(await page.locator('.heavy').count(), 13);
    assert.equal(await page.locator('.light').count(), 3);
    assert.equal(await page.locator('.unit').count(), 3);
    await page.locator('[data-note="2"]').click();
    assert.match(await page.locator('#diagnosis-note').innerText(), /1536×1152/);
    await page.locator('[data-pos="C7"]').click();
    assert.match(await page.locator('#cell-rule').innerText(), /移除燃烧/);
    assert.equal(await page.locator('.cell .label').first().isVisible(), false);
    await page.locator('#labels').check();
    assert.equal(await page.locator('.cell .label').first().isVisible(), true);
    await page.locator('#labels').uncheck();
    await page.locator('#units').uncheck();
    assert.equal(await page.locator('.unit').first().isVisible(), false);
    await page.locator('#units').check();
    await page.locator('#terrain').uncheck();
    assert.equal(await page.locator('#board').evaluate(e => e.classList.contains('no-terrain')), true);
    await page.locator('#terrain').check();
    await page.locator('.phase').nth(3).click();
    assert.match(await page.locator('#phase-detail').innerText(), /多段效果不被覆盖/);
    await page.locator('#skip').click();
    assert.equal(await page.locator('#time-label').innerText(), '480ms');
    assert.match(await page.locator('#hit-text').innerText(), /18 → 13/);
    await page.locator('#reduced').check();
    assert.equal(await page.locator('.actor.on').count(), 0);
    await page.locator('#reduced').uncheck();
    await page.locator('#play').click();
    await page.waitForFunction(() => document.querySelector('#time-label').textContent === '480ms');
    const viewportChecks = [];
    for (const width of [1440, 960, 540]) {
      await page.setViewportSize({ width, height: 1000 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
      assert.equal(overflow, false, `Horizontal overflow at ${width}`);
      viewportChecks.push({ width, horizontalOverflow: overflow });
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: path.join(__dirname, '评审页预览.png') });
    await page.locator('#reading').screenshot({ path: path.join(__dirname, '层次示意预览.png') });
    assert.deepEqual(errors, []);
    const report = { status: 'PASS', browser: 'Headless Microsoft Edge via Playwright', viewportChecks,
      checks: ['historical screenshot loads', '108 cells and frozen terrain coordinates', 'diagnosis hotspots', 'cell descriptions', 'layer toggles', 'phase selection', 'skip/quiet/play timeline'],
      pageErrors: errors, limitation: 'Review HTML only; not a Unity runtime or art approval test.' };
    fs.writeFileSync(path.join(__dirname, 'review_validation.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify(report));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
