# ion-title

Status: done · [#12](https://github.com/wdcossey/Avonic/issues/12)

`AvonicTitle` is a toolbar's title, on one line with an ellipsis. It's part of the toolbar family, built and documented together: see
[`ion-toolbar`](../ion-toolbar/README.md) for its API, resource keys, behaviours and known gaps, [PLAN.md](../ion-toolbar/PLAN.md) for the
design and evidence, and [COMPARISON.md](../ion-toolbar/COMPARISON.md) for the comparison with real Ionic.

`Size="Large"` changes nothing in md, as in Ionic, and is kept so that Ionic's values map across: the title gets Ionic's host
class as `:size-large`, with no md rule on it, and its toolbar `:toolbar-title-large`, which an app can style.
