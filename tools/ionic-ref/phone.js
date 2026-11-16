// The phone that render.js renders on, and boxes.js with --mobile: 390 x 844 at 2x, mobile and touch, with Chrome for Android's
// user agent. A page of the same size in a desktop context lays out differently: a textarea there keeps 15 px for its scroll bar,
// where a mobile one keeps 4 (#155).
//
//   const phone = require('./phone');
//   const ctx = await browser.newContext(phone.CONTEXT);
const CONTEXT = Object.freeze({
    viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true,
    userAgent: 'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0 Mobile Safari/537.36',
});

module.exports = { CONTEXT };
