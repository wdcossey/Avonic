#!/usr/bin/env node
// Renders the REAL Ionic web component (md mode, phone viewport) in headless Chromium and
// captures evidence for the COMPARE step of docs/PROCESS.md: screenshots, the light + shadow
// DOM, bounding boxes, computed layout, and the resolved value of every CSS custom property
// that core.json lists for the component.
//
//   node tools/ionic-ref/render.js ion-item
//   node tools/ionic-ref/render.js ion-item --html '<ion-list><ion-item button>Hi</ion-item></ion-list>'
//   node tools/ionic-ref/render.js ion-ripple-effect --tap      # press, frames at 0/60/160/320ms, release, 2 more
//   node tools/ionic-ref/render.js ion-item --out docs/components/ion-item/ref   # (default)
//
// Requires `npm install` (playwright-core is pinned to the Chromium build preinstalled in the
// cloud environment; locally run `npx playwright-core install chromium` once if you have none).

const path = require('path');
const fs = require('fs');
const http = require('http');
const { chromium } = require('playwright-core');

const ROOT = path.resolve(__dirname, '..', '..');
const args = process.argv.slice(2);
const tag = args.find(a => !a.startsWith('--'));
if (!tag) {
    console.error('usage: node tools/ionic-ref/render.js <ion-tag> [--html <snippet>] [--tap] [--bare] [--out <dir>]');
    process.exit(1);
}
const opt = (name, dflt) => { const i = args.indexOf(name); return i >= 0 && args[i + 1] && !args[i + 1].startsWith('--') ? args[i + 1] : dflt; };
const has = name => args.includes(name);

// Something meaningful to render when no --html is given.
const DEFAULT_HTML = {
    'ion-ripple-effect':
        '<div class="ion-activatable" style="position:relative;overflow:hidden;width:240px;height:96px;' +
        'display:flex;align-items:center;justify-content:center;background:#e0e0e0;color:#3880ff;user-select:none">' +
        'Tap me<ion-ripple-effect></ion-ripple-effect></div>',
    'ion-item':
        '<ion-list><ion-item button detail><ion-label>Item one</ion-label></ion-item>' +
        '<ion-item><ion-label>Item two</ion-label><ion-note slot="end">end</ion-note></ion-item>' +
        '<ion-item lines="none"><ion-label>Item three</ion-label></ion-item></ion-list>',
    'ion-button':
        '<ion-button>Default</ion-button> <ion-button fill="outline">Outline</ion-button> <ion-button fill="clear">Clear</ion-button>',
};
const snippet = opt('--html', DEFAULT_HTML[tag] ?? `<${tag}></${tag}>`);
const outDir  = path.resolve(ROOT, opt('--out', path.join('docs', 'components', tag, 'ref')));

// CSS vars for this tag, from core.json (deduped across ios/md).
const core = JSON.parse(fs.readFileSync(path.join(ROOT, 'node_modules/@ionic/docs/core.json'), 'utf8'));
const comp = core.components.find(c => c.tag === tag);
if (!comp) { console.error(`unknown component "${tag}"`); process.exit(1); }
const cssVars = [...new Set(comp.styles.map(s => s.name))];

// ── tiny static server rooted at the repo so the ESM bundle can lazy-load its chunks ───────
const MIME = { '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.html': 'text/html',
               '.json': 'application/json', '.svg': 'image/svg+xml', '.png': 'image/png', '.woff2': 'font/woff2', '.woff': 'font/woff' };
const bodyWrap = has('--bare')
    ? `<div id="stage">${snippet}</div>`
    : `<ion-app><ion-content><div id="stage" style="padding:16px">${snippet}</div></ion-content></ion-app>`;
const pageHtml = `<!doctype html>
<html mode="md"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<link rel="stylesheet" href="/node_modules/@ionic/core/css/ionic.bundle.css">
<script>window.Ionic = { config: { mode: 'md' } };</script>
<script type="module" src="/node_modules/@ionic/core/dist/ionic/ionic.esm.js"></script>
<style>html,body{margin:0;height:100%;background:var(--ion-background-color,#fff)}</style>
</head><body>${bodyWrap}</body></html>`;

