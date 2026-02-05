using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WpfApp3.Models;

namespace WpfApp3.ViewModels.Beneficiaries
{
    public partial class BeneficiariesViewModel : ObservableObject
    {
        private readonly List<BeneficiaryRecord> _all = new();
        private readonly Dictionary<string, ProjectDetails> _projects = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private int currentPage = 1;

        [ObservableProperty] private string? selectedProject;
        [ObservableProperty] private string selectedStatus = "Endorsed";

        public int PageSize { get; } = 8;

        public ObservableCollection<BeneficiaryRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();
        public ObservableCollection<string> Projects { get; } = new();

        public int TotalRecords => Filtered().Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        public decimal TotalBudget => Filtered().Sum(x => x.Share);
        public string TotalBudgetText => $"Total Budget: ₱ {TotalBudget:N2}";

        // ---------------- MODALS ----------------
        [ObservableProperty] private bool isProjectDetailsOpen;
        [ObservableProperty] private bool isAddBeneficiariesOpen;
        [ObservableProperty] private bool isEditStatusOpen;
        [ObservableProperty] private bool isRemoveOpen;

        // Project details modal fields
        [ObservableProperty] private string projectNameDetails = "";
        [ObservableProperty] private string companyDetails = "";
        [ObservableProperty] private string descriptionDetails = "";
        [ObservableProperty] private string sourceOfFundDetails = "Admin";
        [ObservableProperty] private string totalBudgetDetails = "0";

        public ObservableCollection<string> SourceOfFundOptions { get; } = new()
        {
            "Admin", "Donation", "Sponsor"
        };

        // Add beneficiaries modal
        [ObservableProperty] private string addSearchText = "Search here";
        public ObservableCollection<BeneficiaryRecord> AddItems { get; } = new();

        public int AddSelectedCount => AddItems.Count(x => x.IsSelected);
        public string AddButtonText => $"Add {AddSelectedCount}";
        public string AddFoundText => $"Found {AddItems.Count} records";

        // Edit status modal
        public ObservableCollection<string> StatusOptions { get; } = new()
        {
            "Endorsed", "Pending", "Rejected"
        };
        [ObservableProperty] private string editStatusValue = "Pending";
        private BeneficiaryRecord? _editTarget;

        // Remove modal
        [ObservableProperty] private string removeMessage = "";
        private BeneficiaryRecord? _removeTarget;

        public BeneficiariesViewModel()
        {
            SeedDummy();
            SeedProjects();
            BuildProjects();
            SelectedProject = "All Projects";
            Apply();
            BuildAddList();
        }

        partial void OnSearchTextChanged(string value) { CurrentPage = 1; Apply(); }
        partial void OnCurrentPageChanged(int value) { Apply(); }
        partial void OnSelectedProjectChanged(string? value) { CurrentPage = 1; Apply(); }
        partial void OnSelectedStatusChanged(string value) { CurrentPage = 1; Apply(); }

        partial void OnAddSearchTextChanged(string value) => BuildAddList();

        private void BuildProjects()
        {
            Projects.Clear();
            Projects.Add("All Projects");
            foreach (var p in _all.Select(x => x.ProjectName).Distinct().OrderBy(x => x))
                Projects.Add(p);

            if (SelectedProject is null) SelectedProject = "All Projects";
        }

