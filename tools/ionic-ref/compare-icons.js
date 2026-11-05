#!/usr/bin/env node
// Compares every ion-icon against real Ionic, pixel by pixel. This is how a bug in StreamGeometry.Parse
// was found, so re-run it after upgrading ionicons or changing how icons paint.
//
//   $env:AVONIC_CAPTURE_DIR = "<dir>"; dotnet test Avonic.sln --filter Every_Icon_Paints
//   node tools/ionic-ref/compare-icons.js <dir>
//
// The test writes <dir>/icons-avonic.png. This renders the same grid in Chromium and writes
// <dir>/icons-chromium.png and <dir>/icons-worst.png, the 40 worst pairs with Chromium on the left.
// The layout has to match AvonicIconCaptureTests.

const path = require('path');
const fs = require('fs');
const http = require('http');
const { chromium } = require('playwright-core');

const ROOT = path.resolve(__dirname, '..', '..');
const dir = process.argv[2];
if (!dir) {
    console.error('usage: node tools/ionic-ref/compare-icons.js <capture dir>');
    process.exit(1);
}

const COLS = 40, CELL = 40, SIZE = 32, PAD = 4, SCALE = 2;
const names = fs.readdirSync(path.join(ROOT, 'node_modules/ionicons/dist/svg'))
    .filter(file => file.endsWith('.svg')).map(file => file.slice(0, -4)).sort();
const WIDTH = COLS * CELL, HEIGHT = Math.ceil(names.length / COLS) * CELL;

const cells = names.map((name, i) =>
    `<ion-icon name="${name}" style="position:absolute;left:${(i % COLS) * CELL + PAD}px;top:${Math.floor(i / COLS) * CELL + PAD}px;font-size:${SIZE}px"></ion-icon>`).join('');
const html = `<!doctype html><html mode="md"><head><meta charset="utf-8">
<link rel="stylesheet" href="/node_modules/@ionic/core/css/ionic.bundle.css">
<script>window.Ionic={config:{mode:'md'}};</script>
<script type="module" src="/node_modules/@ionic/core/dist/ionic/ionic.esm.js"></script>
<style>html,body{margin:0;background:#fff;color:#000;overflow:visible;position:static;height:auto;width:auto}body{position:relative;width:${WIDTH}px;height:${HEIGHT}px}</style>
</head><body>${cells}</body></html>`;

const MIME = { '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' };
const server = http.createServer((req, res) => {
    const url = decodeURIComponent(req.url.split('?')[0]);
    if (url === '/') { res.writeHead(200, { 'content-type': 'text/html' }); return res.end(html); }
    const file = path.join(ROOT, url);
    if (!file.startsWith(ROOT) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'content-type': MIME[path.extname(file)] ?? 'application/octet-stream' });
    fs.createReadStream(file).pipe(res);
});

(async () => {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const browser = await chromium.launch();
    const page = await (await browser.newContext({ viewport: { width: WIDTH, height: HEIGHT }, deviceScaleFactor: SCALE })).newPage();

    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForFunction(count => [...document.querySelectorAll('ion-icon')].length === count &&
        [...document.querySelectorAll('ion-icon')].every(e => e.shadowRoot && e.shadowRoot.querySelector('svg')), names.length, { timeout: 120000 });
    await page.waitForTimeout(500);

    const chromiumFile = path.join(dir, 'icons-chromium.png');
    await page.screenshot({ path: chromiumFile, clip: { x: 0, y: 0, width: WIDTH, height: HEIGHT } });

    const uri = file => 'data:image/png;base64,' + fs.readFileSync(file).toString('base64');
    const a = uri(chromiumFile), b = uri(path.join(dir, 'icons-avonic.png'));

    const compared = await page.evaluate(async ({ a, b, names, COLS, CELL, SCALE }) => {
        const load = src => new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
        const [ia, ib] = await Promise.all([load(a), load(b)]);
        if (ia.width !== ib.width || ia.height !== ib.height) throw new Error(`sizes differ: ${ia.width}x${ia.height}, ${ib.width}x${ib.height}`);
        const pixels = img => { const c = document.createElement('canvas'); c.width = img.width; c.height = img.height; const x = c.getContext('2d'); x.drawImage(img, 0, 0); return x.getImageData(0, 0, img.width, img.height).data; };
        const da = pixels(ia), db = pixels(ib), S = CELL * SCALE;
        return names.map((name, i) => {
            const x0 = (i % COLS) * S, y0 = Math.floor(i / COLS) * S;
            let total = 0, big = 0, ink = 0;
            for (let y = y0; y < y0 + S; y++) for (let x = x0; x < x0 + S; x++) {
                const o = (y * ia.width + x) * 4, d = Math.abs(da[o] - db[o]);
                total += d;
                if (d > 64) big++;
                if (da[o] < 128) ink++;
            }
            return { name, i, mean: total / (S * S), big, ink, ratio: ink ? big / ink : (big ? 1 : 0) };
        });
    }, { a, b, names, COLS, CELL, SCALE });

    const sorted = [...compared].sort((x, y) => y.big - x.big);
    const percentile = q => compared.map(r => r.big).sort((x, y) => x - y)[Math.floor(compared.length * q)];
    console.log(`${compared.length} icons at ${SIZE} px, ${SCALE}x. A pixel differs if a channel is off by more than 25%.`);
    console.log(`Differing pixels per icon: median ${percentile(.5)}, p90 ${percentile(.9)}, p99 ${percentile(.99)}, max ${sorted[0].big}.`);
    console.log(`Mean absolute difference (0-255): average ${(compared.reduce((s, r) => s + r.mean, 0) / compared.length).toFixed(3)}, max ${Math.max(...compared.map(r => r.mean)).toFixed(3)}.`);
    console.log(`No differing pixel: ${compared.filter(r => r.big === 0).length}. Over 1% of its ink: ${compared.filter(r => r.ratio > .01).length}. Over 5%: ${compared.filter(r => r.ratio > .05).length}.`);
    console.log('Worst 10:');
    sorted.slice(0, 10).forEach(r => console.log(`  ${r.name.padEnd(32)} ${String(r.big).padStart(4)} of ${String(r.ink).padStart(5)} ink pixels, mean ${r.mean.toFixed(2)}`));

    const sheet = await page.evaluate(async ({ a, b, worst, COLS, CELL, SCALE }) => {
        const load = src => new Promise(res => { const i = new Image(); i.onload = () => res(i); i.src = src; });
        const [ia, ib] = await Promise.all([load(a), load(b)]);
        const S = CELL * SCALE, perRow = 10, c = document.createElement('canvas');
        c.width = perRow * (S * 2 + 8); c.height = Math.ceil(worst.length / perRow) * (S + 8);
        const x = c.getContext('2d'); x.fillStyle = '#ddd'; x.fillRect(0, 0, c.width, c.height);
        worst.forEach((r, k) => {
            const sx = (r.i % COLS) * S, sy = Math.floor(r.i / COLS) * S, dx = (k % perRow) * (S * 2 + 8), dy = Math.floor(k / perRow) * (S + 8);
            x.drawImage(ia, sx, sy, S, S, dx, dy, S, S);
            x.drawImage(ib, sx, sy, S, S, dx + S, dy, S, S);
        });
        return c.toDataURL('image/png');
    }, { a, b, worst: sorted.slice(0, 40), COLS, CELL, SCALE });
    fs.writeFileSync(path.join(dir, 'icons-worst.png'), Buffer.from(sheet.split(',')[1], 'base64'));

    await browser.close();
    server.close();
})().catch(e => { console.error(e); server.close(); process.exit(1); });
