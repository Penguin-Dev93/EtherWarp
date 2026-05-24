using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EtherWarp.Models;
using EtherWarp.Services;

namespace EtherWarp.ViewModels;

public partial class InterfaceViewModel : ObservableObject
{
    private readonly NetworkService _networkService;
    private readonly ConfigViewModel _configVM;

    // Survives the temporary null that occurs when a preset is removed from
    // AvailablePresets mid-Remove/Insert cycle (SavePreset does RemoveAt + Insert
    // to force the ListBox to repaint a plain-model item).
    // Updated on every non-null OnSelectedPresetChanged; never cleared by SyncPresets.
    private Guid? _lastKnownSelectedId;

    // Stable collection reference — never replaced, only mutated in-place.
    // Replacing the reference would cause WPF to tear down the ItemsSource binding
    // and clear ComboBox.SelectedItem before the new items are indexed.
    private readonly ObservableCollection<NetworkPreset> _availablePresets = new();
    public ObservableCollection<NetworkPreset> AvailablePresets => _availablePresets;

    [ObservableProperty] private NetworkPreset? _selectedPreset;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _statusIsSuccess = false;

    public InterfaceViewModel(
        PresetStorageService storage,
        NetworkService networkService,
        ConfigViewModel configVM)
    {
        _networkService = networkService;
        _configVM = configVM;

        _configVM.Presets.CollectionChanged += OnPresetsChanged;
        SyncPresets();
    }

    private void OnPresetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncPresets();
    }

    private void SyncPresets()
    {
        // Prefer the live SelectedPreset; fall back to the last known Id to bridge
        // the gap between the Remove and Insert CollectionChanged events in SavePreset.
        var idToRestore = SelectedPreset?.Id ?? _lastKnownSelectedId;

        var fresh = _configVM.Presets;

        // Items in _availablePresets whose Id is no longer in the source collection
        var toRemove = _availablePresets
            .Where(p => fresh.All(f => f.Id != p.Id))
            .ToList();

        // Items in the source collection not yet in _availablePresets
        var toAdd = fresh
            .Where(f => _availablePresets.All(p => p.Id != f.Id))
            .ToList();

        // Items with matching Id but a different object reference (e.g. a deep-copy path)
        var toReplace = fresh
            .Where(f => _availablePresets.Any(p => p.Id == f.Id && !ReferenceEquals(p, f)))
            .ToList();

        // Mutate in-place — WPF reacts to individual Add/Remove/Replace events on
        // the same collection, so SelectedItem is only cleared when the selected item
        // itself is removed, not on every unrelated change.
        foreach (var item in toRemove)
            _availablePresets.Remove(item);

        foreach (var item in toAdd)
            _availablePresets.Add(item);

        foreach (var updated in toReplace)
        {
            var old = _availablePresets.First(p => p.Id == updated.Id);
            _availablePresets[_availablePresets.IndexOf(old)] = updated;
        }

        // Restore selection.  During the Remove step the preset is absent so found
        // will be null and we leave SelectedPreset null.  During the Insert step
        // idToRestore comes from _lastKnownSelectedId, finds the re-added preset,
        // and restores the selection.
        if (idToRestore.HasValue)
        {
            var found = _availablePresets.FirstOrDefault(p => p.Id == idToRestore.Value);
            if (found is not null)
                SelectedPreset = found;
            // If not found: preset was genuinely deleted — leave SelectedPreset null.
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecutePresetAsync()
    {
        if (SelectedPreset is null || IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        var result = await _networkService.ApplyPresetAsync(SelectedPreset);
        StatusMessage = result.Message;
        StatusIsSuccess = result.Success;

        IsBusy = false;
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ResetToDHCPAsync()
    {
        if (SelectedPreset is null || IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        var result = await _networkService.ResetToDHCPAsync(SelectedPreset.AdapterName);
        StatusMessage = result.Message;
        StatusIsSuccess = result.Success;

        IsBusy = false;
    }

    private bool CanExecute() => SelectedPreset != null && !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        ExecutePresetCommand.NotifyCanExecuteChanged();
        ResetToDHCPCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPresetChanged(NetworkPreset? value)
    {
        // Only update on non-null: a null here means "temporarily orphaned during
        // a Remove/Insert cycle", not "user explicitly cleared the selection".
        if (value is not null)
            _lastKnownSelectedId = value.Id;

        ExecutePresetCommand.NotifyCanExecuteChanged();
        ResetToDHCPCommand.NotifyCanExecuteChanged();
    }
}
