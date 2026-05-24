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

    // Survives the temporary null that occurs when AvailablePresets is replaced mid-Remove/Insert cycle.
    // Set whenever SelectedPreset changes to a non-null value; never cleared by SyncPresets itself.
    private Guid? _lastKnownSelectedId;

    [ObservableProperty] private ObservableCollection<NetworkPreset> _availablePresets = [];
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
        // Use SelectedPreset.Id if still set, otherwise fall back to _lastKnownSelectedId.
        // This handles the Remove/Insert cycle in SavePreset: the Remove call nulls out
        // SelectedPreset via the TwoWay binding before the Insert call runs, so we need
        // the field to bridge the gap between the two CollectionChanged events.
        var idToRestore = SelectedPreset?.Id ?? _lastKnownSelectedId;

        AvailablePresets = new ObservableCollection<NetworkPreset>(_configVM.Presets);
        // ^^^ Replacing ItemsSource causes WPF to clear ComboBox.SelectedItem synchronously,
        // pushing null through the TwoWay binding → SelectedPreset = null here.

        if (idToRestore.HasValue)
        {
            var found = AvailablePresets.FirstOrDefault(p => p.Id == idToRestore.Value);
            if (found is not null)
                SelectedPreset = found;
            // If not found the preset was genuinely deleted; leave SelectedPreset null.
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
        // Keep _lastKnownSelectedId current so SyncPresets can restore across the
        // Remove/Insert race. Only update on non-null: null means "temporarily orphaned",
        // not "user cleared the selection".
        if (value is not null)
            _lastKnownSelectedId = value.Id;

        ExecutePresetCommand.NotifyCanExecuteChanged();
        ResetToDHCPCommand.NotifyCanExecuteChanged();
    }
}
