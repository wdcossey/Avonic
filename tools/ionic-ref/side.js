// Puts two frames side by side in one image, so that Ionic and Avonic can be looked at together.
// Usage: node tools/ionic-ref/side.js <left.png> <right.png> <out.png> [height, default 800]
const fs = require('fs');
const { chromium } = require('playwright-core');

(async () => {
    const [left, right, out, height = '800'] = process.argv.slice(2);
    if (!left || !right || !out) {
        console.error('Usage: node tools/ionic-ref/side.js <left.png> <right.png> <out.png> [height]');
        process.exit(1);
    }

    const image = (file) => `<img style="height:${height}px" src="data:image/png;base64,${fs.readFileSync(file).toString('base64')}">`;
    const browser = await chromium.launch();
    // Wide enough for any pair of phone frames. `fullPage` is for anything wider, which would otherwise lose the right one.
    const page = await browser.newPage({ viewport: { width: 1600, height: Number(height) } });
    await page.setContent(`<body style="margin:0;background:#888;display:inline-flex;gap:8px">${image(left)}${image(right)}</body>`);
    await page.waitForTimeout(200);
    await page.screenshot({ path: out, fullPage: true });
    await browser.close();
})();
