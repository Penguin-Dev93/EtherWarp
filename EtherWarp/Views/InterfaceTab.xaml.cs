using System.Windows.Controls;
using EtherWarp.ViewModels;

namespace EtherWarp.Views;

public partial class InterfaceTab : System.Windows.Controls.UserControl
{
    public InterfaceTab()
    {
        InitializeComponent();
    }

    private void AdapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm) return;
        if (sender is not System.Windows.Controls.ComboBox cb) return;
        if (cb.SelectedItem is string selected)
            mainVm.InterfaceVM.SelectedAdapterName = selected;
    }
}
