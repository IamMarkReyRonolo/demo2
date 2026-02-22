using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp3.ViewModels.Distribution;

namespace WpfApp3.Views.Distribution
{
    public partial class DistributionView : UserControl
    {
        public DistributionView()
        {
            InitializeComponent();
        }

        private void ReleaseModal_Loaded(object sender, RoutedEventArgs e)
        {
            ScanBox.Focus();
            Keyboard.Focus(ScanBox);
        }

        private void ScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (DataContext is DistributionViewModel vm)
            {
                vm.ScanCommand.Execute(vm.ScanInput);
                vm.ScanInput = "";
            }

            ScanBox.Focus();
            Keyboard.Focus(ScanBox);
            e.Handled = true;
        }
    }
}