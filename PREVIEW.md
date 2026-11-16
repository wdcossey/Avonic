# Avonic preview

What Avonic looks like: Ionic's md mode, recreated for Avalonia. Every picture here is a real frame, rendered headless by
Avalonia with Skia at 390 × 844 and a scale of 2, as a phone of that size shows it. Nothing is mocked up or retouched. The
font is Roboto, from `tools/fonts/roboto/`, as the sample has it.

## Showcase

A small app built only from Avonic controls: the sample's `showcase` page, first on its list. Each screen is a page of its own in
[`samples/Avonic.Sample/Pages/Showcase/`](samples/Avonic.Sample/Pages/Showcase/), in plain XAML, so it doubles as example
code. Dark is Ionic's own dark palette, `avares://Avonic.Themes/Tokens/Palettes/Dark.axaml`, merged into the app's resources
([colour system](docs/infra/color-system/README.md#dark-palette)), with no change to the screens.

<table>
  <tr>
    <th>Inbox</th>
    <th>Discover</th>
    <th>Profile</th>
    <th>Settings</th>
  </tr>
  <tr>
    <td><img src="docs/preview/showcase-inbox-light.png" width="200" alt="Inbox, light: a searchbar, messages with avatars, notes and badges, one swiped open" /></td>
    <td><img src="docs/preview/showcase-discover-light.png" width="200" alt="Discover, light: chips, cards with pictures and actions, a fab" /></td>
    <td><img src="docs/preview/showcase-profile-light.png" width="200" alt="Profile, light: a segment, inputs, a textarea, radios, a checkbox and a block button" /></td>
    <td><img src="docs/preview/showcase-settings-light.png" width="200" alt="Settings, light: toggles, a range, notes, a badge and a progress bar" /></td>
  </tr>
  <tr>
    <td><img src="docs/preview/showcase-inbox-dark.png" width="200" alt="Inbox, dark" /></td>
    <td><img src="docs/preview/showcase-discover-dark.png" width="200" alt="Discover, dark" /></td>
    <td><img src="docs/preview/showcase-profile-dark.png" width="200" alt="Profile, dark" /></td>
    <td><img src="docs/preview/showcase-settings-dark.png" width="200" alt="Settings, dark" /></td>
  </tr>
</table>

What's in them:

- **Inbox:** a header with two toolbars, the second holding `ion-searchbar`; `ion-list` of `ion-item-sliding` messages, each an
  `ion-item` with an `ion-avatar` (an `ion-img`, or initials on one of the palette's colours), an `ion-label` with `h2`, `h3` and
  `p`, an `ion-note` and an `ion-badge`. One is swiped open to show its `ion-item-option`s. `ion-tabs` with an `ion-tab-bar` at
  the bottom, and a badge on a tab button.
- **Discover:** `ion-chip`s with icons, `ion-card`s with an `ion-img`, an `ion-card-header` (title and subtitle),
  `ion-card-content` and clear `ion-button`s with icons, a card whose author is an `ion-item`, and an `ion-fab` in the
  content's fixed slot.
- **Profile:** an `ion-segment` in the header, `ion-input`s with floating and stacked labels, an `ion-textarea` that grows with
  its text, an `ion-radio-group` under an `ion-list-header`, an `ion-checkbox`, and a block `ion-button`.
- **Settings:** items with icons at the start, `ion-toggle`s, an `ion-range` between two icons, `ion-note`s for current values,
  an `ion-progress-bar`, and items with the detail chevron.

## Every sample page

The sample has a page for each control, as its tests and the compare step use it: they show states, colours and edge cases
rather than an app. Each frame is the page as it opens, under a toolbar with its title. The `device` page is left out, as it
shows the machine it runs on.

<table>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-ripple-effect.png" width="200" alt="ion-ripple-effect" /><br /><a href="docs/components/ion-ripple-effect/README.md"><code>ion-ripple-effect</code></a></td>
    <td align="center"><img src="docs/preview/pages/color.png" width="200" alt="The colour palette" /><br /><a href="docs/infra/color-system/README.md">colour system</a></td>
    <td align="center"><img src="docs/preview/pages/ion-icon.png" width="200" alt="ion-icon" /><br /><a href="docs/components/ion-icon/README.md"><code>ion-icon</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/gesture.png" width="200" alt="The gesture utility" /><br /><a href="docs/infra/gesture/README.md">gesture</a></td>
    <td align="center"><img src="docs/preview/pages/overlays.png" width="200" alt="Overlays and ion-backdrop" /><br /><a href="docs/infra/overlays/README.md">overlays</a>, <a href="docs/components/ion-backdrop/README.md"><code>ion-backdrop</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-item.png" width="200" alt="ion-item" /><br /><a href="docs/components/ion-item/README.md"><code>ion-item</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-label.png" width="200" alt="ion-label" /><br /><a href="docs/components/ion-label/README.md"><code>ion-label</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-note.png" width="200" alt="ion-note" /><br /><a href="docs/components/ion-note/README.md"><code>ion-note</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-text.png" width="200" alt="ion-text" /><br /><a href="docs/components/ion-text/README.md"><code>ion-text</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-list.png" width="200" alt="ion-list" /><br /><a href="docs/components/ion-list/README.md"><code>ion-list</code></a>, <a href="docs/components/ion-list-header/README.md"><code>ion-list-header</code></a>, <a href="docs/components/ion-item-divider/README.md"><code>ion-item-divider</code></a>, <a href="docs/components/ion-item-group/README.md"><code>ion-item-group</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-button.png" width="200" alt="ion-button" /><br /><a href="docs/components/ion-button/README.md"><code>ion-button</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-content.png" width="200" alt="ion-content" /><br /><a href="docs/components/ion-content/README.md"><code>ion-content</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-toolbar.png" width="200" alt="ion-toolbar" /><br /><a href="docs/components/ion-toolbar/README.md"><code>ion-toolbar</code></a>, <a href="docs/components/ion-header/README.md"><code>ion-header</code></a>, <a href="docs/components/ion-footer/README.md"><code>ion-footer</code></a>, <a href="docs/components/ion-title/README.md"><code>ion-title</code></a>, <a href="docs/components/ion-buttons/README.md"><code>ion-buttons</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-card.png" width="200" alt="ion-card" /><br /><a href="docs/components/ion-card/README.md"><code>ion-card</code></a> and its parts</td>
    <td align="center"><img src="docs/preview/pages/ion-grid.png" width="200" alt="ion-grid" /><br /><a href="docs/components/ion-grid/README.md"><code>ion-grid</code>, <code>ion-row</code>, <code>ion-col</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-spinner.png" width="200" alt="ion-spinner" /><br /><a href="docs/components/ion-spinner/README.md"><code>ion-spinner</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-badge.png" width="200" alt="ion-badge" /><br /><a href="docs/components/ion-badge/README.md"><code>ion-badge</code></a>, <a href="docs/components/ion-avatar/README.md"><code>ion-avatar</code></a>, <a href="docs/components/ion-thumbnail/README.md"><code>ion-thumbnail</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-img.png" width="200" alt="ion-img" /><br /><a href="docs/components/ion-img/README.md"><code>ion-img</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-skeleton-text.png" width="200" alt="ion-skeleton-text" /><br /><a href="docs/components/ion-skeleton-text/README.md"><code>ion-skeleton-text</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-progress-bar.png" width="200" alt="ion-progress-bar" /><br /><a href="docs/components/ion-progress-bar/README.md"><code>ion-progress-bar</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-chip.png" width="200" alt="ion-chip" /><br /><a href="docs/components/ion-chip/README.md"><code>ion-chip</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-checkbox.png" width="200" alt="ion-checkbox" /><br /><a href="docs/components/ion-checkbox/README.md"><code>ion-checkbox</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-toggle.png" width="200" alt="ion-toggle" /><br /><a href="docs/components/ion-toggle/README.md"><code>ion-toggle</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-radio.png" width="200" alt="ion-radio" /><br /><a href="docs/components/ion-radio/README.md"><code>ion-radio</code>, <code>ion-radio-group</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-range.png" width="200" alt="ion-range" /><br /><a href="docs/components/ion-range/README.md"><code>ion-range</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-input.png" width="200" alt="ion-input" /><br /><a href="docs/components/ion-input/README.md"><code>ion-input</code></a>, <a href="docs/components/ion-input-password-toggle/README.md"><code>ion-input-password-toggle</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-textarea.png" width="200" alt="ion-textarea" /><br /><a href="docs/components/ion-textarea/README.md"><code>ion-textarea</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-searchbar.png" width="200" alt="ion-searchbar" /><br /><a href="docs/components/ion-searchbar/README.md"><code>ion-searchbar</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-input-otp.png" width="200" alt="ion-input-otp" /><br /><a href="docs/components/ion-input-otp/README.md"><code>ion-input-otp</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-segment.png" width="200" alt="ion-segment" /><br /><a href="docs/components/ion-segment/README.md"><code>ion-segment</code></a> and its button, view and content</td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-tabs.png" width="200" alt="ion-tabs" /><br /><a href="docs/components/ion-tabs/README.md"><code>ion-tabs</code></a>, <code>ion-tab</code>, <code>ion-tab-bar</code>, <code>ion-tab-button</code></td>
    <td align="center"><img src="docs/preview/pages/ion-fab.png" width="200" alt="ion-fab" /><br /><a href="docs/components/ion-fab/README.md"><code>ion-fab</code>, <code>ion-fab-button</code>, <code>ion-fab-list</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-item-sliding.png" width="200" alt="ion-item-sliding" /><br /><a href="docs/components/ion-item-sliding/README.md"><code>ion-item-sliding</code>, <code>ion-item-options</code>, <code>ion-item-option</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-accordion.png" width="200" alt="ion-accordion" /><br /><a href="docs/components/ion-accordion/README.md"><code>ion-accordion</code></a>, <a href="docs/components/ion-accordion-group/README.md"><code>ion-accordion-group</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-breadcrumbs.png" width="200" alt="ion-breadcrumbs" /><br /><a href="docs/components/ion-breadcrumbs/README.md"><code>ion-breadcrumbs</code>, <code>ion-breadcrumb</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-reorder.png" width="200" alt="ion-reorder" /><br /><a href="docs/components/ion-reorder/README.md"><code>ion-reorder</code>, <code>ion-reorder-group</code></a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-refresher.png" width="200" alt="ion-refresher" /><br /><a href="docs/components/ion-refresher/README.md"><code>ion-refresher</code>, <code>ion-refresher-content</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-infinite-scroll.png" width="200" alt="ion-infinite-scroll" /><br /><a href="docs/components/ion-infinite-scroll/README.md"><code>ion-infinite-scroll</code>, <code>ion-infinite-scroll-content</code></a></td>
    <td align="center"><img src="docs/preview/pages/ion-picker.png" width="200" alt="ion-picker" /><br /><a href="docs/components/ion-picker/README.md"><code>ion-picker</code> and its columns</a></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/preview/pages/ion-app.png" width="200" alt="ion-app" /><br /><a href="docs/components/ion-app/README.md"><code>ion-app</code></a></td>
    <td align="center"><img src="docs/preview/pages/typography.png" width="200" alt="Ionic's typography, opt-in" /><br /><a href="docs/infra/typography/README.md">typography</a></td>
    <td></td>
  </tr>
</table>

## Regenerating

The pictures come from `PreviewCaptureTests`, in `tests/Avonic.Components.Tests/Sample/`, which saves them only when
`AVONIC_PREVIEW_DIR` is set (a relative path is from the repository's root). Without it, the same tests still render every frame
and check its size, as part of the normal test run. Delete `docs/preview` first, so that a page that has gone leaves no
picture behind.

Bash:

```sh
rm -rf docs/preview
AVONIC_PREVIEW_DIR=docs/preview dotnet test tests/Avonic.Components.Tests --filter "FullyQualifiedName~PreviewCaptureTests"
```

PowerShell:

```powershell
Remove-Item -Recurse -Force docs/preview -ErrorAction SilentlyContinue
$env:AVONIC_PREVIEW_DIR = 'docs/preview'
dotnet test tests/Avonic.Components.Tests --filter "FullyQualifiedName~PreviewCaptureTests"
Remove-Item Env:AVONIC_PREVIEW_DIR
```

A new sample page gets a picture by itself: add it to the table above. Each roll-up regenerates them
([process](docs/PROCESS.md#rolling-up-the-tables)).
