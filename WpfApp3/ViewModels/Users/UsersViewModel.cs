using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WpfApp3.Models;

namespace WpfApp3.ViewModels.Users
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly List<UserRecord> _all = new();

        // table/search/paging
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private int currentPage = 1;

        public int PageSize { get; } = 8;

        public ObservableCollection<UserRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();

        public int TotalRecords => Filtered().Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        // ===== MODALS =====
        [ObservableProperty] private bool isFormOpen;
        [ObservableProperty] private bool isDeleteOpen;
        [ObservableProperty] private string formTitle = "Add User";

        private UserRecord? _editingTarget;
        private UserRecord? _deleteTarget;

        [ObservableProperty] private string deleteMessage = "";

        // ===== FORM FIELDS =====
        [ObservableProperty] private string firstNameInput = "";
        [ObservableProperty] private string lastNameInput = "";
        [ObservableProperty] private string? officeInput;
        [ObservableProperty] private string? roleInput;
        [ObservableProperty] private string usernameInput = "";
        [ObservableProperty] private string passwordInput = "";

        public ObservableCollection<string> Offices { get; } = new()
        {
            "Admin", "Finance", "Accounting", "Registrar"
        };

        public ObservableCollection<string> Roles { get; } = new()
        {
            "Admin", "User"
        };

        public UsersViewModel()
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

        private List<UserRecord> Filtered()
        {
            var q = (SearchText ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
                return _all.ToList();

            return _all.Where(x =>
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q) ||
                    (x.FirstName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.LastName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Office ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Role ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Username ?? "").ToLowerInvariant().Contains(q))
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
                new UserRecord { Id=1, FirstName="John", LastName="Doe", Office="Admin", Role="Admin", Username="johnd12", Password="johnpass12" },
                new UserRecord { Id=2, FirstName="Jane", LastName="Doe", Office="Admin", Role="Admin", Username="janed02", Password="janepass02" },
                new UserRecord { Id=3, FirstName="James", LastName="Philips", Office="Finance", Role="User", Username="jamesp03", Password="jamespass03" },
                new UserRecord { Id=4, FirstName="Peter", LastName="Parker", Office="Finance", Role="User", Username="peterp04", Password="peterpass04" },
                new UserRecord { Id=5, FirstName="Mary", LastName="Jane", Office="Accounting", Role="User", Username="mjane05", Password="marypass05" },
                new UserRecord { Id=6, FirstName="Tony", LastName="Chop", Office="Accounting", Role="User", Username="tonychop06", Password="tonypass06" },
                new UserRecord { Id=7, FirstName="Ace", LastName="Fist", Office="Registrar", Role="User", Username="acefist07", Password="acepass07" },
                new UserRecord { Id=8, FirstName="Roger", LastName="Gold", Office="Registrar", Role="User", Username="goldroger08", Password="rogerpass08" },
            });
        }

        // ===== COMMANDS =====

        [RelayCommand]
        private void AddUser()
        {
            _editingTarget = null;
            FormTitle = "Add User";

            FirstNameInput = "";
            LastNameInput = "";
            OfficeInput = null;
            RoleInput = null;
            UsernameInput = "";
            PasswordInput = "";

            IsFormOpen = true;
        }

        [RelayCommand]
        private void Edit(UserRecord? row)
        {
            if (row is null) return;

            _editingTarget = row;
            FormTitle = "Edit User";

            FirstNameInput = row.FirstName;
            LastNameInput = row.LastName;
            OfficeInput = row.Office;
            RoleInput = row.Role;
            UsernameInput = row.Username;
            PasswordInput = row.Password;

            IsFormOpen = true;
        }

        [RelayCommand] private void CloseForm() => IsFormOpen = false;

        [RelayCommand]
        private void SaveForm()
        {
            var first = (FirstNameInput ?? "").Trim();
            var last = (LastNameInput ?? "").Trim();
            var office = (OfficeInput ?? "").Trim();
            var role = (RoleInput ?? "").Trim();
            var user = (UsernameInput ?? "").Trim();
            var pass = (PasswordInput ?? "").Trim();

            if (string.IsNullOrWhiteSpace(first)) first = "First";
            if (string.IsNullOrWhiteSpace(last)) last = "Last";
            if (string.IsNullOrWhiteSpace(office)) office = "Admin";
            if (string.IsNullOrWhiteSpace(role)) role = "User";
            if (string.IsNullOrWhiteSpace(user)) user = "username";
            if (string.IsNullOrWhiteSpace(pass)) pass = "password";

            if (_editingTarget is null)
            {
                var nextId = _all.Count == 0 ? 1 : _all.Max(x => x.Id) + 1;

                _all.Add(new UserRecord
                {
                    Id = nextId,
                    FirstName = first,
                    LastName = last,
                    Office = office,
                    Role = role,
                    Username = user,
                    Password = pass
                });
            }
            else
            {
                _editingTarget.FirstName = first;
                _editingTarget.LastName = last;
                _editingTarget.Office = office;
                _editingTarget.Role = role;
                _editingTarget.Username = user;
                _editingTarget.Password = pass;
                _editingTarget.IsPasswordRevealed = false;
            }

            IsFormOpen = false;
            Apply();
        }

        [RelayCommand]
        private void Delete(UserRecord? row)
        {
            if (row is null) return;

            _deleteTarget = row;
            DeleteMessage = $"Are you sure you want to delete user, {row.Username}? This action cannot be undone.";
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
                _all.Remove(_deleteTarget);

            IsDeleteOpen = false;
            _deleteTarget = null;
            Apply();
        }

        [RelayCommand]
        private void ToggleReveal(UserRecord? row)
        {
            if (row is null) return;
            row.IsPasswordRevealed = !row.IsPasswordRevealed;
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }
    }
}
