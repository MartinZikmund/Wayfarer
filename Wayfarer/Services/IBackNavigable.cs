namespace Wayfarer.Services;

/// <summary>A page with its own back step (e.g. leaving a detail view) that runs before frame history.</summary>
public interface IBackNavigable
{
    bool CanGoBack { get; }

    event EventHandler? CanGoBackChanged;

    void GoBack();
}
