using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EtherWarp.ViewModels;

namespace EtherWarp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        SourceInitialized += (_, _) => ApplyDarkWindowFrame();
    }

    private void ApplyDarkWindowFrame()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var darkMode = 1;
        DwmSetWindowAttribute(hwnd, DwmWindowAttribute.UseImmersiveDarkMode, ref darkMode, sizeof(int));

        var borderColor = ToColorRef(0x13, 0x14, 0x2B);
        DwmSetWindowAttribute(hwnd, DwmWindowAttribute.BorderColor, ref borderColor, sizeof(int));

        var captionColor = ToColorRef(0x13, 0x14, 0x2B);
        DwmSetWindowAttribute(hwnd, DwmWindowAttribute.CaptionColor, ref captionColor, sizeof(int));
    }

    private static int ToColorRef(byte r, byte g, byte b) => r | (g << 8) | (b << 16);

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private enum DwmWindowAttribute
    {
        UseImmersiveDarkMode = 20,
        BorderColor = 34,
        CaptionColor = 35
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
