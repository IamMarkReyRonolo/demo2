using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.ViewModels.Allotment
{
    public partial class AllotmentViewModel : ObservableObject
    {
        private readonly AllotmentsRepository _repo = new();
        private readonly System.Collections.Generic.List<AllotmentRecord> _all = new();

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

        private int? _editingId;
        private int? _deleteId;

        [ObservableProperty] private string deleteMessage = "";

        // ===== FORM FIELDS =====
        [ObservableProperty] private string projectNameInput = "";
        [ObservableProperty] private string companyInput = "";
        [ObservableProperty] private string? departmentInput;
        [ObservableProperty] private string? sourceOfFundInput;
        [ObservableProperty] private string beneficiariesInput = "";

        // NEW: budget inputs
        [ObservableProperty] private string? budgetTypeInput = "Money"; // Money | InKind
        [ObservableProperty] private string budgetAmountInput = "";     // money
        [ObservableProperty] private string budgetQtyInput = "";        // in-kind
        [ObservableProperty] private string budgetUnitInput = "";       // in-kind

        // dropdown options (dummy)
        public ObservableCollection<string> Departments { get; } = new()
        {
            "Operations", "Finance", "Health", "Admin"
        };

        public ObservableCollection<string> SourcesOfFund { get; } = new()
        {
            "Admin", "Donation"
        };

        public ObservableCollection<string> BudgetTypes { get; } = new()
        {
            "Money", "InKind"
        };

        public AllotmentViewModel()
        {
            _repo.EnsureTable();
            ReloadFromDb();
            Apply();
        }

        private void ReloadFromDb()
        {
            _all.Clear();
            _all.AddRange(_repo.GetAll());
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

        private System.Collections.Generic.List<AllotmentRecord> Filtered()
        {
            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
                return _all.ToList();

            return _all.Where(x =>
                    (x.ProjectName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Company ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Department ?? "").ToLowerInvariant().Contains(q) ||
                    (x.SourceOfFund ?? "").ToLowerInvariant().Contains(q) ||
                    (x.BudgetDisplay ?? "").ToLowerInvariant().Contains(q) ||
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

        // ===== COMMANDS =====

        [RelayCommand]
        private void AddAllotment()
        {
            _editingId = null;
            FormTitle = "Add Allotment";

            ProjectNameInput = "";
            CompanyInput = "";
            DepartmentInput = null;
            SourceOfFundInput = null;
            BeneficiariesInput = "";

            BudgetTypeInput = "Money";
            BudgetAmountInput = "";
            BudgetQtyInput = "";
            BudgetUnitInput = "";

            IsFormOpen = true;
        }

        [RelayCommand]
        private void Edit(AllotmentRecord? row)
        {
            if (row is null) return;

            _editingId = row.Id;
            FormTitle = "Edit Allotment";

            ProjectNameInput = row.ProjectName;
            CompanyInput = row.Company;
            DepartmentInput = row.Department;
            SourceOfFundInput = row.SourceOfFund;
            BeneficiariesInput = row.BeneficiariesCount.ToString(CultureInfo.InvariantCulture);

            BudgetTypeInput = string.Equals(row.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase) ? "InKind" : "Money";
            BudgetAmountInput = (row.BudgetAmount ?? 0m).ToString("N2", CultureInfo.InvariantCulture);
            BudgetQtyInput = (row.BudgetQty ?? 0).ToString(CultureInfo.InvariantCulture);
            BudgetUnitInput = row.BudgetUnit ?? "";

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
            if (!int.TryParse((BeneficiariesInput ?? "").Trim(), out var ben))
                ben = 0;

            var project = (ProjectNameInput ?? "").Trim();
            var company = (CompanyInput ?? "").Trim();
            var dept = (DepartmentInput ?? "").Trim();
            var source = (SourceOfFundInput ?? "").Trim();

            if (string.IsNullOrWhiteSpace(project)) project = "Untitled Project";
            if (string.IsNullOrWhiteSpace(company)) company = "Unknown";
            if (string.IsNullOrWhiteSpace(dept)) dept = "Operations";
            if (string.IsNullOrWhiteSpace(source)) source = "Admin";

            var type = (BudgetTypeInput ?? "Money").Trim();
            type = type.Equals("InKind", StringComparison.OrdinalIgnoreCase) ? "InKind" : "Money";

            decimal? amount = null;
            int? qty = null;
            string unit = "";

            if (type == "Money")
            {
                var raw = (BudgetAmountInput ?? "").Replace(",", "").Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var m))
                    amount = m;
                else
                    amount = 0m;
            }
            else
            {
                if (int.TryParse((BudgetQtyInput ?? "").Trim(), out var q))
                    qty = q;
                else
                    qty = 0;

                unit = (BudgetUnitInput ?? "").Trim();
            }

            var rec = new AllotmentRecord
            {
                Id = _editingId ?? 0,
                ProjectName = project,
                Company = company,
                Department = dept,
                SourceOfFund = source,
                BeneficiariesCount = ben,
                BudgetType = type,
                BudgetAmount = amount,
                BudgetQty = qty,
                BudgetUnit = unit
            };

            if (_editingId is null)
            {
                var newId = _repo.Insert(rec);
                rec.Id = newId;
            }
            else
            {
                _repo.Update(rec);
            }

            IsFormOpen = false;

            ReloadFromDb();
            Apply();
        }

        [RelayCommand]
        private void Delete(AllotmentRecord? row)
        {
            if (row is null) return;

            _deleteId = row.Id;
            DeleteMessage = $"Are you sure you want to delete allotment, {row.ProjectName}? This action cannot be undone.";
            IsDeleteOpen = true;
        }

        [RelayCommand]
        private void CancelDelete()
        {
            IsDeleteOpen = false;
            _deleteId = null;
        }

        [RelayCommand]
        private void ConfirmDelete()
        {
            if (_deleteId is not null)
                _repo.Delete(_deleteId.Value);

            IsDeleteOpen = false;
            _deleteId = null;

            ReloadFromDb();
            Apply();
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }
    }
}
