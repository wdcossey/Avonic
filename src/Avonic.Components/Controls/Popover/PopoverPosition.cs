using Avalonia;

namespace Avonic.Components.Controls;

/// <summary>
/// The popover's positioning maths, from Ionic's <c>popover/utils.js</c> line for line, with md's arrow of 0 by 0: where the
/// content goes beside its reference (<c>getPopoverPosition</c>), and how it's kept on screen (<c>calculateWindowAdjustment</c>).
/// Everything is in the viewport's coordinates, as <c>getBoundingClientRect</c>'s are. The origins are fractions of the
/// content's box: 0 is <c>left</c> or <c>top</c>, 0.5 <c>center</c>, 1 <c>right</c> or <c>bottom</c>.
/// </summary>
internal static class PopoverPosition
{
    /// <summary><c>POPOVER_MD_BODY_PADDING</c>: how far md keeps the content from the viewport's edges.</summary>
    internal const double MdBodyPadding = 12;

    /// <summary>What <c>getPopoverPosition</c> returns, with md's arrow left out.</summary>
    internal readonly record struct Result(double Top, double Left, Rect? Reference, double OriginX, double OriginY);

    /// <summary>What <c>calculateWindowAdjustment</c> returns, with md's arrow left out.</summary>
    internal readonly record struct Adjusted(
        double Top,
        double Left,
        double? Bottom,
        double OriginX,
        double OriginY,
        bool CheckSafeAreaLeft,
        bool CheckSafeAreaRight,
        bool CheckSafeAreaTop,
        bool CheckSafeAreaBottom,
        bool AddPopoverBottomClass);

    /// <summary>
    /// <c>getPopoverPosition</c>. <paramref name="reference"/> is the trigger's box, or the event's target's (for
    /// <see cref="PopoverReference.Trigger"/>), and <paramref name="point"/> the event's point (for
    /// <see cref="PopoverReference.Event"/>); with the one it needs missing, it's <paramref name="defaultPosition"/>.
    /// </summary>
    internal static Result GetPopoverPosition(
        bool isRtl,
        double contentWidth,
        double contentHeight,
        PopoverReference referenceKind,
        PopoverSide side,
        PopoverAlignment align,
        Result defaultPosition,
        Rect? reference,
        Point? point)
    {
        Rect box;
        switch (referenceKind)
        {
            case PopoverReference.Event:
                if (point is not { } p)
                {
                    return defaultPosition;
                }

                box = new Rect(p.X, p.Y, 1, 1);
                break;
            default:
                if (reference is not { } r)
                {
                    return defaultPosition;
                }

                box = r;
                break;
        }

        var (sideTop, sideLeft) = CalculatePopoverSide(side, box, contentWidth, isRtl);
        var (alignTop, alignLeft) = CalculatePopoverAlign(align, side, box, contentWidth, contentHeight);
        var (originX, originY) = CalculatePopoverOrigin(side, align, isRtl);
        return new Result(sideTop + alignTop, sideLeft + alignLeft, box, originX, originY);

        (double Top, double Left) CalculatePopoverSide(PopoverSide s, Rect trigger, double width, bool rtl)
        {
            var sideLeftPosition = (trigger.Top, trigger.Left - width);
            var sideRightPosition = (trigger.Top, trigger.Left + trigger.Width);
            return s switch
            {
                PopoverSide.Top => (trigger.Top - contentHeight, trigger.Left),
                PopoverSide.Right => sideRightPosition,
                PopoverSide.Bottom => (trigger.Top + trigger.Height, trigger.Left),
                PopoverSide.Left => sideLeftPosition,
                PopoverSide.Start => rtl ? sideRightPosition : sideLeftPosition,
                _ => rtl ? sideLeftPosition : sideRightPosition,
            };
        }
    }

    /// <summary><c>calculatePopoverAlign</c>: what the alignment adds, along the side.</summary>
    private static (double Top, double Left) CalculatePopoverAlign(
        PopoverAlignment align, PopoverSide side, Rect trigger, double contentWidth, double contentHeight)
    {
        var acrossX = side is PopoverSide.Start or PopoverSide.End or PopoverSide.Left or PopoverSide.Right;
        return align switch
        {
            PopoverAlignment.Center => acrossX
                ? (-(contentHeight / 2 - trigger.Height / 2), 0)
                : (0, -(contentWidth / 2 - trigger.Width / 2)),
            PopoverAlignment.End => acrossX
                ? (-(contentHeight - trigger.Height), 0)
                : (0, -(contentWidth - trigger.Width)),
            _ => (0, 0),
        };
    }

