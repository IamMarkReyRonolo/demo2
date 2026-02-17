using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace WpfApp3.Models
{
    public partial class BeneficiaryRecord : ObservableObject
    {
        [ObservableProperty] private bool isSelected;

        public int Id { get; set; } // beneficiaries.id

        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string lastName = "";
        [ObservableProperty] private string gender = "";
        [ObservableProperty] private string barangay = "";

        // assigned share (nullable)
        [ObservableProperty] private decimal? shareAmount;
        [ObservableProperty] private int? shareQty;
        [ObservableProperty] private string? shareUnit;

        public string ShareText
        {
            get
            {
                if (ShareAmount.HasValue && ShareAmount.Value > 0)
                    return $"₱ {ShareAmount.Value.ToString("N0", CultureInfo.InvariantCulture)}";

                if (ShareQty.HasValue && ShareQty.Value > 0)
                    return $"{ShareQty.Value} {(ShareUnit ?? "").Trim()}".Trim();

                return "";
            }
        }

        partial void OnShareAmountChanged(decimal? value) => OnPropertyChanged(nameof(ShareText));
        partial void OnShareQtyChanged(int? value) => OnPropertyChanged(nameof(ShareText));
        partial void OnShareUnitChanged(string? value) => OnPropertyChanged(nameof(ShareText));
    }
}
