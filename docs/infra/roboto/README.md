# Roboto on both sides

Status: done · [#100](https://github.com/wdcossey/Avonic/issues/100)

The tests, the sample and the Chromium tools draw text with the same Roboto files, from
[`tools/fonts/roboto/`](../../../tools/fonts/roboto/README.md), which says where they come from, how each side loads them, and
what's left between them. The libraries don't ship Roboto, as Ionic doesn't.

Before, with Arial in Chromium and Segoe UI in Avalonia, Ionic on the left:

![The item page before](item-before.png)

After, in Roboto on both sides:

![The item page after](item-after.png)

The list and the toolbar, before and after:

![The list page before](list-before.png)
![The list page after](list-after.png)
![The toolbar page before](toolbar-before.png)
![The toolbar page after](toolbar-after.png)
