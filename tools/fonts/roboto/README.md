# Roboto, for testing and the sample

Ionic's md mode asks for `"Roboto", "Helvetica Neue", sans-serif`, and so does the theme's `Avonic.FontFamily`. Roboto isn't
installed on most PCs, so each side used to fall back to its own font: Arial in Chromium, Segoe UI in Avalonia on Windows, and
something else again on CI's Ubuntu. Every comparison differed in its text, and tests carried measurements from Segoe UI.

These files are loaded on both sides instead, so that text measures the same in Chromium and in Avalonia, on every machine, and a
difference in a frame is a real one. Roboto is also Android's system font, which is what Avonic gets on a phone.

**The libraries don't ship it,** as Ionic doesn't. An app that wants Roboto on a desktop adds it, as an Ionic app adds its own fonts.

## The files

| File | Weight | Used by md for |
|---|---|---|
| `Roboto-Regular.ttf` | 400 | everything by default |
| `Roboto-Medium.ttf` | 500 | `h1` to `h6`, buttons, card titles and subtitles, titles, back and segment buttons |
| `Roboto-Bold.ttf` | 700 | badges, `strong` buttons, an action sheet's selected button, `b` and `strong` |

Every `font-weight` in Ionic's `*.md.css` and `css/*.css` is one of these, or `normal`, `bold`, `inherit`, `initial`, or a placeholder's variable. Nothing in md is
italic, so there's no italic file: Avalonia would slant the regular one, and Chromium would too.

- **Source:** [googlefonts/roboto-3-classic](https://github.com/googlefonts/roboto-3-classic), release
  [v3.015](https://github.com/googlefonts/roboto-3-classic/releases/tag/v3.015), `Roboto_v3.015.zip`
  (SHA-256 `bee3e9334ea86dd63e184598f31fb16750881c2da1a6f097a66e0f66a95b3d54`), from `unhinted/static/`. This is the release Google
  Fonts serves Roboto from (`ofl/roboto/METADATA.pb` in [google/fonts](https://github.com/google/fonts)), which ships only the variable
  font. Avalonia can't load a variable font, so these are the release's static instances. v3.016 exists, and adds 18 glyphs.
- **Version:** 3.015, in each file's `name` table.
- **Unhinted,** so that neither renderer has hinting instructions to apply or ignore, and the outlines are the same on both sides.
- **Licence:** the SIL Open Font License 1.1, in [`OFL.txt`](OFL.txt), from the same tag.

| File | SHA-256 |
|---|---|
| `Roboto-Regular.ttf` | `867580681a6272f69537e220ae9049f082286daf46c1a4a4d21fa55b2cee89fc` |
| `Roboto-Medium.ttf` | `5f4b733aa0214e1610d2870765f9a68b0380b1bfa5a871f0b1eb20e2a86bee59` |
| `Roboto-Bold.ttf` | `f02bf7a9869c58b9bb61130f55c6b1a489ea85f2ac1de75b3421dcb133bc57f3` |

## Who loads them

- **The tests and the sample:** `samples/Avonic.Sample/Avonic.Sample.csproj` embeds these files, and
  [`Roboto.cs`](../../../samples/Avonic.Sample/Roboto.cs)'s `WithRoboto()` registers them as a font collection and maps the family
  name `Roboto` to it, through `FontManagerOptions.FontFamilyMappings`. `Program` and the tests' `TestApp` call it, so the theme
  finds them through its unchanged `Avonic.FontFamily`. In Avalonia 12.1 a mapping is applied before the system's fonts are asked, so
  an installed Roboto doesn't win over these.
- **The Chromium tools:** [`roboto.css`](roboto.css) loads them with `@font-face` as `Roboto`, and [`roboto.js`](roboto.js) links it
  into a tool's page and waits for every face, failing unless each one loaded. `tools/ionic-ref/render.js` and `boxes.js` use it.
- **The proof:** `RobotoTests` compares the bytes of the font Avalonia drew the theme's text with to these files, at each weight, and
  checks that `roboto.css` gives each weight its file and the file's own line metrics.

## Line height

Chromium on Windows takes a font's ascent and descent from its OS/2 win metrics, 2146 and 555 units here, where Avalonia, and
Chromium on Android, Linux and macOS, take hhea's, 1900 and 500, as these files don't set `USE_TYPO_METRICS`. That made
`line-height: normal` 21 at 16 px in Chromium on this PC, and 18.75 in Avalonia. So `roboto.css` sets `ascent-override`,
`descent-override` and `line-gap-override` to hhea's values, which is what an Android phone does with these files anyway.

What's left is rounding: Chromium rounds a line's ascent and descent each to a whole pixel, so a line of 16 px text is 19 there and
18.75 in Avalonia. Seven lines are 133 and 132 after Avalonia's layout rounding.
