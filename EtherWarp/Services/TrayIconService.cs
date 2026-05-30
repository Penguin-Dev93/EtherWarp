using System.Drawing;
using System.Windows;
using EtherWarp.Models;
using EtherWarp.ViewModels;
using WinForms = System.Windows.Forms;

namespace EtherWarp.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly NetworkService _networkService;
    private readonly WinForms.NotifyIcon _notifyIcon;

    private WinForms.ContextMenuStrip? _menu;
    private bool _isDisposed;
    private bool _isBusy;

    public TrayIconService(Window mainWindow, MainViewModel mainViewModel, NetworkService networkService)
    {
        _mainWindow = mainWindow;
        _mainViewModel = mainViewModel;
        _networkService = networkService;

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "EtherWarp",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        BuildMenu();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu?.Dispose();
    }

    private void BuildMenu()
    {
        var oldMenu = _menu;
        var menu = new WinForms.ContextMenuStrip();

        var title = new WinForms.ToolStripMenuItem("EtherWarp") { Enabled = false };
        menu.Items.Add(title);
        menu.Items.Add(new WinForms.ToolStripSeparator());

        menu.Items.Add(CreateMenuItem("Open", (_, _) => ShowMainWindow()));

        var applyPresetMenu = new WinForms.ToolStripMenuItem("Apply Preset");
        PopulateApplyPresetMenu(applyPresetMenu);
        applyPresetMenu.DropDownOpening += (_, _) =>
        {
            applyPresetMenu.DropDownItems.Clear();
            PopulateApplyPresetMenu(applyPresetMenu);
        };
        menu.Items.Add(applyPresetMenu);

        menu.Items.Add(CreateMenuItem("Refresh Adapters", (_, _) => BuildMenu()));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(CreateMenuItem("Exit", (_, _) => System.Windows.Application.Current.Shutdown()));

        _menu = menu;
        _notifyIcon.ContextMenuStrip = menu;
        oldMenu?.Dispose();
    }

    private void PopulateApplyPresetMenu(WinForms.ToolStripMenuItem applyPresetMenu)
    {
        var adapters = _networkService.GetPhysicalAdapterNames();
        var presets = _mainViewModel.ConfigVM.Presets.OrderBy(p => p.Name).ToList();

        if (adapters.Count == 0)
        {
            applyPresetMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("No physical adapters found")
            {
                Enabled = false
            });
            return;
        }

        foreach (var adapterName in adapters)
        {
            var adapterMenu = new WinForms.ToolStripMenuItem(adapterName);

            if (presets.Count == 0)
            {
                adapterMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("No saved presets")
                {
                    Enabled = false
                });
            }
            else
            {
                foreach (var preset in presets)
                {
                    adapterMenu.DropDownItems.Add(CreateMenuItem(
                        preset.Name,
                        async (_, _) => await ApplyPresetAsync(preset, adapterName)));
                }

                adapterMenu.DropDownItems.Add(new WinForms.ToolStripSeparator());
            }

            adapterMenu.DropDownItems.Add(CreateMenuItem(
                "DHCP / Automatic",
                async (_, _) => await ResetToDhcpAsync(adapterName)));

            applyPresetMenu.DropDownItems.Add(adapterMenu);
        }
    }

    private static WinForms.ToolStripMenuItem CreateMenuItem(
        string text,
        EventHandler onClick)
    {
        var item = new WinForms.ToolStripMenuItem(text);
        item.Click += onClick;
        return item;
    }

    private async Task ApplyPresetAsync(NetworkPreset preset, string adapterName)
    {
        if (_isBusy)
            return;

        await RunTrayActionAsync(
            $"Applying '{preset.Name}' to '{adapterName}'...",
            () => _networkService.ApplyPresetAsync(preset, adapterName));
    }

    private async Task ResetToDhcpAsync(string adapterName)
    {
        if (_isBusy)
            return;

        await RunTrayActionAsync(
            $"Resetting '{adapterName}' to DHCP...",
            () => _networkService.ResetToDHCPAsync(adapterName));
    }

    private async Task RunTrayActionAsync(
        string startMessage,
        Func<Task<(bool Success, string Message)>> action)
    {
        _isBusy = true;
        SetMenuEnabled(false);
        ShowBalloon("EtherWarp", startMessage, WinForms.ToolTipIcon.Info);

        try
        {
            var result = await action();
            ShowBalloon(
                result.Success ? "EtherWarp" : "EtherWarp Error",
                result.Message,
                result.Success ? WinForms.ToolTipIcon.Info : WinForms.ToolTipIcon.Error);
        }
        catch (Exception ex)
        {
            ShowBalloon("EtherWarp Error", ex.Message, WinForms.ToolTipIcon.Error);
        }
        finally
        {
            _isBusy = false;
            BuildMenu();
        }
    }

    private void SetMenuEnabled(bool enabled)
    {
        if (_menu is null)
            return;

        foreach (WinForms.ToolStripItem item in _menu.Items)
            item.Enabled = enabled;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void ShowBalloon(string title, string message, WinForms.ToolTipIcon icon)
    {
        _notifyIcon.ShowBalloonTip(4000, title, message, icon);
    }

    private static Icon LoadIcon()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var icon = Icon.ExtractAssociatedIcon(processPath);
                if (icon is not null)
                    return icon;
            }
        }
        catch
        {
        }

        return SystemIcons.Application;
    }
}
