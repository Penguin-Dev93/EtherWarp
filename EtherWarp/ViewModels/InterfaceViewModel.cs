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
        var current = SelectedPreset?.Id;
        AvailablePresets = new ObservableCollection<NetworkPreset>(_configVM.Presets);
        if (current.HasValue)
            SelectedPreset = AvailablePresets.FirstOrDefault(p => p.Id == current);
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
        ExecutePresetCommand.NotifyCanExecuteChanged();
        ResetToDHCPCommand.NotifyCanExecuteChanged();
    }
}
