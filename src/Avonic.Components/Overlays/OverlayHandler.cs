using System.Reflection;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Overlays;

/// <summary>What the alert and the action sheet share: running a button's handler, and reading <c>var(--backdrop-opacity)</c>.</summary>
internal static class OverlayHandler
{
    /// <summary><c>--backdrop-opacity</c>'s md fallback, <c>var(--ion-backdrop-opacity, 0.32)</c>.</summary>
    internal const double DefaultBackdropOpacity = 0.32;

    /// <summary>
    /// Ionic's <c>safeCall</c> awaited, as an overlay's <c>callButtonHandler</c> does: what the handler returned, and when it
    /// returned a <see cref="Task"/> or a <see cref="ValueTask"/>, what that finished with (null for one with no result).
    /// </summary>
    public static async Task<object?> Run(Func<object?> handler)
    {
        var returned = handler();
        // A ValueTask is a Task once asked for one.
        if (returned is not null && returned.GetType() is { IsValueType: true } type
            && (type == typeof(ValueTask) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returned = type.GetMethod(nameof(ValueTask.AsTask))!.Invoke(returned, null);
        }

        if (returned is Task task)
        {
            await task;
            returned = TaskResult(task);
        }

        return returned;
    }

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: the control's own key, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    public static double BackdropOpacity(Control control, string key)
    {
        foreach (var name in (string[])[key, "Avonic.BackdropOpacity"])
        {
            if (control.TryFindResource(name, control.ActualThemeVariant, out var value) && Css.IsSet(value) && value is double or float or int)
            {
                return Convert.ToDouble(value);
            }
        }

        return DefaultBackdropOpacity;
    }

    /// <summary>A <c>Task&lt;T&gt;</c>'s result, or null for a plain <see cref="Task"/>.</summary>
    private static object? TaskResult(Task task)
    {
        var type = task.GetType();
        while (type is not null && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)))
        {
            type = type.BaseType;
        }

        // An async method's plain Task is a Task<VoidTaskResult>: nothing to read.
        return type is not null && type.GetGenericArguments()[0].FullName != "System.Threading.Tasks.VoidTaskResult"
            ? type.GetProperty(nameof(Task<object>.Result), BindingFlags.Public | BindingFlags.Instance)?.GetValue(task)
            : null;
    }
}