        private List<BeneficiaryRecord> Filtered()
        {
            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            IEnumerable<BeneficiaryRecord> src = _all;

            // status tab
            src = src.Where(x => (x.Status ?? "") == (SelectedStatus ?? "Endorsed"));

            // project filter
            var proj = (SelectedProject ?? "All Projects").Trim();
            if (!string.IsNullOrWhiteSpace(proj) && proj != "All Projects")
                src = src.Where(x => (x.ProjectName ?? "") == proj);

            // search
            if (!string.IsNullOrWhiteSpace(q))
            {
                src = src.Where(x =>
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q) ||
                    (x.FirstName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.LastName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.ProjectName ?? "").ToLowerInvariant().Contains(q) ||
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
            OnPropertyChanged(nameof(TotalBudget));
            OnPropertyChanged(nameof(TotalBudgetText));
        }

        private void SeedProjects()
        {
            _projects["School Supplies Drive"] = new ProjectDetails(
                "School Supplies Drive", "BrightFuture",
                "Donation of school supplies for public elementary students.",
                "Admin", 1000000m);

            _projects["Scholarship Grants"] = new ProjectDetails(
                "Scholarship Grants", "BrightFuture",
                "Educational financial assistance for qualified students.",
                "Donation", 1000000m);

            _projects["PWD Assistance"] = new ProjectDetails(
                "PWD Assistance", "BrightFuture",
                "Support program for persons with disabilities.",
                "Donation", 500000m);

            _projects["Farmers' Seed"] = new ProjectDetails(
                "Farmers' Seed", "SafeHome PH",
                "Seed distribution support for local farmers.",
                "Donation", 500000m);

            _projects["Emergency Shelter"] = new ProjectDetails(
                "Emergency Shelter", "SafeHome PH",
                "Temporary shelter assistance for displaced families.",
                "Admin", 500000m);

            _projects["Community Pantry"] = new ProjectDetails(
                "Community Pantry", "SafeHome PH",
                "Food and essentials distribution.",
                "Admin", 200000m);

            _projects["Coastal Clean-Up"] = new ProjectDetails(
                "Coastal Clean-Up", "SafeHome PH",
                "Environmental cleanup and community drive.",
                "Donation", 50000m);

            _projects["Water Filter"] = new ProjectDetails(
                "Water Filter", "SafeHome PH",
                "Clean water access through filter distribution.",
                "Admin", 500000m);
        }

        private void SeedDummy()
        {
            _all.AddRange(new[]
            {
        // ---------------- Endorsed ----------------
        new BeneficiaryRecord { Id=1, ProjectName="School Supplies Drive", FirstName="John",  LastName="Dela Cruz", Gender="Male",   Barangay="San Roque",      Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=2, ProjectName="Scholarship Grants",    FirstName="Maria", LastName="Santos",    Gender="Female", Barangay="Sta. Maria",    Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=3, ProjectName="PWD Assistance",        FirstName="Paolo", LastName="Reyes",     Gender="Male",   Barangay="Poblacion",     Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=4, ProjectName="Farmers' Seed",         FirstName="Ana",   LastName="Garcia",    Gender="Female", Barangay="San Isidro",    Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=5, ProjectName="Emergency Shelter",     FirstName="Mark",  LastName="Navarro",   Gender="Male",   Barangay="Maligaya",      Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=6, ProjectName="Community Pantry",      FirstName="Grace", LastName="Flores",    Gender="Female", Barangay="Bagong Silang", Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=7, ProjectName="Coastal Clean-Up",      FirstName="Kevin", LastName="Lopez",     Gender="Male",   Barangay="San Juan",      Share=10000, Status="Endorsed" },
        new BeneficiaryRecord { Id=8, ProjectName="Water Filter",          FirstName="Rica",  LastName="Mendoza",   Gender="Female", Barangay="San Pedro",     Share=10000, Status="Endorsed" },

        // ---------------- Pending ----------------
        new BeneficiaryRecord { Id=9,  ProjectName="School Supplies Drive", FirstName="James", LastName="Bautista",  Gender="Male",   Barangay="Poblacion",   Share=0, Status="Pending" },
        new BeneficiaryRecord { Id=10, ProjectName="Scholarship Grants",    FirstName="Ella",  LastName="Torres",    Gender="Female", Barangay="San Roque",   Share=0, Status="Pending" },
        new BeneficiaryRecord { Id=11, ProjectName="PWD Assistance",        FirstName="Noah",  LastName="Cabrera",   Gender="Male",   Barangay="San Isidro",  Share=0, Status="Pending" },

        // ---------------- Rejected ----------------
        new BeneficiaryRecord { Id=12, ProjectName="Farmers' Seed",         FirstName="Liam",  LastName="Ramos",     Gender="Male",   Barangay="San Juan",   Share=0, Status="Rejected" },
        new BeneficiaryRecord { Id=13, ProjectName="Emergency Shelter",     FirstName="Sofia", LastName="Aquino",    Gender="Female", Barangay="Sta. Maria", Share=0, Status="Rejected" },

        // ---------------- NO STATUS (for Add Beneficiaries modal) ----------------
        new BeneficiaryRecord { Id=14, ProjectName="School Supplies Drive", FirstName="Joshua", LastName="Perez",     Gender="Male",   Barangay="San Roque",       Share=0, Status="" },
        new BeneficiaryRecord { Id=15, ProjectName="School Supplies Drive", FirstName="Angel",  LastName="Castro",    Gender="Female", Barangay="Poblacion",       Share=0, Status="" },
        new BeneficiaryRecord { Id=16, ProjectName="School Supplies Drive", FirstName="Miguel", LastName="Domingo",   Gender="Male",   Barangay="San Pedro",       Share=0, Status="" },

        new BeneficiaryRecord { Id=17, ProjectName="Scholarship Grants",    FirstName="Bianca", LastName="Lim",       Gender="Female", Barangay="Sta. Maria",      Share=0, Status="" },
        new BeneficiaryRecord { Id=18, ProjectName="Scholarship Grants",    FirstName="Carlo",  LastName="Villanueva",Gender="Male",   Barangay="Maligaya",        Share=0, Status="" },
        new BeneficiaryRecord { Id=19, ProjectName="Scholarship Grants",    FirstName="Denise", LastName="Alvarez",   Gender="Female", Barangay="Bagong Silang",   Share=0, Status="" },

        new BeneficiaryRecord { Id=20, ProjectName="PWD Assistance",        FirstName="Ronnie", LastName="Gomez",     Gender="Male",   Barangay="San Isidro",      Share=0, Status="" },
        new BeneficiaryRecord { Id=21, ProjectName="PWD Assistance",        FirstName="Aira",   LastName="Salazar",   Gender="Female", Barangay="Poblacion",       Share=0, Status="" },
        new BeneficiaryRecord { Id=22, ProjectName="PWD Assistance",        FirstName="Patrick",LastName="Soriano",   Gender="Male",   Barangay="San Juan",        Share=0, Status="" },

        new BeneficiaryRecord { Id=23, ProjectName="Farmers' Seed",         FirstName="Cynthia",LastName="Delos Reyes",Gender="Female",Barangay="Donation",        Share=0, Status="" },
        new BeneficiaryRecord { Id=24, ProjectName="Farmers' Seed",         FirstName="Erwin",  LastName="Manalo",    Gender="Male",   Barangay="San Pedro",       Share=0, Status="" },
        new BeneficiaryRecord { Id=25, ProjectName="Farmers' Seed",         FirstName="Nicole", LastName="Fernandez", Gender="Female", Barangay="San Isidro",      Share=0, Status="" },

        new BeneficiaryRecord { Id=26, ProjectName="Emergency Shelter",     FirstName="Ramon",  LastName="Bacani",    Gender="Male",   Barangay="Sta. Maria",      Share=0, Status="" },
        new BeneficiaryRecord { Id=27, ProjectName="Emergency Shelter",     FirstName="Trisha", LastName="Velasco",   Gender="Female", Barangay="Maligaya",        Share=0, Status="" },
        new BeneficiaryRecord { Id=28, ProjectName="Emergency Shelter",     FirstName="Julius", LastName="Pineda",    Gender="Male",   Barangay="San Roque",       Share=0, Status="" },

        new BeneficiaryRecord { Id=29, ProjectName="Community Pantry",      FirstName="Clarice",LastName="Roxas",     Gender="Female", Barangay="Bagong Silang",   Share=0, Status="" },
        new BeneficiaryRecord { Id=30, ProjectName="Community Pantry",      FirstName="Ian",    LastName="Padilla",   Gender="Male",   Barangay="Poblacion",       Share=0, Status="" },
        new BeneficiaryRecord { Id=31, ProjectName="Community Pantry",      FirstName="Faith",  LastName="Guinto",    Gender="Female", Barangay="San Pedro",       Share=0, Status="" },

        new BeneficiaryRecord { Id=32, ProjectName="Coastal Clean-Up",      FirstName="Leo",    LastName="De Vera",   Gender="Male",   Barangay="San Juan",        Share=0, Status="" },
        new BeneficiaryRecord { Id=33, ProjectName="Coastal Clean-Up",      FirstName="Mika",   LastName="Valdez",    Gender="Female", Barangay="Donation",        Share=0, Status="" },
        new BeneficiaryRecord { Id=34, ProjectName="Coastal Clean-Up",      FirstName="Arvin",  LastName="Espino",    Gender="Male",   Barangay="San Roque",       Share=0, Status="" },

        new BeneficiaryRecord { Id=35, ProjectName="Water Filter",          FirstName="Shane",  LastName="Cortez",    Gender="Female", Barangay="San Pedro",       Share=0, Status="" },
        new BeneficiaryRecord { Id=36, ProjectName="Water Filter",          FirstName="Bryan",  LastName="Sison",     Gender="Male",   Barangay="Poblacion",       Share=0, Status="" },
        new BeneficiaryRecord { Id=37, ProjectName="Water Filter",          FirstName="Jasmine",LastName="Ortega",    Gender="Female", Barangay="Sta. Maria",      Share=0, Status="" },
    });
        }

        // -------- Add Beneficiaries list (modal 2) --------
        private void BuildAddList()
        {
            var q = (AddSearchText ?? "").Trim().ToLowerInvariant();
            var src = _all.Where(x => string.IsNullOrWhiteSpace(x.Status)).ToList();


            if (!string.IsNullOrWhiteSpace(q))
            {
                src = src.Where(x =>
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q) ||
                    (x.FirstName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.LastName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Barangay ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Gender ?? "").ToLowerInvariant().Contains(q)).ToList();
            }

            AddItems.Clear();
            foreach (var r in src)
            {
                r.IsSelected = false;

                // ✅ Update "Add X" live when a checkbox changes
                r.PropertyChanged -= AddRow_PropertyChanged;
                r.PropertyChanged += AddRow_PropertyChanged;

                AddItems.Add(r);
            }

            IsAddAllSelected = false;

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

                // keep header checkbox in sync
                if (AddItems.Count > 0)
                    IsAddAllSelected = AddItems.All(x => x.IsSelected);
            }
        }