const server = http.createServer((req, res) => {
    const url = decodeURIComponent(req.url.split('?')[0]);
    if (url === '/' || url === '/index.html') { res.writeHead(200, { 'content-type': 'text/html' }); return res.end(pageHtml); }
    const file = path.join(ROOT, url);
    if (!file.startsWith(ROOT) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'content-type': MIME[path.extname(file)] ?? 'application/octet-stream' });
    fs.createReadStream(file).pipe(res);
});

(async () => {
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    const base = `http://127.0.0.1:${server.address().port}`;
    fs.mkdirSync(outDir, { recursive: true });

    const browser = await chromium.launch();
    const ctx = await browser.newContext({
        viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true,
        userAgent: 'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0 Mobile Safari/537.36',
    });
    const page = await ctx.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(String(e)));
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });

    await page.goto(base + '/');
    await page.waitForFunction(t => {
        const els = [...document.querySelectorAll(t)];
        return els.length > 0 && els.every(e => e.classList.contains('hydrated'));
    }, tag, { timeout: 20000 });
    await page.waitForTimeout(300);

    const LAYOUT = ['display', 'position', 'box-sizing', 'width', 'height', 'min-height', 'min-width', 'padding', 'margin',
        'border', 'border-radius', 'background-color', 'color', 'font-size', 'font-weight', 'font-family', 'line-height',
        'letter-spacing', 'text-transform', 'opacity', 'overflow', 'contain', 'pointer-events', 'transition', 'transform',
        'box-shadow', 'outline', 'cursor'];

    const dump = await page.evaluate(({ tag, cssVars, LAYOUT }) => {
        const r2 = n => Math.round(n * 100) / 100;
        const rect = e => { const r = e.getBoundingClientRect(); return { x: r2(r.x), y: r2(r.y), w: r2(r.width), h: r2(r.height) }; };
        const computed = (e, props) => { const cs = getComputedStyle(e); const o = {}; for (const p of props) o[p] = cs.getPropertyValue(p); return o; };
        const tree = (node, indent) => {
            let s = '';
            for (const c of node.children) {
                const part = c.getAttribute('part'); const slot = c.getAttribute('slot'); const name = c.getAttribute('name');
                s += `${indent}<${c.tagName.toLowerCase()}${c.id ? '#' + c.id : ''}${c.className && typeof c.className === 'string' ? '.' + [...c.classList].join('.') : ''}` +
                     `${part ? ` part="${part}"` : ''}${slot ? ` slot="${slot}"` : ''}${c.tagName === 'SLOT' ? ` name="${name ?? ''}"` : ''}>\n`;
                if (c.shadowRoot) s += `${indent}  #shadow-root\n` + tree(c.shadowRoot, indent + '    ');
                s += tree(c, indent + '  ');
            }
            return s;
        };
        return [...document.querySelectorAll(tag)].map(e => ({
            attributes: Object.fromEntries([...e.attributes].map(a => [a.name, a.value])),
            classes: [...e.classList],
            rect: rect(e),
            cssVars: Object.fromEntries(cssVars.map(v => [v, getComputedStyle(e).getPropertyValue(v).trim()])),
            host: computed(e, LAYOUT),
            parts: [...(e.shadowRoot ? e.shadowRoot.querySelectorAll('[part]') : [])].map(p => ({
                part: p.getAttribute('part'), tag: p.tagName.toLowerCase(), classes: [...p.classList], rect: rect(p), computed: computed(p, LAYOUT),
            })),
            tree: (e.shadowRoot ? '  #shadow-root\n' + tree(e.shadowRoot, '    ') : '') + tree(e, '  '),
        }));
    }, { tag, cssVars, LAYOUT });

    const first = await page.locator(tag).first();
    await page.screenshot({ path: path.join(outDir, 'page.png') });
    await first.screenshot({ path: path.join(outDir, 'element.png') });

    // ── optional press/hold/release sequence (ripples, activated states) ───────────────────
    const frames = [];
    if (has('--tap')) {
        const b = await first.boundingBox();
        const cx = b.x + b.width / 2, cy = b.y + b.height / 2;
        const clip = { x: Math.max(0, b.x - 8), y: Math.max(0, b.y - 8), width: b.width + 16, height: b.height + 16 };
        const shot = async name => { const f = `${name}.png`; await page.screenshot({ path: path.join(outDir, f), clip }); frames.push(f); };
        await page.mouse.move(cx, cy);
        const t0 = Date.now();
        await page.mouse.down();
        for (const ms of [0, 60, 160, 320]) { const wait = t0 + ms - Date.now(); if (wait > 0) await page.waitForTimeout(wait); await shot(`press-${String(ms).padStart(3, '0')}ms`); }
        await page.mouse.up();
        const t1 = Date.now();
        for (const ms of [60, 220, 420]) { const wait = t1 + ms - Date.now(); if (wait > 0) await page.waitForTimeout(wait); await shot(`release-${String(ms).padStart(3, '0')}ms`); }
    }

    await browser.close();
    server.close();

    // ── write ──────────────────────────────────────────────────────────────────────────────
    fs.writeFileSync(path.join(outDir, 'page.html'), pageHtml);
    fs.writeFileSync(path.join(outDir, 'dump.json'), JSON.stringify({ tag, snippet, viewport: '390x844@2x md', errors, elements: dump }, null, 2));

    const md = [];
    md.push(`# ${tag} — reference render (Ionic ${core.components.length ? require(path.join(ROOT, 'node_modules/@ionic/core/package.json')).version : ''}, md, 390×844@2x)`);
    md.push('', `Generated by \`node tools/ionic-ref/render.js ${args.map(a => /\s/.test(a) ? JSON.stringify(a) : a).join(' ')}\``, '');
    md.push('```html', snippet, '```', '');
    md.push(`![page](page.png) ![element](element.png)`, '');
    if (frames.length) md.push('## Press / release frames', '', frames.map(f => `![${f}](${f})`).join(' '), '');
    if (errors.length) md.push('## Console errors', '', ...errors.map(e => `- ${e}`), '');
    dump.forEach((e, i) => {
        md.push(`## Element ${i + 1}`, '', `- attributes: \`${JSON.stringify(e.attributes)}\``, `- classes: \`${e.classes.join(' ')}\``,
                `- rect: \`${JSON.stringify(e.rect)}\``, '');
        if (cssVars.length) {
            md.push('### CSS custom properties (resolved)', '', '| var | value |', '|---|---|');
            for (const [k, v] of Object.entries(e.cssVars)) md.push(`| \`${k}\` | \`${v || '(unset)'}\` |`);
            md.push('');
        }
        md.push('### Host computed', '', '| prop | value |', '|---|---|');
        for (const [k, v] of Object.entries(e.host)) if (v) md.push(`| ${k} | \`${v}\` |`);
        md.push('');
        if (e.parts.length) {
            md.push('### Shadow parts', '');
            for (const p of e.parts) {
                md.push(`#### part="${p.part}" (\`${p.tag}\`) rect \`${JSON.stringify(p.rect)}\``, '', '| prop | value |', '|---|---|');
                for (const [k, v] of Object.entries(p.computed)) if (v) md.push(`| ${k} | \`${v}\` |`);
                md.push('');
            }
        }
        md.push('### DOM tree', '', '```', `<${tag}>`, e.tree.trimEnd(), '```', '');
    });
    fs.writeFileSync(path.join(outDir, 'README.md'), md.join('\n'));

    console.log(`${tag}: ${dump.length} element(s) → ${path.relative(ROOT, outDir)}/  (page.png, element.png${frames.length ? ', ' + frames.length + ' frames' : ''}, dump.json, README.md)`);
    if (errors.length) { console.log(`${errors.length} console error(s) — see dump.json`); }
})().catch(e => { console.error(e); server.close(); process.exit(1); });
