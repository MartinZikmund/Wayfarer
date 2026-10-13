using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class MomentsViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public string Title => "Moments";
}
