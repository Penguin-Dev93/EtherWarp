using System.Windows.Controls;
using EtherWarp.ViewModels;

namespace EtherWarp.Views;

public partial class ConfigurationTab : UserControl
{
    public ConfigurationTab()
    {
        InitializeComponent();
    }

    private void AdapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm) return;
        if (sender is not ComboBox cb) return;

        // Only update on explicit user selection (string item).
        // Null selections during programmatic form resets are handled by
        // ClearForm() setting FormAdapterName directly — ignore them here.
        if (cb.SelectedItem is string selected)
            mainVm.ConfigVM.FormAdapterName = selected;
    }
}
