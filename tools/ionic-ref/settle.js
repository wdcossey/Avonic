// How the Chromium tools wait for a page of Ionic components before they read it, instead of a fixed delay. See tools/README.md.
//
//   const settle = require('./settle');
//   page HTML: `…<div id="stage">${settle.defer(snippet)}</div>…`, then after page.goto:
//   await settle.settle(page);
//
// 1. Every ion-* tag the page uses is defined: Ionic's loader defines them all when it starts.
// 2. Every lazy module of Ionic's bundle is imported, before the deferred snippet is in the document. Each component waits for its
//    own module, so without this, components resume in the order their modules happen to arrive: an ion-item with two ion-labels
//    takes the style of whichever label ran last, which changed from run to run (#104). With the modules in, they start in
//    document order, as in Ionic's custom-elements build, where nothing is lazy.
// 3. The snippet goes in, from the <template> defer() made.
// 4. componentOnReady() for every ion-* element, in the light DOM and in shadow roots, until a pass finds no new one.
// 5. Two animation frames, as components do work in a frame after they load (ion-item's componentDidLoad).
// 6. Then frames until the page is still: the same layout box and classes for every element, the fonts loaded, every image
//    complete and no finite animation or transition running, for `frames` frames in a row. Infinite ones (a spinner) are left
//    out. If that doesn't happen within `timeout`, it throws, naming what kept changing, rather than reading the page early.

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const BUNDLE = path.join(ROOT, 'node_modules', '@ionic', 'core', 'dist', 'ionic');
const ATTRIBUTE = 'data-ionic-ref-defer';

/** The snippet, held back in a <template> until settle() has loaded what it needs. */
function defer(html) {
    return `<template ${ATTRIBUTE}>${html}</template>`;
}

/** Waits for the page to be ready to read; throws if it isn't within `timeout` ms. */
async function settle(page, { timeout = 20000, frames = 3 } = {}) {
    const entries = fs.readdirSync(BUNDLE).filter(file => file.endsWith('.entry.js')).sort();
    if (!entries.length) throw new Error(`settle: no lazy modules in ${BUNDLE}; run npm install`);

    await page.evaluate(async ({ entries, timeout, frames, attribute }) => {
        const start = performance.now();
        const fail = what => { throw new Error(`settle: ${what}, after ${Math.round(performance.now() - start)} ms`); };
        const within = (promise, what) => new Promise((resolve, reject) => {
            const timer = setTimeout(() => { try { fail(what()); } catch (e) { reject(e); } }, Math.max(0, start + timeout - performance.now()));
            promise.then(v => { clearTimeout(timer); resolve(v); }, e => { clearTimeout(timer); reject(e); });
        });
        const frame = () => new Promise(resolve => requestAnimationFrame(() => resolve()));

        // Every element, through shadow roots, in document order.
        const everything = (root = document) => {
            const found = [];
            const walk = node => { for (const e of node.querySelectorAll('*')) { found.push(e); if (e.shadowRoot) walk(e.shadowRoot); } };
            walk(root);
            return found;
        };
        const isIonic = e => e.localName.startsWith('ion-');
        const describe = e => `${e.localName}${e.id ? '#' + e.id : ''}${typeof e.className === 'string' && e.className ? '.' + e.className.trim().split(/\s+/).join('.') : ''}`;

        // 1. Defined.
        const templates = [...document.querySelectorAll(`template[${attribute}]`)];
        const tags = new Set([...everything(), ...templates.flatMap(t => [...t.content.querySelectorAll('*')])].filter(isIonic).map(e => e.localName));
        await within(Promise.all([...tags].map(t => customElements.whenDefined(t))),
            () => `never defined: ${[...tags].filter(t => !customElements.get(t)).join(', ')}`);

        // 2. Every lazy module, at the URL Ionic's loader imports it from, so that the loader finds it already evaluated.
        const loader = document.querySelector('script[src*="/@ionic/core/dist/ionic/ionic.esm.js"]');
        if (!loader) fail('no <script> for @ionic/core/dist/ionic/ionic.esm.js');
        const base = new URL('.', loader.src);
        await within(Promise.all(entries.map(file => import(new URL(file, base).href))), () => 'Ionic\'s lazy modules didn\'t load');

        // 3. The snippet.
        for (const t of templates) t.replaceWith(t.content);

        // 4. Ready, until no new component appears.
        const ready = new Set();
        for (;;) {
            const pending = everything().filter(e => isIonic(e) && !ready.has(e));
            if (!pending.length) break;
            await within(Promise.all(pending.map(e => typeof e.componentOnReady === 'function' ? e.componentOnReady() : Promise.resolve())),
                () => `not ready: ${pending.filter(e => !e.classList.contains('hydrated')).slice(0, 5).map(describe).join(', ')}`);
            pending.forEach(e => ready.add(e));
        }

        // 5. Two frames.
        await frame();
        await frame();

        // 6. Still. offset* is the layout box, which a transform doesn't move, so a spinning spinner doesn't count; an SVG element
        //    has none, so it counts by being there.
        const box = e => e instanceof HTMLElement ? `${e.offsetLeft},${e.offsetTop},${e.offsetWidth},${e.offsetHeight}` : '';
        const snapshot = () => {
            const elements = everything();
            return { elements, keys: elements.map(e => `${e.localName}|${e.getAttribute('class') ?? ''}|${box(e)}`) };
        };
        const busy = elements => {
            const roots = [document, ...elements.filter(e => e.shadowRoot).map(e => e.shadowRoot)];
            const running = roots.flatMap(r => r.getAnimations()).filter(a => a.playState === 'running' && Number.isFinite(a.effect?.getComputedTiming().endTime));
            const images = elements.filter(e => e instanceof HTMLImageElement && !e.complete);
            const reasons = [];
            if (document.fonts.status !== 'loaded') reasons.push('fonts loading');
            if (images.length) reasons.push(`${images.length} image(s) loading: ${images.slice(0, 3).map(i => i.src).join(', ')}`);
            if (running.length) reasons.push(`${running.length} animation(s) running: ${running.slice(0, 3).map(a => describe(a.effect.target ?? document.body)).join(', ')}`);
            return reasons;
        };
        let last = snapshot(), why = busy(last.elements), still = why.length ? 0 : 1;
        while (still < frames) {
            if (performance.now() - start > timeout) {
                fail(`the page never kept still for ${frames} frames (${why.join('; ') || 'unknown'})`);
            }
            await frame();
            const now = snapshot();
            const reasons = busy(now.elements);
            const same = now.keys.length === last.keys.length && now.keys.every((k, i) => k === last.keys[i]);
            if (same && !reasons.length) {
                still++;
            } else {
                still = 1;
                const changed = now.keys.length !== last.keys.length
                    ? [`${last.keys.length} elements, then ${now.keys.length}`]
                    : now.elements.filter((e, i) => now.keys[i] !== last.keys[i]).slice(0, 5).map(describe);
                why = [...reasons, ...(same ? [] : [`changing: ${changed.join(', ')}`])];
            }
            last = now;
        }
    }, { entries, timeout, frames, attribute: ATTRIBUTE });
}

module.exports = { defer, settle };
