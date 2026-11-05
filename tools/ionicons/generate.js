#!/usr/bin/env node
// Bundles ionicons' SVGs for AvonicIcon. The SVGs aren't changed: stripping them saves about 1 KB once
// compressed (see docs/components/ion-icon/PLAN.md), and AvonicIcon parses them at runtime anyway.
//
//   node tools/ionicons/generate.js
//
// Output is committed, so a build doesn't need node_modules: sorted lines of `name<TAB>svg`, Brotli
// compressed. Re-run after upgrading ionicons. IconArtworkTests fails if the output is out of date.

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const ROOT = path.resolve(__dirname, '..', '..');
const SOURCE = path.join(ROOT, 'node_modules', 'ionicons');
const OUT_DIR = path.join(ROOT, 'src', 'Avonic.Components', 'Icons');

const svgDir = path.join(SOURCE, 'dist', 'svg');
if (!fs.existsSync(svgDir)) {
    console.error('ionicons not found: run `npm install` first.');
    process.exit(1);
}

// Sort by UTF-16 code unit, not locale, so the output is the same on every machine.
const names = fs.readdirSync(svgDir)
    .filter(file => file.endsWith('.svg'))
    .map(file => file.slice(0, -'.svg'.length))
    .sort();

const lines = names.map(name => {
    const svg = fs.readFileSync(path.join(svgDir, name + '.svg'), 'utf8').replace(/\r?\n/g, ' ').trim();
    return `${name}\t${svg}`;
});

const raw = Buffer.from(lines.join('\n'), 'utf8');
const compressed = zlib.brotliCompressSync(raw, {
    params: {
        [zlib.constants.BROTLI_PARAM_QUALITY]: zlib.constants.BROTLI_MAX_QUALITY,
        [zlib.constants.BROTLI_PARAM_SIZE_HINT]: raw.length,
    },
});

fs.mkdirSync(OUT_DIR, { recursive: true });
fs.writeFileSync(path.join(OUT_DIR, 'ionicons.br'), compressed);
fs.copyFileSync(path.join(SOURCE, 'LICENSE'), path.join(OUT_DIR, 'LICENSE'));

const version = JSON.parse(fs.readFileSync(path.join(SOURCE, 'package.json'), 'utf8')).version;
const kb = bytes => `${Math.round(bytes / 1024)} KB`;
console.log(`ionicons ${version}: ${names.length} icons, ${kb(raw.length)} raw, ${kb(compressed.length)} compressed`);
