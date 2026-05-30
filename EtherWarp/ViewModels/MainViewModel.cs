using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EtherWarp.Services;

namespace EtherWarp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public ConfigViewModel ConfigVM { get; }
    public InterfaceViewModel InterfaceVM { get; }
    public NetworkService NetworkService { get; }

    public MainViewModel()
    {
        var storage = new PresetStorageService();
        NetworkService = new NetworkService();

        ConfigVM = new ConfigViewModel(storage);
        InterfaceVM = new InterfaceViewModel(storage, NetworkService, ConfigVM);
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeService.ApplyTheme(value);
    }
}

internal static class ThemeService
{
    public static void ApplyTheme(bool isDark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;

        var existing = dicts.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("Theme"));

        if (existing != null)
            dicts.Remove(existing);

        dicts.Insert(0, new ResourceDictionary
        {
            Source = new Uri(
                isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml",
                UriKind.Relative)
        });
    }
}
