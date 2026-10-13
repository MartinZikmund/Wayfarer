using System.Collections.ObjectModel;
using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class StoriesViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public ObservableCollection<Trip> Trips { get; } = new();

    [ObservableProperty]
    public partial Trip? SelectedTrip { get; set; }

    [ObservableProperty]
    public partial bool HighlightPlaces { get; set; } = true;

    public bool HasStory => SelectedTrip is not null;

    partial void OnSelectedTripChanged(Trip? value) => OnPropertyChanged(nameof(HasStory));

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;
        try
        {
            if (Trips.Count == 0)
            {
                foreach (Trip trip in await _tripService.GetTripsAsync())
                {
                    Trips.Add(trip);
                }
            }

            // A trip id selects that story; entry and photo ids fall back to the current one.
            Trip? requested = parameter is string id ? Trips.FirstOrDefault(t => t.Id == id) : null;
            SelectedTrip = requested ?? SelectedTrip ?? Trips.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
