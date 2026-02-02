using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp3.Models
{
    public partial class UserRecord : ObservableObject
    {
        [ObservableProperty] private bool isSelected;

        public int Id { get; set; }

        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string lastName = "";
        [ObservableProperty] private string office = "";
        [ObservableProperty] private string role = "";
        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";

        [ObservableProperty] private bool isPasswordRevealed;

        public string PasswordDisplay => IsPasswordRevealed
            ? Password
            : new string('*', Math.Max(8, Password?.Length ?? 8));

        partial void OnIsPasswordRevealedChanged(bool value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
        }

        partial void OnPasswordChanged(string value)
        {
            OnPropertyChanged(nameof(PasswordDisplay));
        }
    }
}
