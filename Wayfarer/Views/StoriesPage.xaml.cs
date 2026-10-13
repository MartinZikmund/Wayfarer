using System.ComponentModel;
using Microsoft.UI.Xaml.Documents;
using Wayfarer.Helpers;
using Wayfarer.Models;
using Wayfarer.ViewModels;
using Windows.Foundation;

namespace Wayfarer.Views;

public sealed partial class StoriesPage : Page
{
    private const double TwoColumnWidth = 720;
    private const double ThreeColumnWidth = 1100;

    // Below this the picker and toggle would squeeze a longer trip title.
    private const double WideHeaderWidth = 900;
    private const double MinColumnHeight = 120;
    private const double ColumnHeightStep = 24;
    private const double ColumnSpacing = 24;

    // One body line, so a paragraph split mid-line still fits.
    private const double LineSlack = 24;
    private const int MaxFitPasses = 24;

    private int _columns = 1;
    private double _columnHeight = MinColumnHeight;
    private int _fitPasses;

    public StoriesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Columns.SizeChanged += OnColumnsSizeChanged;
        // Rebuild after the theme walk, so the probe already holds the new theme's brush.
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(BuildStory);
    }

    public StoriesViewModel ViewModel { get; } = App.GetService<StoriesViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }

    // LayoutUpdated is a global event that roots its handler, so only listen while the page is in the tree.
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnUnloaded(sender, e);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        StoryText.LayoutUpdated += OnStoryLayoutUpdated;

        // The trip may have been picked during OnNavigatedTo, before we were listening.
        BuildStory();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        StoryText.LayoutUpdated -= OnStoryLayoutUpdated;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(StoriesViewModel.SelectedTrip):
                BuildStory();
                break;
            case nameof(StoriesViewModel.HighlightPlaces):
                BuildStory();
                break;
        }
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        int columns = e.NewSize.Width >= ThreeColumnWidth ? 3 : e.NewSize.Width >= TwoColumnWidth ? 2 : 1;
        VisualStateManager.GoToState(this, e.NewSize.Width >= WideHeaderWidth ? "WideHeader" : "NarrowHeader", false);
        if (columns != _columns)
        {
            _columns = columns;
            ApplyColumns();
        }
    }

    private void OnColumnsSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_columns > 1 && e.PreviousSize.Width != e.NewSize.Width)
        {
            ApplyColumns();
        }
    }

    private void BuildStory()
    {
        StoryText.Blocks.Clear();
        BylineText.Blocks.Clear();
        if (ViewModel.SelectedTrip is not { } trip)
        {
            return;
        }

        BylineText.Blocks.Add(StoryComposer.Byline(trip, AccentBrushProbe.Background));
        foreach (Paragraph paragraph in StoryComposer.Compose(trip, AccentBrushProbe.Background, ViewModel.HighlightPlaces))
        {
            StoryText.Blocks.Add(paragraph);
        }

        RelinkOverflow();
    }

    // The overflow chain keeps showing the previous story unless it is re-linked after the blocks change.
    private void RelinkOverflow()
    {
        StoryText.OverflowContentTarget = null;
        Overflow1.OverflowContentTarget = null;
        ApplyColumns();
        Overflow1.InvalidateMeasure();
        Overflow2.InvalidateMeasure();
    }

    // One column flows as a normal page; wider layouts need a fixed column height to force the text to overflow.
    private void ApplyColumns()
    {
        Column1.Width = _columns >= 2 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Column2.Width = _columns >= 3 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Columns.ColumnSpacing = 0;
        Overflow1.Visibility = _columns >= 2 ? Visibility.Visible : Visibility.Collapsed;
        Overflow2.Visibility = _columns >= 3 ? Visibility.Visible : Visibility.Collapsed;
        StoryText.OverflowContentTarget = _columns >= 2 ? Overflow1 : null;
        Overflow1.OverflowContentTarget = _columns >= 3 ? Overflow2 : null;

        if (_columns == 1)
        {
            StoryText.Height = double.NaN;
            return;
        }

        Columns.ColumnSpacing = ColumnSpacing;
        _columnHeight = Math.Max(MinColumnHeight, EstimateColumnHeight());
        _fitPasses = 0;
        SetColumnHeight();
    }

    // Sizes the columns from a measured single-column height; HasOverflowContent alone is not reliable on Uno.
    private double EstimateColumnHeight()
    {
        // Before the first arrange the width is unknown; OnColumnsSizeChanged refits once it is.
        double columnWidth = (Columns.ActualWidth - ColumnSpacing * (_columns - 1)) / _columns;
        if (ViewModel.SelectedTrip is not { } trip || columnWidth <= 0)
        {
            return 0;
        }

        RichTextBlock probe = new() { TextWrapping = TextWrapping.Wrap };
        foreach (Paragraph paragraph in StoryComposer.Compose(trip, AccentBrushProbe.Background, ViewModel.HighlightPlaces))
        {
            probe.Blocks.Add(paragraph);
        }

        probe.Measure(new Size(columnWidth, double.PositiveInfinity));
        return probe.DesiredSize.Height / _columns + LineSlack;
    }

    private void SetColumnHeight()
    {
        StoryText.Height = _columnHeight;
        Overflow1.Height = _columnHeight;
        Overflow2.Height = _columnHeight;
    }

    // Grow the columns until the last one swallows the rest of the story.
    private void OnStoryLayoutUpdated(object? sender, object e)
    {
        RichTextBlockOverflow last = _columns >= 3 ? Overflow2 : Overflow1;
        if (_columns < 2 || !last.HasOverflowContent || _fitPasses >= MaxFitPasses)
        {
            return;
        }

        _fitPasses++;
        _columnHeight += ColumnHeightStep;
        SetColumnHeight();
    }
}
