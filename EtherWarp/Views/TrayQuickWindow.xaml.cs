using System.Windows;
using System.Windows.Media;
using EtherWarp.Models;
using EtherWarp.Services;
using EtherWarp.ViewModels;

namespace EtherWarp.Views;

public partial class TrayQuickWindow : Window
{
    private readonly Window _mainWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly NetworkService _networkService;
    private bool _isBusy;

    public TrayQuickWindow(
        Window mainWindow,
        MainViewModel mainViewModel,
        NetworkService networkService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _mainViewModel = mainViewModel;
        _networkService = networkService;
    }

    public void ShowNearCursor()
    {
        RefreshOptions();
        PositionNearCursor();
        Show();
        Activate();
    }

    private void RefreshOptions()
    {
        var selectedAdapter = AdapterComboBox.SelectedItem as string;
        var selectedPreset = PresetListBox.SelectedItem as NetworkPreset;

        var adapters = _networkService.GetPhysicalAdapterNames();
        AdapterComboBox.ItemsSource = adapters;
        AdapterComboBox.SelectedItem =
            selectedAdapter is not null && adapters.Contains(selectedAdapter)
                ? selectedAdapter
                : adapters.FirstOrDefault();

        var presets = _mainViewModel.ConfigVM.Presets.OrderBy(p => p.Name).ToList();
        PresetListBox.ItemsSource = presets;
        PresetListBox.DisplayMemberPath = nameof(NetworkPreset.Name);
        PresetListBox.SelectedItem = selectedPreset is not null
            ? presets.FirstOrDefault(p => p.Id == selectedPreset.Id)
            : presets.FirstOrDefault();

        UpdateButtonState();
        SetStatus(adapters.Count == 0
            ? "No physical adapters found."
            : presets.Count == 0
                ? "No saved presets found."
                : "Ready",
            false);
    }

    private void PositionNearCursor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var dpi = VisualTreeHelper.GetDpi(this);
        var cursorX = cursor.X / dpi.DpiScaleX;
        var cursorY = cursor.Y / dpi.DpiScaleY;

        var screen = System.Windows.Forms.Screen.FromPoint(cursor);
        var workArea = screen.WorkingArea;
        var workLeft = workArea.Left / dpi.DpiScaleX;
        var workTop = workArea.Top / dpi.DpiScaleY;
        var workRight = workArea.Right / dpi.DpiScaleX;
        var workBottom = workArea.Bottom / dpi.DpiScaleY;
        var left = cursorX - Width + 16;
        var top = cursorY - Height - 16;

        if (left < workLeft)
            left = workLeft + 8;
        if (top < workTop)
            top = workTop + 8;
        if (left + Width > workRight)
            left = workRight - Width - 8;
        if (top + Height > workBottom)
            top = workBottom - Height - 8;

        Left = left;
        Top = top;
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (PresetListBox.SelectedItem is not NetworkPreset preset ||
            AdapterComboBox.SelectedItem is not string adapterName)
        {
            return;
        }

        await RunNetworkActionAsync(
            $"Applying '{preset.Name}' to '{adapterName}'...",
            () => _networkService.ApplyPresetAsync(preset, adapterName));
    }

    private async void DhcpButton_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterComboBox.SelectedItem is not string adapterName)
            return;

        await RunNetworkActionAsync(
            $"Resetting '{adapterName}' to DHCP...",
            () => _networkService.ResetToDHCPAsync(adapterName));
    }

    private async Task RunNetworkActionAsync(
        string startMessage,
        Func<Task<(bool Success, string Message)>> action)
    {
        if (_isBusy)
            return;

        _isBusy = true;
        UpdateButtonState();
        SetStatus(startMessage, false);

        try
        {
            var result = await action();
            SetStatus(result.Message, result.Success);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
        }
        finally
        {
            _isBusy = false;
            UpdateButtonState();
        }
    }

    private void SetStatus(string message, bool success)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = success
            ? (System.Windows.Media.Brush)FindResource("SuccessColor")
            : (System.Windows.Media.Brush)FindResource("TextSecondary");
    }

    private void UpdateButtonState()
    {
        ApplyButton.IsEnabled =
            !_isBusy &&
            AdapterComboBox.SelectedItem is string &&
            PresetListBox.SelectedItem is NetworkPreset;

        DhcpButton.IsEnabled =
            !_isBusy &&
            AdapterComboBox.SelectedItem is string;
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();

        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void AdapterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateButtonState();
    }

    private void PresetListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateButtonState();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!_isBusy)
            Hide();
    }
}
