using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class GalleryViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public string Title => "Gallery";
}
