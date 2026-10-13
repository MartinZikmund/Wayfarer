using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class StoriesViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public string Title => "Stories";
}
