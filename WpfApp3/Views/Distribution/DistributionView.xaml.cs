using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp3.ViewModels.Distribution;

namespace WpfApp3.Views.Distribution
{
    public partial class DistributionView : UserControl
    {
        private readonly StringBuilder _scanBuffer = new();
        private bool _hooked;

        public DistributionView()
        {
            InitializeComponent();
            Loaded += (_, __) => HookVm();
            DataContextChanged += (_, __) => HookVm();
            Unloaded += (_, __) => UnhookGlobalScan();
        }

        private void HookVm()
        {
            if (DataContext is not DistributionViewModel vm) return;

            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.PropertyChanged += Vm_PropertyChanged;

            // If modal already open, hook now
            if (vm.IsReleaseSessionOpen) HookGlobalScan();
            else UnhookGlobalScan();
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DistributionViewModel vm) return;

            if (e.PropertyName == nameof(DistributionViewModel.IsReleaseSessionOpen))
            {
                if (vm.IsReleaseSessionOpen) HookGlobalScan();
                else UnhookGlobalScan();
            }
        }

        private void HookGlobalScan()
        {
            if (_hooked) return;
            _hooked = true;

            _scanBuffer.Clear();

            // Capture typed characters (scanner acts like keyboard)
            TextCompositionManager.AddPreviewTextInputHandler(this, OnPreviewTextInput);

            // Capture Enter / Return to finalize scan
            Keyboard.AddPreviewKeyDownHandler(this, OnPreviewKeyDown);
        }

        private void UnhookGlobalScan()
        {
            if (!_hooked) return;
            _hooked = false;

            TextCompositionManager.RemovePreviewTextInputHandler(this, OnPreviewTextInput);
            Keyboard.RemovePreviewKeyDownHandler(this, OnPreviewKeyDown);

            _scanBuffer.Clear();
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (DataContext is not DistributionViewModel vm) return;
            if (!vm.IsReleaseSessionOpen) return;
            if (vm.IsConfirmReleaseOpen) return; // stop scanning while confirm modal is up

            // append scanner characters
            _scanBuffer.Append(e.Text);

            // ✅ show live in the textbox
            vm.ScanInput = _scanBuffer.ToString();

            // prevent text from being typed into other focused controls
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not DistributionViewModel vm) return;
            if (!vm.IsReleaseSessionOpen) return;
            if (vm.IsConfirmReleaseOpen) return;

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                var raw = _scanBuffer.ToString().Trim();
                _scanBuffer.Clear();

                // show final scan in textbox
                vm.ScanInput = raw;

                if (!string.IsNullOrWhiteSpace(raw))
                    vm.ScanCommand.Execute(raw);

                e.Handled = true;
            }
        }
    }
}