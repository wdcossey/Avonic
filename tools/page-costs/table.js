#!/usr/bin/env node
// The page costs a -p:AvonicPerf=true build logs (docs/infra/performance/README.md, "Page costs on a device"), as Markdown tables.
//
//   node tools/page-costs/table.js <run.txt>                  one run: first open, later opens (median of rounds 2..n), elements
//   node tools/page-costs/table.js <before.txt> <after.txt>   two runs: later opens side by side, and after / before
//
// A run is the lines as logged, with or without logcat's "AVONIC-PERF " prefix. Pages are sorted by the later opens, slowest first.
// Comparing two runs, the last row sums only the pages both have, so that a run filtered with AVONIC_PERF_ONLY, or one with pages
// the other lacks, still compares like with like.
//
// A run with month changes (the datetime-x1 page's "datetime-month: round N, next 3, ..." lines, SampleShell.PageCosts.cs) gets a
// second table: per round, and over the later rounds, the median and the most of each change's time to its first frame drawn, to
// the heading showing the new month, and of its longest gap between frames; and the gaps over 16.7 and 33 ms, per round. The
// rounds' checks after the month changes ("datetime-month: round N, check: ..." and its warnings, PerfChecks.cs) are listed after,
// with the platform line, DeviceProbe's busy windows ("...: busy: ..."), and any note of what took a page away (a back request, the activity), so that a run that measured
// something it left behind, or lost its page, says so.
'use strict';
const fs = require('fs');

const line = /(?:AVONIC-PERF )?([\w-]+): (\d+) ms, (\d+) elements, .*layout ([\d.]+), render ([\d.]+)/;
// As MonthChange.Line writes it; tests/Avonic.Components.Tests/Sample/PageCostTests.cs holds the same pattern.
const change =
  /(?:AVONIC-PERF )?([\w-]+): round (\d+), (next|previous) (\d+), .* to .*, first (?:([\d.]+) ms|n\/a), heading (?:([\d.]+) ms|n\/a), (\d+) frames, longest ([\d.]+) ms, gap median ([\d.]+) ms, (\d+) over 16\.7 ms, (\d+) over 33 ms/;

function median(values) {
  if (values.length === 0) return NaN;
  const s = [...values].sort((a, b) => a - b);
  const m = s.length >> 1;
  return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2;
}