        // ---------------- Commands ----------------
        [RelayCommand] private void SetEndorsed() => SelectedStatus = "Endorsed";
        [RelayCommand] private void SetPending() => SelectedStatus = "Pending";
        [RelayCommand] private void SetRejected() => SelectedStatus = "Rejected";

        // Eye icon -> Project details modal
        [RelayCommand]
        private void OpenProjectDetails()
        {
            var key = SelectedProject;
            if (string.IsNullOrWhiteSpace(key) || key == "All Projects")
                key = _projects.Keys.FirstOrDefault();

            if (key is null || !_projects.TryGetValue(key, out var d))
                d = new ProjectDetails("Project Name", "Company", "Description here", "Admin", 0);

            ProjectNameDetails = d.ProjectName;
            CompanyDetails = d.Company;
            DescriptionDetails = d.Description;
            SourceOfFundDetails = d.SourceOfFund;
            TotalBudgetDetails = $"₱ {d.TotalBudget:N2}";

            IsProjectDetailsOpen = true;
        }

        [RelayCommand] private void CloseProjectDetails() => IsProjectDetailsOpen = false;

        // Add beneficiaries modal
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
            var picked = AddItems.Where(x => x.IsSelected).ToList();
            if (picked.Count == 0) return;

            // demo: mark selected as Pending
            foreach (var r in picked)
                r.Status = "Pending";

