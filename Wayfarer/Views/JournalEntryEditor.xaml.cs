using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Wayfarer.Helpers;
using Wayfarer.Services;
using Wayfarer.ViewModels;
using Windows.UI;

// The rich text APIs below work at runtime; their [NotImplemented] stubs are stale.
#pragma warning disable Uno0001

namespace Wayfarer.Views;

/// <summary>A font color choice in the editor's color flyout.</summary>
public sealed class ColorSwatch(string name, Color color)
{
    public string Name { get; } = name;

    public Color Color { get; } = color;

    public SolidColorBrush Brush { get; } = new(color);

    public override string ToString() => Name;
}

/// <summary>Rich text editor for one journal entry. Shared by the journal page and the entry window.</summary>
public sealed partial class JournalEntryEditor : UserControl
{
    // Saved into the RTF, so one palette has to read on both themes: every swatch is at least 3.7:1
    // against the light and the dark editor surface.
    private static readonly ColorSwatch[] Palette =
    [
        new("Teal", ColorHelper.FromArgb(255, 12, 146, 142)),
        new("Blue", ColorHelper.FromArgb(255, 29, 133, 237)),
        new("Purple", ColorHelper.FromArgb(255, 168, 95, 242)),
        new("Magenta", ColorHelper.FromArgb(255, 238, 43, 173)),
        new("Red", ColorHelper.FromArgb(255, 239, 69, 57)),
        new("Orange", ColorHelper.FromArgb(255, 217, 97, 18)),
        new("Gold", ColorHelper.FromArgb(255, 165, 125, 13)),
        new("Green", ColorHelper.FromArgb(255, 12, 151, 24)),
    ];

    private string _baseline = "";
    private bool _loading;

    public JournalEntryEditor()
    {
        InitializeComponent();
        ColorGrid.ItemsSource = Palette;
        OpenWindowButton.Visibility = EntryWindow.IsSupported ? Visibility.Visible : Visibility.Collapsed;
    }

    public JournalEntryEditorViewModel ViewModel { get; } = new(App.GetService<ITripService>());

    public string? EntryId => ViewModel.Entry?.Id;

    /// <summary>Hidden inside the entry window itself, where it makes no sense.</summary>
    public bool ShowOpenInNewWindow
    {
        set => OpenWindowButton.Visibility = value && EntryWindow.IsSupported ? Visibility.Visible : Visibility.Collapsed;
    }

    public event EventHandler? Deleted;

    public event EventHandler? OpenedInNewWindow;

    public async Task LoadAsync(string entryId)
    {
        _loading = true;
        try
        {
            if (await ViewModel.LoadAsync(entryId) is not { } entry)
            {
                return;
            }

            Editor.Document.SetText(TextSetOptions.FormatRtf, RtfBuilder.FromEntry(entry));
            Editor.Document.ClearUndoRedoHistory();
            _baseline = GetPlainText();
            UpdateToolbarState();
        }
        finally
        {
            _loading = false;
        }
    }

    public async Task SaveIfDirtyAsync()
    {
        if (!ViewModel.IsDirty)
        {
            return;
        }

        Editor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
        await ViewModel.SaveAsync(rtf, GetPlainText());
    }

    public string SaveStatusText(bool isDirty) => isDirty ? "Unsaved changes" : "All changes saved";

    private string GetPlainText()
    {
        Editor.Document.GetText(TextGetOptions.None, out var text);
        return text;
    }

    private void UpdateToolbarState()
    {
        var selection = Editor.Document.Selection;
        BoldButton.IsChecked = selection.CharacterFormat.Bold == FormatEffect.On;
        ItalicButton.IsChecked = selection.CharacterFormat.Italic == FormatEffect.On;
        UnderlineButton.IsChecked = selection.CharacterFormat.Underline == UnderlineType.Single;
        StrikethroughButton.IsChecked = selection.CharacterFormat.Strikethrough == FormatEffect.On;
        UndoButton.IsEnabled = Editor.Document.CanUndo();
        RedoButton.IsEnabled = Editor.Document.CanRedo();
    }

    private void Formatted()
    {
        ViewModel.MarkDirty();
        Editor.Focus(FocusState.Programmatic);
        UpdateToolbarState();
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e) => UpdateToolbarState();

    private void OnEditorTextChanged(object sender, RoutedEventArgs e)
    {
        if (!_loading && GetPlainText() != _baseline)
        {
            ViewModel.MarkDirty();
        }

        UpdateToolbarState();
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Undo();
        Formatted();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Redo();
        Formatted();
    }

    private void OnBoldClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Selection.CharacterFormat.Bold = BoldButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
        Formatted();
    }

    private void OnItalicClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Selection.CharacterFormat.Italic = ItalicButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
        Formatted();
    }

    private void OnUnderlineClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Selection.CharacterFormat.Underline = UnderlineButton.IsChecked == true ? UnderlineType.Single : UnderlineType.None;
        Formatted();
    }

    private void OnStrikethroughClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Selection.CharacterFormat.Strikethrough = StrikethroughButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
        Formatted();
    }

    private void OnColorClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ColorSwatch swatch)
        {
            Editor.Document.Selection.CharacterFormat.ForegroundColor = swatch.Color;
            ColorFlyout.Hide();
            Formatted();
        }
    }

    private void OnAutomaticColorClick(object sender, RoutedEventArgs e)
    {
        Editor.Document.Selection.CharacterFormat.ForegroundColor = TextConstants.AutoColor;
        ColorFlyout.Hide();
        Formatted();
    }

    private void OnLinkFlyoutOpening(object? sender, object e)
    {
        Editor.Document.Selection.GetText(TextGetOptions.None, out var selected);
        LinkBox.Text = Uri.TryCreate(selected.Trim(), UriKind.Absolute, out var uri) ? uri.ToString() : "";
    }

    private void OnInsertLinkClick(object sender, RoutedEventArgs e)
    {
        var address = LinkBox.Text.Trim();
        if (address.Length == 0)
        {
            return;
        }

        if (!address.Contains("://", StringComparison.Ordinal))
        {
            address = $"https://{address}";
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return;
        }

        var selection = Editor.Document.Selection;
        if (selection.Length == 0)
        {
            selection.SetText(TextSetOptions.None, uri.ToString());
        }

        selection.Link = $"\"{uri}\"";
        LinkFlyout.Hide();
        Formatted();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e) => await SaveIfDirtyAsync();

    private async void OnSaveAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await SaveIfDirtyAsync();
    }

    private async void OnOpenInNewWindowClick(object sender, RoutedEventArgs e)
    {
        await SaveIfDirtyAsync();
        if (EntryId is { } id)
        {
            EntryWindow.Open(id);
            OpenedInNewWindow?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Title = "Delete this entry?",
            Content = $"\"{ViewModel.EntryTitle}\" will be removed from your journal. This can't be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Keep it",
            DefaultButton = ContentDialogButton.Close,
        };
        AutomationProperties.SetAutomationId(dialog, "DeleteEntryDialog");

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteAsync();
            Deleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