    /// <summary><c>calculatePopoverOrigin</c>: the transform's origin, the side's opposite edge and the alignment's.</summary>
    private static (double X, double Y) CalculatePopoverOrigin(PopoverSide side, PopoverAlignment align, bool isRtl)
    {
        var along = align switch
        {
            PopoverAlignment.Center => 0.5,
            PopoverAlignment.End => 1,
            _ => 0,
        };
        return side switch
        {
            PopoverSide.Top => (along, 1),
            PopoverSide.Bottom => (along, 0),
            PopoverSide.Left => (1, along),
            PopoverSide.Right => (0, along),
            PopoverSide.Start => (isRtl ? 0 : 1, along),
            _ => (isRtl ? 1 : 0, along),
        };
    }

    /// <summary>
    /// <c>calculateWindowAdjustment</c>, with md's arrow of 0: kept <paramref name="bodyPadding"/> inside the viewport and its safe
    /// area across, and on a <see cref="PopoverSide.Top"/> or <see cref="PopoverSide.Bottom"/> side flipped above the reference or
    /// stretched to the bottom when it doesn't fit below. With no reference, the reference's bottom is the viewport's middle less
    /// half the content, and its height 0.
    /// </summary>
    internal static Adjusted CalculateWindowAdjustment(
        PopoverSide side,
        double coordTop,
        double coordLeft,
        double bodyPadding,
        double bodyWidth,
        double bodyHeight,
        double contentWidth,
        double contentHeight,
        Thickness safeArea,
        double contentOriginX,
        double contentOriginY,
        Rect? triggerCoordinates)
    {
        const double arrowHeight = 0;
        var left = coordLeft;
        var top = coordTop;
        double? bottom = null;
        var originX = contentOriginX;
        var originY = contentOriginY;
        bool checkSafeAreaLeft = false, checkSafeAreaRight = false, checkSafeAreaTop = false, checkSafeAreaBottom = false;
        var triggerTop = triggerCoordinates is { } t ? t.Top + t.Height : bodyHeight / 2 - contentHeight / 2;
        var triggerHeight = triggerCoordinates?.Height ?? 0;
        var addPopoverBottomClass = false;

        // Off the left of the screen, else off the right.
        if (left < bodyPadding + safeArea.Left)
        {
            left = bodyPadding;
            checkSafeAreaLeft = true;
            originX = 0;
        }
        else if (contentWidth + bodyPadding + left + safeArea.Right > bodyWidth)
        {
            checkSafeAreaRight = true;
            left = bodyWidth - contentWidth - bodyPadding;
            originX = 1;
        }

        // Off the bottom, or over its safe area, on a top or bottom side.
        if (triggerTop + triggerHeight + contentHeight > bodyHeight - safeArea.Bottom && side is PopoverSide.Top or PopoverSide.Bottom)
        {
            // Above the trigger, if it clears the top safe area and the padding.
            var idealFlipTop = triggerTop - contentHeight - triggerHeight - (arrowHeight - 1);
            if (idealFlipTop >= safeArea.Top + bodyPadding)
            {
                top = idealFlipTop;
                originY = 1;
                addPopoverBottomClass = true;
            }
            else
            {
                // The bottom held at the padding, so it scrolls from the trigger down; pulled up to the top if that leaves nothing.
                bottom = bodyPadding;
                checkSafeAreaBottom = true;
                var bottomEdge = bodyHeight - safeArea.Bottom - bodyPadding;
                if (top >= bottomEdge)
                {
                    top = safeArea.Top + bodyPadding;
                    checkSafeAreaTop = true;
                }
            }
        }

        return new Adjusted(top, left, bottom, originX, originY, checkSafeAreaLeft, checkSafeAreaRight, checkSafeAreaTop,
            checkSafeAreaBottom, addPopoverBottomClass);
    }
}