            IsAddBeneficiariesOpen = false;
            Apply();
        }


        // Edit status modal (pencil)
        [RelayCommand]
        private void OpenEditStatus(BeneficiaryRecord? row)
        {
            if (row is null) return;
            _editTarget = row;
            EditStatusValue = row.Status ?? "Pending";
            IsEditStatusOpen = true;
        }

        [RelayCommand] private void CloseEditStatus() => IsEditStatusOpen = false;

        [RelayCommand]
        private void ConfirmEditStatus()
        {
            if (_editTarget is not null)
                _editTarget.Status = EditStatusValue;

            IsEditStatusOpen = false;
            _editTarget = null;
            Apply();
        }

        // Remove modal (trash)
        [RelayCommand]
        private void OpenRemove(BeneficiaryRecord? row)
        {
            if (row is null) return;
            _removeTarget = row;
            RemoveMessage = "Are you sure you want to remove the selected beneficiary? This action cannot be undone.";
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
            if (_removeTarget is not null)
                _all.Remove(_removeTarget);

            IsRemoveOpen = false;
            _removeTarget = null;
            Apply();
            BuildProjects();
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }

        // simple internal record
        private record ProjectDetails(string ProjectName, string Company, string Description, string SourceOfFund, decimal TotalBudget);

        [ObservableProperty] private bool isAddAllSelected;

        partial void OnIsAddAllSelectedChanged(bool value)
        {
            foreach (var r in AddItems)
                r.IsSelected = value;

            OnPropertyChanged(nameof(AddSelectedCount));
            OnPropertyChanged(nameof(AddButtonText));
        }

    }
}
