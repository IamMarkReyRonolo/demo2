namespace WpfApp3.Models
{
    public class AllotmentRecord
    {
        public bool IsSelected { get; set; }

        public int Id { get; set; }
        public string ProjectName { get; set; } = "";
        public string Company { get; set; } = "";
        public string Department { get; set; } = "";
        public string SourceOfFund { get; set; } = "";
        public int BeneficiariesCount { get; set; }
        public decimal TotalBudget { get; set; }
    }
}
