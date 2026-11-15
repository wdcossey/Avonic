namespace Avonic.Components.Controls;

/// <summary>
/// The value of a spinner config key on <see cref="AvonicConfig"/>: a spinner, or <see cref="None"/>, Ionic's <c>null</c>. The key
/// itself is a <c>ConfigSpinner?</c>, whose null is Ionic's <c>undefined</c>, the key not set, so that the next fallback applies.
/// </summary>
/// <remarks>
/// <para>
/// Ionic's <c>config.get(key, fallback)</c> falls back only when the key is <c>undefined</c>: a key set to <c>null</c> gives
/// <c>null</c>, which <c>ion-infinite-scroll-content</c> and <c>ion-refresher-content</c> take as no spinner. Three states, then,
/// where a <see cref="SpinnerName"/><c>?</c> has two, so the key's null stays "not set", as it was before there was a way to say
/// none, and <see cref="None"/> says none.
/// </para>
/// <code>
/// AvonicConfig.InfiniteLoadingSpinner = SpinnerName.Dots;         // dots
/// AvonicConfig.InfiniteLoadingSpinner = ConfigSpinner.None;       // no spinner, as Ionic's null
/// AvonicConfig.InfiniteLoadingSpinner = null;                     // not set: AvonicConfig.Spinner, then md's default
/// </code>
/// </remarks>
/// <param name="Name">The spinner, or null for none.</param>
public readonly record struct ConfigSpinner(SpinnerName? Name)
{
    /// <summary>No spinner: Ionic's <c>null</c>.</summary>
    public static ConfigSpinner None => default;

    /// <summary>A spinner by name.</summary>
    public static implicit operator ConfigSpinner(SpinnerName name) => new(name);

    /// <summary>
    /// Ionic's <c>config.get(key, fallback)</c>: the key's spinner, or none, if it's set, and <paramref name="fallback"/> if not.
    /// </summary>
    internal static SpinnerName? Get(ConfigSpinner? key, SpinnerName? fallback) => key is { } value ? value.Name : fallback;
}
