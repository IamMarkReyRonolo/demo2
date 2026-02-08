using System.Windows;
using System.Windows.Controls;
using WpfApp3.ViewModels;
using WpfApp3.ViewModels.Login;

namespace WpfApp3.Views.Login
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            if (DataContext is LoginViewModel vm)
            {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            {
                vm.Password = pb.Password;
            }
        }

        private void OnLoginSucceeded()
        {
            // Goes to your existing MainWindow.xaml
            var main = new MainWindow();
            main.Show();
            Close();
        }
    }
}
