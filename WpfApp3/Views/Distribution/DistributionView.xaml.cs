using System.ComponentModel;
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
            DataContextChanged += (_, __) => HookVm();
        }

        private void HookVm()
        {
            if (DataContext is DistributionViewModel vm)
            {
                vm.PropertyChanged -= Vm_PropertyChanged;
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DistributionViewModel vm) return;

            // When scanning turns on, focus the hidden textbox so scanner writes into it
            if (e.PropertyName == nameof(DistributionViewModel.IsScanning) && vm.IsScanning)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScanBox.Focus();
                    Keyboard.Focus(ScanBox);
                }));
            }
        }

        private void ReleaseModal_Loaded(object sender, RoutedEventArgs e)
        {
            // If already scanning, focus hidden box
            if (DataContext is DistributionViewModel vm && vm.IsScanning)
            {
                ScanBox.Focus();
                Keyboard.Focus(ScanBox);
            }
        }

        private void ScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (DataContext is DistributionViewModel vm)
            {
                vm.ScanCommand.Execute(vm.ScanInput);
                vm.ScanInput = "";

                // keep focus for next scan
                if (vm.IsScanning)
                {
                    ScanBox.Focus();
                    Keyboard.Focus(ScanBox);
                }
            }

            e.Handled = true;
        }
    }
}