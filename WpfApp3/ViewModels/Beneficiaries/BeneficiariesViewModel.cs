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
        private readonly AllotmentsRepository _allotmentRepo = new();
        private readonly AllotmentBeneficiariesRepository _assignRepo = new();

        private List<BeneficiaryRecord> _assignedCache = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private int currentPage = 1;

        public int PageSize { get; } = 8;

        public ObservableCollection<AllotmentProjectOption> Projects { get; } = new();
        [ObservableProperty] private AllotmentProjectOption? selectedProject;

        public ObservableCollection<BeneficiaryRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();

        public int TotalRecords => Filtered().Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        public string TotalBudgetText =>
            SelectedProject is null ? "Total Budget: ₱ 0.00" : $"Total Budget: {SelectedProject.TotalBudgetText}";

        // ---------------- MODALS ----------------
        [ObservableProperty] private bool isProjectDetailsOpen;
        [ObservableProperty] private bool isAddBeneficiariesOpen;
        [ObservableProperty] private bool isEditShareOpen;
        [ObservableProperty] private bool isRemoveOpen;

        // Project details modal fields
        [ObservableProperty] private string projectNameDetails = "";
        [ObservableProperty] private string companyDetails = "";
        [ObservableProperty] private string departmentDetails = "";
        [ObservableProperty] private string sourceOfFundDetails = "";
        [ObservableProperty] private string totalBudgetDetails = "";

        // Add beneficiaries modal
        [ObservableProperty] private string addSearchText = "";
        public ObservableCollection<BeneficiaryRecord> AddItems { get; } = new();

        public int AddSelectedCount => AddItems.Count(x => x.IsSelected);
        public string AddButtonText => $"Add {AddSelectedCount}";
        public string AddFoundText => $"Found {AddItems.Count} records";

        [ObservableProperty] private bool isAddAllSelected;

        // Edit share modal inputs + validation
        private BeneficiaryRecord? _editTarget;

        [ObservableProperty] private string shareAmountInput = "";
        [ObservableProperty] private string shareQtyInput = "";
        [ObservableProperty] private string shareUnitInput = "";

        [ObservableProperty] private string shareAmountError = "";
        [ObservableProperty] private bool hasShareAmountError;

        [ObservableProperty] private string shareInKindError = "";
        [ObservableProperty] private bool hasShareInKindError;

        // Remove modal
        private BeneficiaryRecord? _removeTarget;
        [ObservableProperty] private string removeMessage = "";

        private bool _ready;

        public BeneficiariesViewModel()
        {
            LoadProjectsFromDb();
            _ready = true;

            // default to first project
            SelectedProject = Projects.FirstOrDefault();
            ReloadEverything();
        }

        partial void OnSearchTextChanged(string value) { CurrentPage = 1; Apply(); }
        partial void OnCurrentPageChanged(int value) { Apply(); }

        partial void OnSelectedProjectChanged(AllotmentProjectOption? value)
        {
            if (!_ready) return;
            CurrentPage = 1;
            ReloadEverything();
        }

        partial void OnAddSearchTextChanged(string value) => BuildAddList();

        private void LoadProjectsFromDb()
        {
            Projects.Clear();

            var list = _allotmentRepo.GetAllProjects();
            foreach (var p in list)
                Projects.Add(p);
        }

        private void ReloadEverything()
        {
            LoadAssignedFromDb();
            Apply();
            BuildAddList();

            OnPropertyChanged(nameof(TotalBudgetText));
        }

        private void LoadAssignedFromDb()
        {
            _assignedCache.Clear();

            if (SelectedProject is null) return;

            // ✅ Assigned + only Endorsed (enforced in SQL join)
            _assignedCache = _assignRepo.GetAssignedEndorsed(SelectedProject.Id);
        }

        private List<BeneficiaryRecord> Filtered()
        {
            IEnumerable<BeneficiaryRecord> src = _assignedCache;

            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(q))
            {
                src = src.Where(x =>
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q) ||
                    (x.FirstName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.LastName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Barangay ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Gender ?? "").ToLowerInvariant().Contains(q));
            }

            return src.ToList();
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

        // -------- Add Beneficiaries (modal) --------
        private void BuildAddList()
        {
            AddItems.Clear();
            IsAddAllSelected = false;

            if (SelectedProject is null) return;

            var q = (AddSearchText ?? "").Trim().ToLowerInvariant();

            // ✅ Endorsed beneficiaries NOT yet assigned to this project
            var src = _assignRepo.GetAvailableEndorsedNotAssigned(SelectedProject.Id, q);

            foreach (var r in src)
            {
                r.IsSelected = false;

                r.PropertyChanged -= AddRow_PropertyChanged;
                r.PropertyChanged += AddRow_PropertyChanged;

                AddItems.Add(r);
            }

            OnPropertyChanged(nameof(AddSelectedCount));
            OnPropertyChanged(nameof(AddButtonText));
            OnPropertyChanged(nameof(AddFoundText));
        }

        private void AddRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BeneficiaryRecord.IsSelected))
            {
                OnPropertyChanged(nameof(AddSelectedCount));
                OnPropertyChanged(nameof(AddButtonText));

                if (AddItems.Count > 0)
                    IsAddAllSelected = AddItems.All(x => x.IsSelected);
            }
        }

        partial void OnIsAddAllSelectedChanged(bool value)
        {
            foreach (var r in AddItems)
                r.IsSelected = value;

            OnPropertyChanged(nameof(AddSelectedCount));
            OnPropertyChanged(nameof(AddButtonText));
        }

        // ---------------- Commands ----------------

        [RelayCommand]
        private void OpenProjectDetails()
        {
            if (SelectedProject is null) return;

            ProjectNameDetails = SelectedProject.ProjectName;
            CompanyDetails = SelectedProject.Company;
            DepartmentDetails = SelectedProject.Department;
            SourceOfFundDetails = SelectedProject.SourceOfFund;
            TotalBudgetDetails = SelectedProject.TotalBudgetText;

            IsProjectDetailsOpen = true;
        }

        [RelayCommand] private void CloseProjectDetails() => IsProjectDetailsOpen = false;

        [RelayCommand]
        private void OpenAddBeneficiaries()
        {
            AddSearchText = "";
            BuildAddList();
            IsAddBeneficiariesOpen = true;
        }

        [RelayCommand] private void CloseAddBeneficiaries() => IsAddBeneficiariesOpen = false;

        [RelayCommand]
        private void ConfirmAddSelected()
        {
            if (SelectedProject is null) return;

            var picked = AddItems.Where(x => x.IsSelected).Select(x => x.Id).ToList();
            if (picked.Count == 0) return;

            _assignRepo.AddAssignments(SelectedProject.Id, picked);

            IsAddBeneficiariesOpen = false;
            ReloadEverything();
        }

        // Edit share
        [RelayCommand]
        private void OpenEditShare(BeneficiaryRecord? row)
        {
            if (row is null || SelectedProject is null) return;

            _editTarget = row;
            ClearShareErrors();

            if (SelectedProject.BudgetType == "Money")
            {
                ShareAmountInput = (row.ShareAmount ?? 0m).ToString("N0", CultureInfo.InvariantCulture);
                ShareQtyInput = "";
                ShareUnitInput = "";
            }
            else
            {
                ShareAmountInput = "";
                ShareQtyInput = (row.ShareQty ?? 0).ToString(CultureInfo.InvariantCulture);
                ShareUnitInput = row.ShareUnit ?? "";
            }

            IsEditShareOpen = true;
        }

        [RelayCommand] private void CloseEditShare() => IsEditShareOpen = false;

        [RelayCommand]
        private void ConfirmEditShare()
        {
            if (SelectedProject is null || _editTarget is null) return;

            ClearShareErrors();

            if (SelectedProject.BudgetType == "Money")
            {
                var raw = (ShareAmountInput ?? "").Replace(",", "").Trim();
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) || amt <= 0)
                {
                    ShareAmountError = "Share amount must be a valid number (> 0).";
                    HasShareAmountError = true;
                    return;
                }

                _assignRepo.UpdateShareMoney(SelectedProject.Id, _editTarget.Id, amt);
            }
            else
            {
                if (!int.TryParse((ShareQtyInput ?? "").Trim(), out var qty) || qty <= 0)
                {
                    ShareInKindError = "Quantity must be a valid number (> 0) and unit is required.";
                    HasShareInKindError = true;
                    return;
                }

                var unit = (ShareUnitInput ?? "").Trim();
                if (string.IsNullOrWhiteSpace(unit))
                {
                    ShareInKindError = "Quantity must be a valid number (> 0) and unit is required.";
                    HasShareInKindError = true;
                    return;
                }

                _assignRepo.UpdateShareInKind(SelectedProject.Id, _editTarget.Id, qty, unit);
            }

            IsEditShareOpen = false;
            _editTarget = null;

            ReloadEverything();
        }

        private void ClearShareErrors()
        {
            HasShareAmountError = false;
            ShareAmountError = "";

            HasShareInKindError = false;
            ShareInKindError = "";
        }

        // Remove assignment
        [RelayCommand]
        private void OpenRemove(BeneficiaryRecord? row)
        {
            if (row is null || SelectedProject is null) return;

            _removeTarget = row;
            RemoveMessage = $"Remove {row.FirstName} {row.LastName} from this project?";
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
            if (SelectedProject is null || _removeTarget is null) return;

            _assignRepo.RemoveAssignment(SelectedProject.Id, _removeTarget.Id);

            IsRemoveOpen = false;
            _removeTarget = null;

            ReloadEverything();
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }
    }
}
