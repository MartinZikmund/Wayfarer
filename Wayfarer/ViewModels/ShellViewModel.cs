using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class ShellViewModel(ITripService tripService, INavigationService navigation) : ObservableObject
{
    private CancellationTokenSource? _searchCts;

    /// <summary>Suggestions for the title bar search; a newer query cancels the previous one.</summary>
    public async Task<IReadOnlyList<SearchResult>?> GetSuggestionsAsync(string query)
    {
        _searchCts?.Cancel();
        CancellationTokenSource cts = new();
        _searchCts = cts;

        try
        {
            var results = await tripService.SearchAsync(query, cts.Token);
            return cts.IsCancellationRequested ? null : results;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>Opens the chosen result, or the best match when the user just pressed Enter.</summary>
    public async Task OpenSearchResultAsync(SearchResult? chosen, string query)
    {
        var target = chosen ?? (await tripService.SearchAsync(query)).FirstOrDefault();
        if (target is not null)
        {
            navigation.Navigate(target.TargetPage, target.TargetId);
        }
    }
}
