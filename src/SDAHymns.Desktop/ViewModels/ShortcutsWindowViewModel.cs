using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SDAHymns.Core.Services;

namespace SDAHymns.Desktop.ViewModels;

public partial class ShortcutsWindowViewModel : ObservableObject
{
    private readonly IHotKeyManager _hotKeyManager;
    private readonly Dictionary<string, KeyGesture> _originalShortcuts = new();

    [ObservableProperty]
    private ObservableCollection<ShortcutGroup> _shortcutGroups = [];

    [ObservableProperty]
    private bool _isEditMode = false;

    [ObservableProperty]
    private string _editButtonLabel = "Edit Shortcuts";

    public ShortcutsWindowViewModel(IHotKeyManager hotKeyManager)
    {
        _hotKeyManager = hotKeyManager;
        LoadShortcuts();
    }

    private void LoadShortcuts()
    {
        var categories = _hotKeyManager.GetAllCategories();
        var groups = new ObservableCollection<ShortcutGroup>();

        foreach (var category in categories)
        {
            var shortcuts = _hotKeyManager.GetShortcutsByCategory(category)
                .Select(s =>
                {
                    var display = new ShortcutDisplay(this, s.Action, s.Gesture.ToString(), s.DisplayName, s.Description ?? string.Empty);
                    _originalShortcuts[s.Action] = s.Gesture;
                    return display;
                })
                .ToList();

            groups.Add(new ShortcutGroup(category, new ObservableCollection<ShortcutDisplay>(shortcuts)));
        }

        ShortcutGroups = groups;
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
        EditButtonLabel = IsEditMode ? "View Mode" : "Edit Shortcuts";

        foreach (var group in ShortcutGroups)
        {
            foreach (var shortcut in group.Shortcuts)
            {
                shortcut.IsEditing = IsEditMode;
            }
        }
    }

    [RelayCommand]
    private void SaveChanges()
    {
        // Validate no conflicts
        var allShortcuts = ShortcutGroups.SelectMany(g => g.Shortcuts).ToList();
        var shortcuts = allShortcuts.GroupBy(s => s.Shortcut).Where(g => g.Count() > 1).ToList();

        if (shortcuts.Any())
        {
            var conflictingActions = string.Join(", ", shortcuts.First().Select(s => s.ActionName));
            // Show error - for now just return
            return;
        }

        // Apply changes to manager
        foreach (var shortcut in allShortcuts)
        {
            var gesture = KeyGesture.Parse(shortcut.Shortcut);
            _hotKeyManager.RegisterHotKey(shortcut.Action, gesture);
        }

        // Save to disk
        _hotKeyManager.SaveCustomBindings();

        // Update originals
        _originalShortcuts.Clear();
        foreach (var shortcut in allShortcuts)
        {
            _originalShortcuts[shortcut.Action] = KeyGesture.Parse(shortcut.Shortcut);
        }

        // Exit edit mode
        ToggleEditMode();
    }

    [RelayCommand]
    private void CancelChanges()
    {
        // Restore originals
        foreach (var group in ShortcutGroups)
        {
            foreach (var shortcut in group.Shortcuts)
            {
                if (_originalShortcuts.TryGetValue(shortcut.Action, out var original))
                {
                    shortcut.Shortcut = original.ToString();
                    shortcut.ConflictMessage = null;
                }
            }
        }

        // Exit edit mode
        ToggleEditMode();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _hotKeyManager.ResetToDefaults();
        _originalShortcuts.Clear();
        ShortcutGroups.Clear();
        LoadShortcuts();
    }

    public void ValidateShortcut(ShortcutDisplay changedShortcut)
    {
        // Clear all conflict messages first
        foreach (var group in ShortcutGroups)
        {
            foreach (var shortcut in group.Shortcuts)
            {
                shortcut.ConflictMessage = null;
            }
        }

        // Find duplicates
        var allShortcuts = ShortcutGroups.SelectMany(g => g.Shortcuts).ToList();
        var duplicates = allShortcuts
            .GroupBy(s => s.Shortcut)
            .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        foreach (var group in duplicates)
        {
            foreach (var shortcut in group)
            {
                var otherActions = string.Join(", ", group.Where(s => s != shortcut).Select(s => s.ActionName));
                shortcut.ConflictMessage = $"Conflicts with: {otherActions}";
            }
        }
    }
}

public record ShortcutGroup(string Category, ObservableCollection<ShortcutDisplay> Shortcuts);

public partial class ShortcutDisplay : ObservableObject
{
    private readonly ShortcutsWindowViewModel _parentViewModel;

    public string Action { get; }

    [ObservableProperty]
    private string _shortcut;

    public string ActionName { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string? _conflictMessage;

    public ShortcutDisplay(ShortcutsWindowViewModel parent, string action, string shortcut, string actionName, string description)
    {
        _parentViewModel = parent;
        Action = action;
        _shortcut = shortcut;
        ActionName = actionName;
        Description = description;
    }

    partial void OnShortcutChanged(string value)
    {
        _parentViewModel?.ValidateShortcut(this);
    }
}
