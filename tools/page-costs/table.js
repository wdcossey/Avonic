#!/usr/bin/env node
// The page costs a -p:AvonicPerf=true build logs (docs/infra/performance/README.md, "Page costs on a device"), as a Markdown table.
//
//   node tools/page-costs/table.js <run.txt>                  one run: first open, later opens (median of rounds 2..n), elements
//   node tools/page-costs/table.js <before.txt> <after.txt>   two runs of the same pages: later opens side by side, and after / before
//
// A run is the lines as logged, with or without logcat's "AVONIC-PERF " prefix. Pages are sorted by the later opens, slowest first.
'use strict';
const fs = require('fs');

const line = /(?:AVONIC-PERF )?([\w-]+): (\d+) ms, (\d+) elements, .*layout ([\d.]+), render ([\d.]+)/;

function median(values) {
  const s = [...values].sort((a, b) => a - b);
  const m = s.length >> 1;
  return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2;
}

function load(path) {
  const pages = new Map();
  let start = '';
  for (const text of fs.readFileSync(path, 'utf8').split(/\r?\n/)) {
    if (text.includes('start: ')) start = text.slice(text.indexOf('start: ') + 7);
    const m = line.exec(text);
    if (!m) continue;
    if (!pages.has(m[1])) pages.set(m[1], []);
    pages.get(m[1]).push({ ms: +m[2], elements: +m[3], layout: +m[4] });
  }
  const result = new Map();
  for (const [title, runs] of pages) {
    const later = runs.length > 1 ? runs.slice(1) : runs;
    result.set(title, {
      first: runs[0].ms,
      later: median(later.map((r) => r.ms)),
      layout: median(later.map((r) => r.layout)),
      elements: runs[0].elements,
    });
  }
  return { start, pages: result };
}

const [a, b] = process.argv.slice(2);
if (!a) {
  console.error('usage: node tools/page-costs/table.js <run.txt> [<after.txt>]');
  process.exit(2);
}
const before = load(a);
const sum = (run, key) => [...run.pages.values()].reduce((t, p) => t + p[key], 0);
const ms = (v) => Math.round(v).toString();

if (!b) {
  console.log(`${before.start}\n`);
  console.log('| Page | Elements | First | Later | Layout (later) |');
  console.log('|---|---:|---:|---:|---:|');
  for (const [t, p] of [...before.pages].sort((x, y) => y[1].later - x[1].later)) {
    console.log(`| ${t} | ${p.elements} | ${p.first} | ${ms(p.later)} | ${ms(p.layout)} |`);
  }
  console.log(`| **all ${before.pages.size}** | | ${sum(before, 'first')} | ${ms(sum(before, 'later'))} | ${ms(sum(before, 'layout'))} |`);
} else {
  const after = load(b);
  console.log(`before: ${before.start}\nafter:  ${after.start}\n`);
  console.log('| Page | Elements before | Elements after | Later before | Later after | After / before |');
  console.log('|---|---:|---:|---:|---:|---:|');
  const titles = [...new Set([...before.pages.keys(), ...after.pages.keys()])]
    .sort((x, y) => (before.pages.get(y)?.later ?? 0) - (before.pages.get(x)?.later ?? 0));
  for (const t of titles) {
    const p = before.pages.get(t);
    const q = after.pages.get(t);
    const ratio = p && q ? (q.later / p.later).toFixed(2) : '';
    console.log(`| ${t} | ${p?.elements ?? ''} | ${q?.elements ?? ''} | ${p ? ms(p.later) : ''} | ${q ? ms(q.later) : ''} | ${ratio} |`);
  }
  const bl = sum(before, 'later');
  const al = sum(after, 'later');
  console.log(`| **all** | | | ${ms(bl)} | ${ms(al)} | ${(al / bl).toFixed(2)} |`);
}
