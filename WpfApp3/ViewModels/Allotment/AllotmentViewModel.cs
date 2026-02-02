using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WpfApp3.Models;

namespace WpfApp3.ViewModels.Allotment
{
    public partial class AllotmentViewModel : ObservableObject
    {
        private readonly List<AllotmentRecord> _all = new();

        // table/search/paging
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private int currentPage = 1;

        public int PageSize { get; } = 8;

        public ObservableCollection<AllotmentRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();

        public int TotalRecords => Filtered().Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        // ===== MODALS STATE =====
        [ObservableProperty] private bool isFormOpen;
        [ObservableProperty] private bool isDeleteOpen;

        [ObservableProperty] private string formTitle = "Add Allotment";

        private AllotmentRecord? _editingTarget;
        private AllotmentRecord? _deleteTarget;

        [ObservableProperty] private string deleteMessage = "";

        // ===== FORM FIELDS =====
        [ObservableProperty] private string projectNameInput = "";
        [ObservableProperty] private string companyInput = "";
        [ObservableProperty] private string? departmentInput;
        [ObservableProperty] private string? sourceOfFundInput;
        [ObservableProperty] private string beneficiariesInput = "";
        [ObservableProperty] private string totalBudgetInput = "";

        // dropdown options (dummy)
        public ObservableCollection<string> Departments { get; } = new()
        {
            "Operations", "Finance", "Health", "Admin"
        };

        public ObservableCollection<string> SourcesOfFund { get; } = new()
        {
            "Admin", "Donation"
        };

        public AllotmentViewModel()
        {
            SeedDummy();
            Apply();
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            Apply();
        }

        partial void OnCurrentPageChanged(int value)
        {
            Apply();
        }

        private List<AllotmentRecord> Filtered()
        {
            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
                return _all.ToList();

            return _all.Where(x =>
                    x.ProjectName.ToLowerInvariant().Contains(q) ||
                    x.Company.ToLowerInvariant().Contains(q) ||
                    x.Department.ToLowerInvariant().Contains(q) ||
                    x.SourceOfFund.ToLowerInvariant().Contains(q) ||
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q))
                .ToList();
        }

        private void Apply()
        {
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Items.Clear();
            foreach (var it in Filtered()
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize))
            {
                Items.Add(it);
            }

            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++) PageNumbers.Add(i);

            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(FoundText));
        }

        private void SeedDummy()
        {
            _all.AddRange(new[]
            {
                new AllotmentRecord { Id=1, ProjectName="School Supplies Drive", Company="BrightFuture", Department="Operations", SourceOfFund="Admin", BeneficiariesCount=100, TotalBudget=100000m },
                new AllotmentRecord { Id=2, ProjectName="Scholarship Grants", Company="BrightFuture", Department="Operations", SourceOfFund="Admin", BeneficiariesCount=150, TotalBudget=1000000m },
                new AllotmentRecord { Id=3, ProjectName="PWD Assistance", Company="BrightFuture", Department="Operations", SourceOfFund="Donation", BeneficiariesCount=100, TotalBudget=500000m },
                new AllotmentRecord { Id=4, ProjectName="Farmers' Seed", Company="SafeHome PH", Department="Finance", SourceOfFund="Donation", BeneficiariesCount=100, TotalBudget=500000m },
                new AllotmentRecord { Id=5, ProjectName="Emergency Shelter", Company="SafeHome PH", Department="Finance", SourceOfFund="Admin", BeneficiariesCount=100, TotalBudget=500000m },
                new AllotmentRecord { Id=6, ProjectName="Community Pantry", Company="SafeHome PH", Department="Finance", SourceOfFund="Admin", BeneficiariesCount=200, TotalBudget=200000m },
                new AllotmentRecord { Id=7, ProjectName="Coastal Clean-Up", Company="SafeHome PH", Department="Operations", SourceOfFund="Donation", BeneficiariesCount=150, TotalBudget=50000m },
                new AllotmentRecord { Id=8, ProjectName="Water Filter", Company="SafeHome PH", Department="Operations", SourceOfFund="Admin", BeneficiariesCount=100, TotalBudget=500000m },
            });
        }

        // ===== COMMANDS =====

        // Add button
        [RelayCommand]
        private void AddAllotment()
        {
            _editingTarget = null;
            FormTitle = "Add Allotment";

            ProjectNameInput = "";
            CompanyInput = "";
            DepartmentInput = null;
            SourceOfFundInput = null;
            BeneficiariesInput = "";
            TotalBudgetInput = "";

            IsFormOpen = true;
        }

        // Pencil in row -> open edit modal
        [RelayCommand]
        private void Edit(AllotmentRecord? row)
        {
            if (row is null) return;

            _editingTarget = row;
            FormTitle = "Edit Allotment";

            ProjectNameInput = row.ProjectName;
            CompanyInput = row.Company;
            DepartmentInput = row.Department;
            SourceOfFundInput = row.SourceOfFund;
            BeneficiariesInput = row.BeneficiariesCount.ToString(CultureInfo.InvariantCulture);
            TotalBudgetInput = row.TotalBudget.ToString("N0", CultureInfo.InvariantCulture);

            IsFormOpen = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormOpen = false;
        }

        [RelayCommand]
        private void SaveForm()
        {
            // basic parsing
            if (!int.TryParse((BeneficiariesInput ?? "").Trim(), out var ben))
                ben = 0;

            // allow commas in money input
            var moneyRaw = (TotalBudgetInput ?? "").Replace(",", "").Trim();
            if (!decimal.TryParse(moneyRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var budget))
                budget = 0m;

            // simple required defaults (no validation UI yet)
            var project = (ProjectNameInput ?? "").Trim();
            var company = (CompanyInput ?? "").Trim();
            var dept = (DepartmentInput ?? "").Trim();
            var source = (SourceOfFundInput ?? "").Trim();

            if (string.IsNullOrWhiteSpace(project)) project = "Untitled Project";
            if (string.IsNullOrWhiteSpace(company)) company = "Unknown";
            if (string.IsNullOrWhiteSpace(dept)) dept = "Operations";
            if (string.IsNullOrWhiteSpace(source)) source = "Admin";

            if (_editingTarget is null)
            {
                // create new
                var nextId = _all.Count == 0 ? 1 : _all.Max(x => x.Id) + 1;

                _all.Add(new AllotmentRecord
                {
                    Id = nextId,
                    ProjectName = project,
                    Company = company,
                    Department = dept,
                    SourceOfFund = source,
                    BeneficiariesCount = ben,
                    TotalBudget = budget
                });
            }
            else
            {
                // update existing
                _editingTarget.ProjectName = project;
                _editingTarget.Company = company;
                _editingTarget.Department = dept;
                _editingTarget.SourceOfFund = source;
                _editingTarget.BeneficiariesCount = ben;
                _editingTarget.TotalBudget = budget;
            }

            IsFormOpen = false;
            Apply();
        }

        // Trash in row -> open delete confirm modal
        [RelayCommand]
        private void Delete(AllotmentRecord? row)
        {
            if (row is null) return;

            _deleteTarget = row;
            DeleteMessage = $"Are you sure you want to delete allotment, {row.ProjectName}? This action cannot be undone.";
            IsDeleteOpen = true;
        }

        [RelayCommand]
        private void CancelDelete()
        {
            IsDeleteOpen = false;
            _deleteTarget = null;
        }

        [RelayCommand]
        private void ConfirmDelete()
        {
            if (_deleteTarget is not null)
            {
                _all.Remove(_deleteTarget);
            }

            IsDeleteOpen = false;
            _deleteTarget = null;
            Apply();
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }
    }
}
