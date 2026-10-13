using System.ComponentModel;
using Microsoft.UI.Dispatching;
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
    private const double MinColumnHeight = 360;
    private const double ColumnHeightStep = 48;
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
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        StoryText.LayoutUpdated += OnStoryLayoutUpdated;
        // Rebuild after the theme walk, so the probe already holds the new theme's brush.
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(BuildStory);
    }

    public StoriesViewModel ViewModel { get; } = App.GetService<StoriesViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
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
        if (columns != _columns)
        {
            _columns = columns;
            ApplyColumns();
        }
    }

    private void BuildStory()
    {
        StoryText.Blocks.Clear();
        if (ViewModel.SelectedTrip is not { } trip)
        {
            return;
        }

        foreach (Paragraph paragraph in StoryComposer.Compose(trip, AccentBrushProbe.Background, ActualTheme, ViewModel.HighlightPlaces))
        {
            StoryText.Blocks.Add(paragraph);
        }

        ApplyColumns();
        RelinkOverflow();

        // Uno only paints an inline photo that opens an overflow column after the columns change width.
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            Columns.Margin = new Thickness(0, 0, 1, 0);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => Columns.Margin = new Thickness(0));
        });
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

    // HasOverflowContent alone under-reports when a photo sits at a column break, so start from a simulated fill:
    // text paragraphs may split across columns, inline photo cards may not.
    private double EstimateColumnHeight()
    {
        double width = Columns.ActualWidth > 0 ? Columns.ActualWidth : ActualWidth;
        double columnWidth = (width - ColumnSpacing * (_columns - 1)) / _columns;
        if (ViewModel.SelectedTrip is not { } trip || columnWidth <= 0)
        {
            return 0;
        }

        List<(double Height, bool CanSplit)> blocks = new();
        foreach (Paragraph paragraph in StoryComposer.Compose(trip, AccentBrushProbe.Background, ActualTheme, ViewModel.HighlightPlaces))
        {
            bool canSplit = !paragraph.Inlines.OfType<InlineUIContainer>().Any();
            RichTextBlock probe = new() { TextWrapping = TextWrapping.Wrap };
            probe.Blocks.Add(paragraph);
            probe.Measure(new Size(columnWidth, double.PositiveInfinity));
            blocks.Add((probe.DesiredSize.Height, canSplit));
        }

        double height = Math.Max(blocks.Sum(b => b.Height) / _columns, blocks.Max(b => b.Height));
        while (!Fits(blocks, height, _columns))
        {
            height += LineSlack / 2;
        }

        return height + LineSlack;
    }

    private static bool Fits(List<(double Height, bool CanSplit)> blocks, double columnHeight, int columns)
    {
        int column = 1;
        double used = 0;
        foreach ((double height, bool canSplit) in blocks)
        {
            if (used + height <= columnHeight)
            {
                used += height;
            }
            else if (canSplit)
            {
                double remaining = height - (columnHeight - used);
                column += 1 + (int)(remaining / columnHeight);
                used = remaining % columnHeight;
            }
            else
            {
                column++;
                used = height;
            }
        }

        return column <= columns;
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
