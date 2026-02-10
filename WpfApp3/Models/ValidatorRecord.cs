using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp3.Models
{
    public partial class ValidatorRecord : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty] private string beneficiaryId = "";
        [ObservableProperty] private string civilRegistryId = "";

        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string middleName = "";
        [ObservableProperty] private string lastName = "";

        [ObservableProperty] private string gender = "";
        [ObservableProperty] private string dateOfBirth = "";     // keep as string for now (dummy)
        [ObservableProperty] private string classification = "";

        [ObservableProperty] private string barangay = "";
        [ObservableProperty] private string presentAddress = "";

        // "", "Endorsed", "Pending", "Rejected"
        [ObservableProperty] private string status = "";

        public string FullName => $"{LastName}, {FirstName} {MiddleName}".Replace("  ", " ").Trim();
    }
}
