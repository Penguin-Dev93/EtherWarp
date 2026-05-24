using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EtherWarp.Models;
using EtherWarp.Services;

namespace EtherWarp.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly PresetStorageService _storage;

    [ObservableProperty] private ObservableCollection<NetworkPreset> _presets = [];
    [ObservableProperty] private NetworkPreset? _selectedPreset;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private bool _isNewPreset = false;

    // Form fields
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formIP = string.Empty;
    [ObservableProperty] private string _formSubnet = string.Empty;
    [ObservableProperty] private string _formGateway = string.Empty;
    [ObservableProperty] private string _formPrimaryDNS = string.Empty;
    [ObservableProperty] private string _formSecondaryDNS = string.Empty;

    // Validation errors
    [ObservableProperty] private string _errorName = string.Empty;
    [ObservableProperty] private string _errorIP = string.Empty;
    [ObservableProperty] private string _errorSubnet = string.Empty;
    [ObservableProperty] private string _errorGateway = string.Empty;
    [ObservableProperty] private string _errorPrimaryDNS = string.Empty;
    [ObservableProperty] private string _errorSecondaryDNS = string.Empty;

    public ConfigViewModel(PresetStorageService storage)
    {
        _storage = storage;

        var saved = _storage.LoadPresets();
        Presets = new ObservableCollection<NetworkPreset>(saved);
    }

    [RelayCommand]
    private void NewPreset()
    {
        IsNewPreset = true;
        ClearForm();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditPreset()
    {
        if (SelectedPreset is null) return;

        IsNewPreset = false;
        ClearErrors();
        FormName         = SelectedPreset.Name;
        FormIP           = SelectedPreset.IPAddress;
        FormSubnet       = SelectedPreset.SubnetMask;
        FormGateway      = SelectedPreset.Gateway      ?? string.Empty;
        FormPrimaryDNS   = SelectedPreset.PrimaryDNS   ?? string.Empty;
        FormSecondaryDNS = SelectedPreset.SecondaryDNS ?? string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedPreset is null) return;
        Presets.Remove(SelectedPreset);
        SelectedPreset = null;
        PersistPresets();
    }

    [RelayCommand]
    private void SavePreset()
    {
        var (isValid, errors) = ValidationService.ValidatePreset(
            FormName, FormIP, FormSubnet,
            FormGateway, FormPrimaryDNS, FormSecondaryDNS);

        if (!isValid)
        {
            ErrorName         = errors.GetValueOrDefault("Name", string.Empty);
            ErrorIP           = errors.GetValueOrDefault("IPAddress", string.Empty);
            ErrorSubnet       = errors.GetValueOrDefault("SubnetMask", string.Empty);
            ErrorGateway      = errors.GetValueOrDefault("Gateway", string.Empty);
            ErrorPrimaryDNS   = errors.GetValueOrDefault("PrimaryDNS", string.Empty);
            ErrorSecondaryDNS = errors.GetValueOrDefault("SecondaryDNS", string.Empty);
            return;
        }

        ClearErrors();

        if (IsNewPreset)
        {
            var preset = new NetworkPreset
            {
                Name         = FormName,
                IPAddress    = FormIP,
                SubnetMask   = FormSubnet,
                Gateway      = NullIfEmpty(FormGateway),
                PrimaryDNS   = NullIfEmpty(FormPrimaryDNS),
                SecondaryDNS = NullIfEmpty(FormSecondaryDNS)
            };
            Presets.Add(preset);
            SelectedPreset = preset;
        }
        else if (SelectedPreset is not null)
        {
            // Capture reference before collection mutation clears SelectedPreset via TwoWay binding
            var preset = SelectedPreset;
            preset.Name         = FormName;
            preset.IPAddress    = FormIP;
            preset.SubnetMask   = FormSubnet;
            preset.Gateway      = NullIfEmpty(FormGateway);
            preset.PrimaryDNS   = NullIfEmpty(FormPrimaryDNS);
            preset.SecondaryDNS = NullIfEmpty(FormSecondaryDNS);

            // Force ListBox refresh: NetworkPreset is a plain model with no INPC
            var idx = Presets.IndexOf(preset);
            if (idx >= 0)
            {
                Presets.RemoveAt(idx);
                Presets.Insert(idx, preset);
            }
            SelectedPreset = preset;
        }

        PersistPresets();
        IsEditing = false;
        ClearForm();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearForm();
    }

    private void PersistPresets() => _storage.SavePresets(Presets.ToList());

    private void ClearForm()
    {
        FormName = FormIP = FormSubnet = FormGateway =
        FormPrimaryDNS = FormSecondaryDNS = string.Empty;
        ClearErrors();
    }

    private void ClearErrors()
    {
        ErrorName = ErrorIP = ErrorSubnet =
        ErrorGateway = ErrorPrimaryDNS = ErrorSecondaryDNS = string.Empty;
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
