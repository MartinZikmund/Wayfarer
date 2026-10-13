using System.ComponentModel;
using Wayfarer.Helpers;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

/// <summary>One trip's dashboard card. Drives its animation from <see cref="TripMoment.IsPlaying"/>.</summary>
public sealed partial class MomentCard : UserControl
{
    public static readonly DependencyProperty MomentProperty = DependencyProperty.Register(
        nameof(Moment), typeof(TripMoment), typeof(MomentCard), new PropertyMetadata(null, OnMomentChanged));

    private readonly ILogger _logger = App.GetService<ILogger<MomentCard>>();

    public MomentCard()
    {
        InitializeComponent();
        Player.Loaded += (_, _) => ApplyPlayback();
        Unloaded += (_, _) => Watch(Moment, false);
        Loaded += (_, _) => Watch(Moment, true);
    }

    public TripMoment? Moment
    {
        get => (TripMoment?)GetValue(MomentProperty);
        set => SetValue(MomentProperty, value);
    }

    private string TripLine(TripMoment? moment) =>
        moment is null ? "" : $"{moment.Trip.Destination} · {Format.DateRange(moment.Trip.StartDate, moment.Trip.EndDate)}";

    private string Distance(TripMoment? moment) => moment is null ? "" : Format.Distance(moment.Trip.DistanceKm);

    private string Duration(TripMoment? moment) => moment is null ? "" : Format.Days(moment.Trip.DayCount);

    private string PhotoCount(TripMoment? moment) => moment is null ? "" : Format.Photos(moment.Trip.Photos.Count);

    private static void OnMomentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var card = (MomentCard)d;
        card.Watch(e.OldValue as TripMoment, false);
        card.Watch(e.NewValue as TripMoment, card.IsLoaded);

        // Let x:Bind push the new animation source first.
        card.DispatcherQueue.TryEnqueue(card.ApplyPlayback);
    }

    private void Watch(TripMoment? moment, bool watch)
    {
        if (moment is null)
        {
            return;
        }

        moment.PropertyChanged -= OnMomentPropertyChanged;
        if (watch)
        {
            moment.PropertyChanged += OnMomentPropertyChanged;
        }
    }

    private void OnMomentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TripMoment.IsPlaying))
        {
            ApplyPlayback();
        }
    }

    // A recycled or reloaded card must honour the toggle, not autoplay.
    private async void ApplyPlayback()
    {
        if (Moment is not { } moment || !Player.IsLoaded)
        {
            return;
        }

        if (!moment.IsPlaying)
        {
            Player.Pause();
            return;
        }

        if (Player.IsPlaying)
        {
            Player.Resume();
            return;
        }

        try
        {
            await Player.PlayAsync(0, 1, looped: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not play the {Trip} animation.", moment.Trip.Id);
        }
    }
}
