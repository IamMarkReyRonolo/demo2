using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp3.Models
{
    public partial class BeneficiaryRecord : ObservableObject
    {
        [ObservableProperty] private bool isSelected;

        public int Id { get; set; }

        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string lastName = "";
        [ObservableProperty] private string gender = "";
        [ObservableProperty] private string barangay = "";

        // ✅ NEW
        [ObservableProperty] private string classification = "None";

        // share fields from allotment_beneficiaries
        [ObservableProperty] private decimal? shareAmount;
        [ObservableProperty] private int? shareQty;
        [ObservableProperty] private string? shareUnit;

        // ✅ display text used by XAML binding ShareText
        public string ShareText
        {
            get
            {
                if (ShareAmount is not null)
                    return $"₱ {ShareAmount.Value:N2}";

                if (ShareQty is not null && !string.IsNullOrWhiteSpace(ShareUnit))
                    return $"{ShareQty.Value:N0} {ShareUnit}";

                return "";
            }
        }

        // make ShareText refresh whenever underlying fields change
        partial void OnShareAmountChanged(decimal? value) => OnPropertyChanged(nameof(ShareText));
        partial void OnShareQtyChanged(int? value) => OnPropertyChanged(nameof(ShareText));
        partial void OnShareUnitChanged(string? value) => OnPropertyChanged(nameof(ShareText));
    }
}
