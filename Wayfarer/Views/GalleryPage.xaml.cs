using Microsoft.UI.Xaml.Automation;
using Wayfarer.Models;
using Wayfarer.Services;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class GalleryPage : Page
{
    private const double NarrowWidth = 641;
    private bool _filterBuilt;

    public GalleryPage()
    {
        InitializeComponent();
    }

    public GalleryViewModel ViewModel { get; } = App.GetService<GalleryViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
        BuildTripFilter();

        if (ViewModel.RequestedPhotoId is { } id)
        {
            ViewModel.ClearRequest();
            Photo? photo = await App.GetService<ITripService>().GetPhotoAsync(id);
            if (photo is not null)
            {
                await ShowPhotoAsync(photo);
            }
        }
    }

    private void BuildTripFilter()
    {
        if (_filterBuilt)
        {
            return;
        }

        _filterBuilt = true;
        foreach (Trip trip in ViewModel.Trips)
        {
            SelectorBarItem item = new() { Text = trip.Title, Tag = trip.Id };
            AutomationProperties.SetAutomationId(item, $"GalleryFilter_{trip.Id}");
            TripFilter.Items.Add(item);
        }
    }

    private void OnTripFilterChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args) =>
        ViewModel.Filter(sender.SelectedItem?.Tag as string);

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e) =>
        PhotoLayout.LineHeight = e.NewSize.Width < NarrowWidth ? 140 : 220;

    private void OnItemsInfoRequested(LinedFlowLayout sender, LinedFlowLayoutItemsInfoRequestedEventArgs args)
    {
        IReadOnlyList<GalleryItem> items = ViewModel.Items;
        int start = args.ItemsRangeStartIndex;
        int count = Math.Min(args.ItemsRangeRequestedLength, items.Count - start);
        if (count <= 0)
        {
            return;
        }

        double[] ratios = new double[count];
        for (int i = 0; i < count; i++)
        {
            ratios[i] = items[start + i].Photo.AspectRatio;
        }

        args.SetDesiredAspectRatios(ratios);
    }

    private async void OnPhotoInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is GalleryItem item)
        {
            await ShowPhotoAsync(item.Photo);
        }
    }

    private async Task ShowPhotoAsync(Photo photo)
    {
        GalleryPhotoDialog dialog = new(photo, ViewModel.TripTitle(photo)) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }
}
