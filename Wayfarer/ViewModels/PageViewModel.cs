namespace Wayfarer.ViewModels;

/// <summary>Base for page view models; pages call <see cref="OnNavigatedToAsync"/> from Page.OnNavigatedTo.</summary>
public abstract partial class PageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <param name="parameter">Navigation parameter: usually a trip, entry or photo id (string), or null.</param>
    public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask;
}