function load(path) {
  const pages = new Map();
  const changes = new Map(); // label -> [{ round, first, heading, frames, longest, gap, over16, over33 }]
  const checks = [];
  let start = '';
  for (const text of fs.readFileSync(path, 'utf8').split(/\r?\n/)) {
    if (text.includes('start: ')) start = text.slice(text.indexOf('start: ') + 7);
    // After the prefix, wherever it is: a log captured without logcat's "-v raw" has a date, a pid and a tag before it.
    const body = text.includes('AVONIC-PERF ') ? text.slice(text.indexOf('AVONIC-PERF ') + 12) : text;
    if (/, (check|warning): |: busy: |^(platform|note): /.test(body)) checks.push(body);
    const c = change.exec(text);
    if (c) {
      if (!changes.has(c[1])) changes.set(c[1], []);
      changes.get(c[1]).push({
        round: +c[2],
        first: c[5] === undefined ? null : +c[5],
        heading: c[6] === undefined ? null : +c[6],
        frames: +c[7],
        longest: +c[8],
        gap: +c[9],
        over16: +c[10],
        over33: +c[11],
      });
      continue;
    }
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
  return { start, pages: result, changes, checks };
}

// One group of month changes: medians and maxima over its changes, and the long gaps per round.
function summarise(group) {
  const known = (key) => group.map((c) => c[key]).filter((v) => v !== null);
  const most = (key) => (known(key).length ? Math.max(...known(key)) : NaN);
  const rounds = new Set(group.map((c) => c.round)).size || 1;
  return {
    count: group.length,
    firstMedian: median(known('first')),
    firstMax: most('first'),
    headingMedian: median(known('heading')),
    headingMax: most('heading'),
    longestMedian: median(known('longest')),
    longestMax: most('longest'),
    gap: median(group.filter((c) => c.frames > 0).map((c) => c.gap)),
    over16: group.reduce((t, c) => t + c.over16, 0) / rounds,
    over33: group.reduce((t, c) => t + c.over33, 0) / rounds,
  };
}

// A run's changes by round, and its later rounds (2..n, or the only one) together.
function groups(changes) {
  const rounds = [...new Set(changes.map((c) => c.round))].sort((a, b) => a - b);
  const later = rounds.length > 1 ? changes.filter((c) => c.round !== rounds[0]) : changes;
  return { rounds: rounds.map((r) => [`Round ${r}`, changes.filter((c) => c.round === r)]), later };
}

const fixed = (v, digits) => (Number.isFinite(v) ? v.toFixed(digits) : 'n/a');
const ms = (v) => (Number.isFinite(v) ? Math.round(v).toString() : 'n/a');
const perRound = (v) => fixed(v, Number.isInteger(v) ? 0 : 1);

const rows = [
  ['Changes', 'count', (v) => `${v}`],
  ['First frame drawn, median (ms)', 'firstMedian', ms],
  ['First frame drawn, most', 'firstMax', ms],
  ['Heading shows the month, median', 'headingMedian', ms],
  ['Heading shows the month, most', 'headingMax', ms],
  ['Longest gap, median', 'longestMedian', ms],
  ['Longest gap, most', 'longestMax', ms],
  ['Gap between frames, median', 'gap', (v) => fixed(v, 1)],
  ['Gaps over 16.7 ms, per round', 'over16', perRound],
  ['Gaps over 33 ms, per round', 'over33', perRound],
];

const [a, b] = process.argv.slice(2);
if (!a) {
  console.error('usage: node tools/page-costs/table.js <run.txt> [<after.txt>]');
  process.exit(2);
}
const before = load(a);
const sumOf = (run, key, titles) => titles.reduce((t, title) => t + run.pages.get(title)[key], 0);
const listChecks = (name, run) => {
  if (run.checks.length === 0) return;
  console.log(`
${name}checks
`);
  for (const c of run.checks) console.log(`- ${c.includes(', warning: ') || c.startsWith('note: ') ? '**' + c + '**' : c}`);
};

if (!b) {
  console.log(`${before.start}\n`);
  if (before.pages.size > 0) {
    const titles = [...before.pages.keys()];
    console.log('| Page | Elements | First | Later | Layout (later) |');
    console.log('|---|---:|---:|---:|---:|');
    for (const [t, p] of [...before.pages].sort((x, y) => y[1].later - x[1].later)) {
      console.log(`| ${t} | ${p.elements} | ${p.first} | ${ms(p.later)} | ${ms(p.layout)} |`);
    }
    console.log(`| **all ${before.pages.size}** | | ${sumOf(before, 'first', titles)} | ${ms(sumOf(before, 'later', titles))} | ${ms(sumOf(before, 'layout', titles))} |`);
  }
  for (const [label, changes] of before.changes) {
    const { rounds, later } = groups(changes);
    const columns = [...rounds, ['Later rounds', later]].map(([name, group]) => [name, summarise(group)]);
    console.log(`\n${label}: month changes\n`);
    console.log(`| | ${columns.map(([name]) => name).join(' | ')} |`);
    console.log(`|---|${columns.map(() => '---:').join('|')}|`);
    for (const [name, key, format] of rows) {
      console.log(`| ${name} | ${columns.map(([, s]) => format(s[key])).join(' | ')} |`);
    }
  }
  listChecks('', before);
} else {
  const after = load(b);
  console.log(`before: ${before.start}\nafter:  ${after.start}\n`);
  if (before.pages.size > 0 || after.pages.size > 0) {
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
    const both = titles.filter((t) => before.pages.has(t) && after.pages.has(t));
    const bl = sumOf(before, 'later', both);
    const al = sumOf(after, 'later', both);
    const name = both.length === titles.length ? '**all**' : `**the ${both.length} in both**`;
    console.log(`| ${name} | | | ${ms(bl)} | ${ms(al)} | ${bl > 0 ? (al / bl).toFixed(2) : ''} |`);
  }
  for (const label of new Set([...before.changes.keys(), ...after.changes.keys()])) {
    const p = before.changes.has(label) ? summarise(groups(before.changes.get(label)).later) : null;
    const q = after.changes.has(label) ? summarise(groups(after.changes.get(label)).later) : null;
    console.log(`\n${label}: month changes, later rounds\n`);
    console.log('| | Before | After | After / before |');
    console.log('|---|---:|---:|---:|');
    for (const [name, key, format] of rows) {
      const x = p?.[key];
      const y = q?.[key];
      const ratio = key !== 'count' && Number.isFinite(x) && Number.isFinite(y) && x > 0 ? (y / x).toFixed(2) : '';
      console.log(`| ${name} | ${p ? format(x) : ''} | ${q ? format(y) : ''} | ${ratio} |`);
    }
  }
  listChecks('before: ', before);
  listChecks('after: ', after);
}
