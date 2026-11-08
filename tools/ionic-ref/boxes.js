#!/usr/bin/env node
// Prints the boxes in a page of real ion-item and ion-item-divider rows, in Chromium, md, 390 wide, for working out a layout. render.js dumps
// everything about one component, and this is one line a row: its height, its inner and container parts as `x width height`, and each
// element with an id, with y from the top of the row.
//
//   node tools/ionic-ref/boxes.js <page.html> [screenshot.png]
const path = require('path'), fs = require('fs'), http = require('http');
const ROOT = path.resolve(__dirname, '..', '..');
const { chromium } = require(path.join(ROOT, 'node_modules/playwright-core'));
// The same Roboto files as the tests and the sample, so that text measures the same on both sides.
const roboto = require('../fonts/roboto/roboto');
if (!process.argv[2]) {
    console.error('Usage: node tools/ionic-ref/boxes.js <page.html> [screenshot.png]');
    process.exit(1);
}

const body = fs.readFileSync(process.argv[2], 'utf8');
const page = `<!doctype html><html mode="md"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover"><link rel="stylesheet" href="/node_modules/@ionic/core/css/ionic.bundle.css">${roboto.LINK}<script>window.Ionic={config:{mode:'md'}};</script><script type="module" src="/node_modules/@ionic/core/dist/ionic/ionic.esm.js"></script><style>html,body{margin:0;height:auto;overflow:auto;position:static;background:#fff}</style></head><body><div id="stage">${body}</div></body></html>`;
const srv = http.createServer((q, s) => {
    const u = q.url.split('?')[0];
    if (u === '/p') { s.setHeader('content-type', 'text/html'); return s.end(page); }
    const f = path.join(ROOT, decodeURIComponent(u));
    if (!f.startsWith(ROOT) || !fs.existsSync(f) || !fs.statSync(f).isFile()) { s.statusCode = 404; return s.end(); }
    const e = path.extname(f);
    s.setHeader('content-type', e === '.js' ? 'text/javascript' : e === '.css' ? 'text/css' : e === '.svg' ? 'image/svg+xml' : roboto.MIME[e] ?? 'text/plain');
    s.end(fs.readFileSync(f));
});
srv.listen(0, async () => {
    const b = await chromium.launch();
    const p = await b.newPage({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2 });
    await p.goto(`http://localhost:${srv.address().port}/p`);
    await p.waitForSelector('ion-item.hydrated, ion-item-divider.hydrated');
    await roboto.loadRoboto(p);
    await p.waitForTimeout(800);
    const r = await p.evaluate(() => {
        const R = e => { if (!e) return '-'; const b = e.getBoundingClientRect(); return [b.x, b.width, b.height].map(v => +v.toFixed(1)).join(' '); };
        const o = [];
        for (const h of document.querySelectorAll('ion-item,ion-item-divider')) {
            const top = h.getBoundingClientRect().y;
            const s = h.shadowRoot;
            const inner = s.querySelector('.item-inner,.item-divider-inner');
            const w = s.querySelector('.input-wrapper,.item-divider-wrapper');
            const kids = [...h.querySelectorAll('[id]')].map(k => {
                const b = k.getBoundingClientRect();
                return `${k.id}[x${b.x.toFixed(1)} y${(b.y - top).toFixed(1)} w${b.width.toFixed(1)} h${b.height.toFixed(1)}]`;
            });
            o.push(`${h.id}: h=${h.getBoundingClientRect().height.toFixed(1)} inner[${R(inner)}] wrap[${R(w)}] ${kids.join(' ')}`);
        }
        return o;
    });
    console.log(r.join('\n'));
    if (process.argv[3]) await p.screenshot({ path: process.argv[3], fullPage: true });
    await b.close();
    srv.close();
});
