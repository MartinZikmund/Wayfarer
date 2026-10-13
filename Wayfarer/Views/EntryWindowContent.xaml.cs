namespace Wayfarer.Views;

/// <summary>Root of a journal entry window: the editor on the page background.</summary>
public sealed partial class EntryWindowContent : UserControl
{
    public EntryWindowContent()
    {
        InitializeComponent();
        EntryEditor.ShowOpenInNewWindow = false;
    }

    public JournalEntryEditor Editor => EntryEditor;
}
