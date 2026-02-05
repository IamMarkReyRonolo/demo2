using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp3.Models
{
    public partial class BeneficiaryRecord : ObservableObject
    {
        [ObservableProperty] private bool isSelected;

        public int Id { get; set; }

        [ObservableProperty] private string projectName = "";
        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string lastName = "";
        [ObservableProperty] private string gender = "";
        [ObservableProperty] private string barangay = "";
        [ObservableProperty] private decimal share;
        [ObservableProperty] private string status = ""; // "Endorsed" | "Pending" | "Rejected"
    }
}
