# ion-item-group

Status: done · [#9](https://github.com/wdcossey/Avonic/issues/9)

`AvonicItemGroup` holds a run of items and dividers that belong together in an [`AvonicList`](../ion-list/README.md). It has
no look of its own. A sticky [`AvonicItemDivider`](../ion-item-divider/README.md) stays inside its group, and a label directly
inside one acts as it does in an item. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicList>
    <c:AvonicItemGroup>
        <c:AvonicItemDivider Content="Fruit" />
        <c:AvonicItem Content="Apple" />
        <c:AvonicItem Content="Pear" />
    </c:AvonicItemGroup>
    <c:AvonicItemGroup>
        <c:AvonicItemDivider Content="Vegetables" />
        <c:AvonicItem Content="Leek" />
    </c:AvonicItemGroup>
</c:AvonicList>
```

## API

No props, events, methods, slots, parts or CSS variables, in Ionic or here. `Items`, `ItemsSource`, `ItemTemplate` and `ItemsPanel` come from `ItemsControl`.

## Behaviours

Each has a test in `AvonicItemGroupTests`. See the [plan](PLAN.md#behaviours) for the source of each.

- It stacks what's in it, and has no height when empty.
- It's a `Group` to automation.
- A label in it reads `Avonic.Label.Color`, inherits its colour, uses the font token, and gets an ellipsis with `ion-text-nowrap`.
- Each button item is its own tab stop, and the arrow keys do nothing.

## Extensions

None.

## Resource keys

None, as in Ionic.
