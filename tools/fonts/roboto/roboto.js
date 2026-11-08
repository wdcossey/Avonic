// Roboto for the Chromium tools. A page links roboto.css, which loads the files the tests and the sample embed under the family
// name Roboto, and loadRoboto waits for every face and fails unless each one loaded, so that nothing is measured in a fallback.
// See README.md.
//
//   const roboto = require('../fonts/roboto/roboto');
//   `<head>${roboto.LINK}…</head>`, then `await roboto.loadRoboto(page)` before measuring or taking a screenshot.

/** The stylesheet, served by a tool's own server, which is rooted at the repo. */
const LINK = '<link rel="stylesheet" href="/tools/fonts/roboto/roboto.css">';

/** The weights the files cover: md uses 400, 500 and bold. */
const WEIGHTS = [400, 500, 700];

/** For a tool's server. Chromium doesn't check a font's MIME type, but say what it is. */
const MIME = { '.ttf': 'font/ttf' };

/** Loads every face, and throws unless each one loaded from roboto.css. */
async function loadRoboto(page) {
    const found = await page.evaluate(async weights => {
        // A face that fails to load rejects, so settle them all and let the check below say which.
        await Promise.allSettled(weights.map(w => document.fonts.load(`${w} 16px Roboto`)));
        await document.fonts.ready;
        return {
            sheet: [...document.styleSheets].some(s => (s.href || '').endsWith('/tools/fonts/roboto/roboto.css')),
            faces: [...document.fonts].filter(f => f.family.replace(/"/g, '') === 'Roboto').map(f => ({ weight: f.weight, status: f.status })),
        };
    }, WEIGHTS);
    for (const w of WEIGHTS) {
        if (!found.sheet || !found.faces.some(f => Number(f.weight) === w && f.status === 'loaded')) {
            throw new Error(`Roboto ${w} didn't load from tools/fonts/roboto/roboto.css (${JSON.stringify(found)}), so Chromium would use a fallback`);
        }
    }
}

module.exports = { LINK, WEIGHTS, MIME, loadRoboto };
