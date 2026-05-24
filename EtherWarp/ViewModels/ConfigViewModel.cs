using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EtherWarp.Models;
using EtherWarp.Services;

namespace EtherWarp.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly PresetStorageService _storage;
    private readonly NetworkService _networkService;

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
    [ObservableProperty] private string _formAdapterName = string.Empty;

    // Stable collection reference — never replaced, only mutated in-place.
    // Using [ObservableProperty] would generate a setter that replaces the reference,
    // causing WPF to tear down the ItemsSource binding and clear SelectedItem.
    private readonly ObservableCollection<string> _availableAdapters = new();
    public ObservableCollection<string> AvailableAdapters => _availableAdapters;

    // Validation errors
    [ObservableProperty] private string _errorName = string.Empty;
    [ObservableProperty] private string _errorAdapterName = string.Empty;
    [ObservableProperty] private string _errorIP = string.Empty;
    [ObservableProperty] private string _errorSubnet = string.Empty;
    [ObservableProperty] private string _errorGateway = string.Empty;
    [ObservableProperty] private string _errorPrimaryDNS = string.Empty;
    [ObservableProperty] private string _errorSecondaryDNS = string.Empty;

    public ConfigViewModel(PresetStorageService storage, NetworkService networkService)
    {
        _storage = storage;
        _networkService = networkService;

        var saved = _storage.LoadPresets();
        Presets = new ObservableCollection<NetworkPreset>(saved);
        RefreshAdapters();
    }

    [RelayCommand]
    private void NewPreset()
    {
        IsNewPreset = true;
        // Populate (clear) fields and refresh adapters while the form is still
        // Collapsed, so all bindings are current before IsEditing=true renders it.
        ClearForm();
        RefreshAdapters();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditPreset()
    {
        if (SelectedPreset is null) return;

        IsNewPreset = false;
        ClearErrors();
        // Refresh adapters and populate every form field while the form is still
        // Collapsed.  This guarantees that FormAdapterName transitions from the
        // cleared-empty state to the preset's adapter name, which always fires
        // PropertyChanged.  The ComboBox therefore has an up-to-date SelectedItem
        // before IsEditing=true makes the form Visible — preventing the TwoWay
        // binding from pushing null back after the first render.
        RefreshAdapters();
        FormName         = SelectedPreset.Name;
        FormAdapterName  = SelectedPreset.AdapterName;
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
            FormName, FormAdapterName, FormIP, FormSubnet,
            FormGateway, FormPrimaryDNS, FormSecondaryDNS);

        if (!isValid)
        {
            ErrorName         = errors.GetValueOrDefault("Name", string.Empty);
            ErrorAdapterName  = errors.GetValueOrDefault("AdapterName", string.Empty);
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
                AdapterName  = FormAdapterName,
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
            preset.AdapterName  = FormAdapterName;
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
        // Collapse the form first so the user never sees the fields clear, then
        // reset form state.  On the next EditPreset call this guarantees that every
        // field — including FormAdapterName — starts from empty, so the assignment
        // from the preset always produces a value-change that fires PropertyChanged,
        // giving the ComboBox a binding notification before the form becomes Visible.
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

    private void RefreshAdapters()
    {
        // Capture the current selection before mutating the collection.
        // In-place mutation avoids the WPF ItemsSource-replacement race where replacing
        // the collection reference causes the ComboBox to clear SelectedItem and push
        // null through the TwoWay binding before the new items are indexed.
        var current = FormAdapterName;
        var fresh = _networkService.GetPhysicalAdapterNames();

        var toRemove = _availableAdapters.Except(fresh).ToList();
        var toAdd    = fresh.Except(_availableAdapters).ToList();

        foreach (var item in toRemove) _availableAdapters.Remove(item);
        foreach (var item in toAdd)    _availableAdapters.Add(item);

        // If the previously selected adapter is still in the list, keep it selected.
        // The in-place mutation above may still briefly clear SelectedItem for items
        // that were removed and re-added, so we restore explicitly.
        if (!string.IsNullOrEmpty(current) && _availableAdapters.Contains(current))
            FormAdapterName = current;
    }

    private void ClearForm()
    {
        FormName = FormIP = FormSubnet = FormGateway =
        FormPrimaryDNS = FormSecondaryDNS = FormAdapterName = string.Empty;
        ClearErrors();
    }

    private void ClearErrors()
    {
        ErrorName = ErrorAdapterName = ErrorIP = ErrorSubnet =
        ErrorGateway = ErrorPrimaryDNS = ErrorSecondaryDNS = string.Empty;
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
