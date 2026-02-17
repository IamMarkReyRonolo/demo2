using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.ViewModels.Beneficiaries
{
    public partial class BeneficiariesViewModel : ObservableObject
    {
        private readonly AllotmentsRepository _allotmentsRepo = new();
        private readonly BeneficiariesRepository _benefRepo = new();
        private readonly AllotmentBeneficiariesRepository _assignRepo = new();

        // paging
        [ObservableProperty] private int currentPage = 1;
        public int PageSize { get; } = 8;

        // projects
        public ObservableCollection<AllotmentProjectOption> Projects { get; } = new();
        [ObservableProperty] private AllotmentProjectOption? selectedProject;

        // main table
        public ObservableCollection<BeneficiaryRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();

        public int TotalRecords => _cachedAssigned.Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        // budget text shows PROJECT budget (not sum of shares)
        public string TotalBudgetText
        {
            get
            {
                var p = SelectedProject;
                if (p is null) return "Total Budget: -";

                if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
                    return $"Total Budget: {p.BudgetQty:N0} {p.BudgetUnit}".Trim();

                return $"Total Budget: ₱ {p.BudgetAmount:N2}";
            }
        }

        // ---------------- MODALS ----------------
        [ObservableProperty] private bool isProjectDetailsOpen;
        [ObservableProperty] private bool isAddBeneficiariesOpen;
        [ObservableProperty] private bool isEditShareOpen;
        [ObservableProperty] private bool isRemoveOpen;

        // Project details fields (from allotment)
        [ObservableProperty] private string projectNameDetails = "";
        [ObservableProperty] private string companyDetails = "";
        [ObservableProperty] private string departmentDetails = "";
        [ObservableProperty] private string sourceOfFundDetails = "";
        [ObservableProperty] private string totalBudgetDetails = "";

        // Add modal
        [ObservableProperty] private string addSearchText = "";
        public ObservableCollection<BeneficiaryRecord> AddItems { get; } = new();

        [ObservableProperty] private bool isAddAllSelected;

        // Add share inputs + errors
        [ObservableProperty] private string addShareAmountInput = "";
        [ObservableProperty] private string addShareQtyInput = "";
        [ObservableProperty] private string addShareUnitInput = "";

        [ObservableProperty] private string addShareAmountError = "";
        [ObservableProperty] private bool hasAddShareAmountError;

        [ObservableProperty] private string addShareInKindError = "";
        [ObservableProperty] private bool hasAddShareInKindError;

        public int AddSelectedCount => AddItems.Count(x => x.IsSelected);
        public string AddButtonText => $"Add {AddSelectedCount}";
        public string AddFoundText => $"Found {AddItems.Count} records";

        public bool CanConfirmAddSelected =>
            AddSelectedCount > 0 &&
            !HasAddShareAmountError &&
            !HasAddShareInKindError &&
            ShareInputsArePresent();

        // Edit share modal
        private BeneficiaryRecord? _editTarget;

        [ObservableProperty] private string editShareAmountInput = "";
        [ObservableProperty] private string editShareQtyInput = "";
        [ObservableProperty] private string editShareUnitInput = "";

        [ObservableProperty] private string editShareAmountError = "";
        [ObservableProperty] private bool hasEditShareAmountError;

        [ObservableProperty] private string editShareInKindError = "";
        [ObservableProperty] private bool hasEditShareInKindError;

        public bool CanConfirmEditShare =>
            _editTarget is not null &&
            !HasEditShareAmountError &&
            !HasEditShareInKindError &&
            EditInputsArePresent();

        // Remove modal
        [ObservableProperty] private string removeMessage = "";
        private BeneficiaryRecord? _removeTarget;

        // cached assigned list for paging
        private List<BeneficiaryRecord> _cachedAssigned = new();

        public BeneficiariesViewModel()
        {
            LoadProjects();
        }

        partial void OnCurrentPageChanged(int value) => ApplyPaging();

        partial void OnSelectedProjectChanged(AllotmentProjectOption? value)
        {
            CurrentPage = 1;

            // default unit in add modal if in-kind project
            if (value is not null && string.Equals(value.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
            {
                AddShareUnitInput = value.BudgetUnit ?? "";
                EditShareUnitInput = value.BudgetUnit ?? "";
            }

            ReloadAssigned();
            OnPropertyChanged(nameof(TotalBudgetText));
        }

        partial void OnAddSearchTextChanged(string value) => BuildAddList();

        partial void OnIsAddAllSelectedChanged(bool value)
        {
            foreach (var r in AddItems) r.IsSelected = value;
            OnPropertyChanged(nameof(AddSelectedCount));
            OnPropertyChanged(nameof(AddButtonText));
            OnPropertyChanged(nameof(CanConfirmAddSelected));
        }

        partial void OnAddShareAmountInputChanged(string value) { ValidateAddShare(); }
        partial void OnAddShareQtyInputChanged(string value) { ValidateAddShare(); }
        partial void OnAddShareUnitInputChanged(string value) { ValidateAddShare(); }

        partial void OnEditShareAmountInputChanged(string value) { ValidateEditShare(); }
        partial void OnEditShareQtyInputChanged(string value) { ValidateEditShare(); }
        partial void OnEditShareUnitInputChanged(string value) { ValidateEditShare(); }

        private void LoadProjects()
        {
            Projects.Clear();

            var list = _allotmentsRepo.GetAll();
            foreach (var a in list)
                Projects.Add(a);

            // ✅ default to FIRST project
            SelectedProject = Projects.FirstOrDefault();

            ReloadAssigned();
        }

        private void ReloadAssigned()
        {
            Items.Clear();
            _cachedAssigned.Clear();

            var p = SelectedProject;
            if (p is null) { ApplyPaging(); return; }

            // ✅ show endorsed only (enforced by query)
            _cachedAssigned = _assignRepo.GetAssignedEndorsed(p.Id);

            ApplyPaging();

            OnPropertyChanged(nameof(FoundText));
            OnPropertyChanged(nameof(TotalBudgetText));
        }

        private void ApplyPaging()
        {
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Items.Clear();
            foreach (var it in _cachedAssigned
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize))
            {
                Items.Add(it);
            }

            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++) PageNumbers.Add(i);

            OnPropertyChanged(nameof(FoundText));
            OnPropertyChanged(nameof(TotalBudgetText));
        }

        // ---------------- Commands ----------------

        [RelayCommand]
        private void OpenProjectDetails()
        {
            var p = SelectedProject;
            if (p is null) return;

            ProjectNameDetails = p.ProjectName;
            CompanyDetails = p.Company;
            DepartmentDetails = p.Department;
            SourceOfFundDetails = p.SourceOfFund;

            TotalBudgetDetails = string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase)
                ? $"{p.BudgetQty:N0} {p.BudgetUnit}".Trim()
                : $"₱ {p.BudgetAmount:N2}";

            IsProjectDetailsOpen = true;
        }

        [RelayCommand] private void CloseProjectDetails() => IsProjectDetailsOpen = false;

        [RelayCommand]
        private void OpenAddBeneficiaries()
        {
            if (SelectedProject is null) return;

            AddSearchText = "";
            IsAddAllSelected = false;

            // reset share fields
            AddShareAmountInput = "";
            AddShareQtyInput = "";
            AddShareUnitInput = string.Equals(SelectedProject.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase)
                ? (SelectedProject.BudgetUnit ?? "")
                : "";

            ValidateAddShare();
            BuildAddList();

            IsAddBeneficiariesOpen = true;
        }

        [RelayCommand] private void CloseAddBeneficiaries() => IsAddBeneficiariesOpen = false;

        [RelayCommand]
        private void SearchAdd()
        {
            BuildAddList();
        }

        private void BuildAddList()
        {
            AddItems.Clear();

            var p = SelectedProject;
            if (p is null) return;

            // ✅ only Endorsed not yet assigned to this project
            var list = _assignRepo.GetAvailableEndorsed(p.Id, AddSearchText);

            foreach (var r in list)
            {
                r.IsSelected = false;
                r.PropertyChanged -= AddRow_PropertyChanged;
                r.PropertyChanged += AddRow_PropertyChanged;
                AddItems.Add(r);
            }

            IsAddAllSelected = false;
            OnPropertyChanged(nameof(AddSelectedCount));
            OnPropertyChanged(nameof(AddButtonText));
            OnPropertyChanged(nameof(AddFoundText));
            OnPropertyChanged(nameof(CanConfirmAddSelected));
        }

        private void AddRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BeneficiaryRecord.IsSelected))
            {
                OnPropertyChanged(nameof(AddSelectedCount));
                OnPropertyChanged(nameof(AddButtonText));
                OnPropertyChanged(nameof(CanConfirmAddSelected));

                if (AddItems.Count > 0)
                    IsAddAllSelected = AddItems.All(x => x.IsSelected);
            }
        }

        [RelayCommand]
        private void ConfirmAddSelected()
        {
            var p = SelectedProject;
            if (p is null) return;

            ValidateAddShare();
            if (!CanConfirmAddSelected) return;

            var ids = AddItems.Where(x => x.IsSelected).Select(x => x.BeneficiaryPk).ToList();
            if (ids.Count == 0) return;

            // build share values by project type
            decimal? shareAmount = null;
            int? shareQty = null;
            string? shareUnit = null;

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse((AddShareQtyInput ?? "").Trim(), out var qty);
                shareQty = qty;
                shareUnit = (AddShareUnitInput ?? "").Trim();
            }
            else
            {
                var raw = (AddShareAmountInput ?? "").Replace(",", "").Trim();
                decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt);
                shareAmount = amt;
            }

            _assignRepo.AddMany(p.Id, ids, shareAmount, shareQty, shareUnit);

            IsAddBeneficiariesOpen = false;
            ReloadAssigned();
        }

        [RelayCommand]
        private void OpenEditShare(BeneficiaryRecord? row)
        {
            if (row is null || SelectedProject is null) return;

            _editTarget = row;

            // prefill based on current stored values
            EditShareAmountInput = row.ShareAmount.HasValue ? row.ShareAmount.Value.ToString("N2", CultureInfo.InvariantCulture) : "";
            EditShareQtyInput = row.ShareQty.HasValue ? row.ShareQty.Value.ToString(CultureInfo.InvariantCulture) : "";
            EditShareUnitInput = !string.IsNullOrWhiteSpace(row.ShareUnit) ? row.ShareUnit : (SelectedProject.BudgetUnit ?? "");

            ValidateEditShare();
            IsEditShareOpen = true;
        }

        [RelayCommand]
        private void CloseEditShare()
        {
            IsEditShareOpen = false;
            _editTarget = null;
        }

        [RelayCommand]
        private void ConfirmEditShare()
        {
            var p = SelectedProject;
            if (p is null || _editTarget is null) return;

            ValidateEditShare();
            if (!CanConfirmEditShare) return;

            decimal? shareAmount = null;
            int? shareQty = null;
            string? shareUnit = null;

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse((EditShareQtyInput ?? "").Trim(), out var qty);
                shareQty = qty;
                shareUnit = (EditShareUnitInput ?? "").Trim();
            }
            else
            {
                var raw = (EditShareAmountInput ?? "").Replace(",", "").Trim();
                decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt);
                shareAmount = amt;
            }

            _assignRepo.UpdateShare(_editTarget.AssignmentId, shareAmount, shareQty, shareUnit);

            IsEditShareOpen = false;
            _editTarget = null;
            ReloadAssigned();
        }

        [RelayCommand]
        private void OpenRemove(BeneficiaryRecord? row)
        {
            if (row is null) return;
            _removeTarget = row;
            RemoveMessage = "Are you sure you want to remove this beneficiary from the selected project?";
            IsRemoveOpen = true;
        }

        [RelayCommand]
        private void CloseRemove()
        {
            IsRemoveOpen = false;
            _removeTarget = null;
        }

        [RelayCommand]
        private void ConfirmRemove()
        {
            if (_removeTarget is null) return;

            _assignRepo.Remove(_removeTarget.AssignmentId);

            IsRemoveOpen = false;
            _removeTarget = null;
            ReloadAssigned();
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }

        // ---------------- Validation ----------------

        private bool ShareInputsArePresent()
        {
            var p = SelectedProject;
            if (p is null) return false;

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(AddShareQtyInput) && !string.IsNullOrWhiteSpace(AddShareUnitInput);

            return !string.IsNullOrWhiteSpace(AddShareAmountInput);
        }

        private bool EditInputsArePresent()
        {
            var p = SelectedProject;
            if (p is null) return false;

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(EditShareQtyInput) && !string.IsNullOrWhiteSpace(EditShareUnitInput);

            return !string.IsNullOrWhiteSpace(EditShareAmountInput);
        }

        private void ValidateAddShare()
        {
            var p = SelectedProject;

            HasAddShareAmountError = false;
            AddShareAmountError = "";

            HasAddShareInKindError = false;
            AddShareInKindError = "";

            if (p is null)
            {
                HasAddShareAmountError = true;
                AddShareAmountError = "Select a project first.";
                OnPropertyChanged(nameof(CanConfirmAddSelected));
                return;
            }

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse((AddShareQtyInput ?? "").Trim(), out var qty) || qty <= 0)
                {
                    HasAddShareInKindError = true;
                    AddShareInKindError = "Share qty must be a valid number (> 0).";
                }
                else if (string.IsNullOrWhiteSpace(AddShareUnitInput))
                {
                    HasAddShareInKindError = true;
                    AddShareInKindError = "Unit is required.";
                }
            }
            else
            {
                var raw = (AddShareAmountInput ?? "").Replace(",", "").Trim();
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) || amt <= 0m)
                {
                    HasAddShareAmountError = true;
                    AddShareAmountError = "Share amount must be a valid number (> 0).";
                }
            }

            OnPropertyChanged(nameof(CanConfirmAddSelected));
        }

        private void ValidateEditShare()
        {
            var p = SelectedProject;

            HasEditShareAmountError = false;
            EditShareAmountError = "";

            HasEditShareInKindError = false;
            EditShareInKindError = "";

            if (p is null)
            {
                HasEditShareAmountError = true;
                EditShareAmountError = "Select a project first.";
                OnPropertyChanged(nameof(CanConfirmEditShare));
                return;
            }

            if (string.Equals(p.BudgetType, "InKind", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse((EditShareQtyInput ?? "").Trim(), out var qty) || qty <= 0)
                {
                    HasEditShareInKindError = true;
                    EditShareInKindError = "Share qty must be a valid number (> 0).";
                }
                else if (string.IsNullOrWhiteSpace(EditShareUnitInput))
                {
                    HasEditShareInKindError = true;
                    EditShareInKindError = "Unit is required.";
                }
            }
            else
            {
                var raw = (EditShareAmountInput ?? "").Replace(",", "").Trim();
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) || amt <= 0m)
                {
                    HasEditShareAmountError = true;
                    EditShareAmountError = "Share amount must be a valid number (> 0).";
                }
            }

            OnPropertyChanged(nameof(CanConfirmEditShare));
        }
    }
}
