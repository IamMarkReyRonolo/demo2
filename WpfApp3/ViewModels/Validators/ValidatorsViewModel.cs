using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using WpfApp3.Models;

namespace WpfApp3.ViewModels.Validators
{
    public enum ValidatorsMainTab
    {
        NotYetValidated,
        Validated
    }

    public enum ValidatorsStatusTab
    {
        Endorsed,
        Pending,
        Rejected
    }

    public partial class ValidatorsViewModel : ObservableObject
    {
        private readonly List<ValidatorRecord> _allNotYet = new();
        private readonly List<ValidatorRecord> _allValidated = new();

        // ===== Tabs =====
        [ObservableProperty] private ValidatorsMainTab activeMainTab = ValidatorsMainTab.NotYetValidated;
        [ObservableProperty] private ValidatorsStatusTab activeStatusTab = ValidatorsStatusTab.Endorsed;

        // ===== Search =====
        [ObservableProperty] private string searchNotYetText = "";
        [ObservableProperty] private string searchValidatedText = "";

        // ===== Lists shown in UI =====
        public ObservableCollection<ValidatorRecord> NotYetItems { get; } = new();
        public ObservableCollection<ValidatorRecord> ValidatedItems { get; } = new();

        // ===== Selection =====
        [ObservableProperty] private ValidatorRecord? selectedPerson;

        // ===== Modals =====
        [ObservableProperty] private bool isValidateModalOpen = false;
        [ObservableProperty] private bool isProfileModalOpen = false;

        [ObservableProperty] private string validateSelectedStatus = ""; // dropdown selected (Endorsed/Pending/Rejected)

        // ===== UI text =====
        public string NotYetFoundText => $"Found {NotYetItems.Count} records";
        public string ValidatedFoundText => $"Found {ValidatedItems.Count} records";

        // ===== Dropdown Sources =====
        public ObservableCollection<string> GenderOptions { get; } = new() { "Male", "Female" };
        public ObservableCollection<string> ClassificationOptions { get; } = new() { "PWD", "Senior Citizen", "Indigenous", "None" };
        public ObservableCollection<string> ValidateStatusOptions { get; } = new() { "Endorsed", "Pending", "Rejected" };

        public ValidatorsViewModel()
        {
            SeedDummyData();
            LoadNotYet();
            LoadValidated();

            // default selection
            SelectedPerson = NotYetItems.FirstOrDefault();
        }

        private void SeedDummyData()
        {
            // Not yet validated (Status = "")
            var notYet = new[]
            {
                NewPerson(101,"BENE-000101","CR-900101","Arjun","M.","Codilla","Male","25 January 1990","PWD","Male","San Jose, California, USA"),
                NewPerson(102,"BENE-000102","CR-900102","Maria","L.","Santos","Female","03 March 1992","Senior Citizen","Admin","Quezon City, Philippines"),
                NewPerson(103,"BENE-000103","CR-900103","John","A.","Dela Cruz","Male","12 December 1988","PWD","Donation","Cebu City, Philippines"),
                NewPerson(104,"BENE-000104","CR-900104","Angel","R.","Reyes","Female","06 June 1996","Indigenous","Donation","Davao City, Philippines"),
                NewPerson(105,"BENE-000105","CR-900105","Paolo","S.","Garcia","Male","09 September 1991","None","Admin","Baguio City, Philippines"),
                NewPerson(106,"BENE-000106","CR-900106","Kristine","P.","Navarro","Female","21 July 1994","PWD","Admin","Iloilo City, Philippines"),
                NewPerson(107,"BENE-000107","CR-900107","Mark","T.","Flores","Male","10 October 1993","None","Donation","Cagayan de Oro, Philippines"),
                NewPerson(108,"BENE-000108","CR-900108","Lea","G.","Mendoza","Female","14 February 1995","PWD","Donation","Legazpi, Philippines"),
            };

            _allNotYet.AddRange(notYet);

            // Validated (Status = Endorsed/Pending/Rejected)
            _allValidated.AddRange(new[]
            {
                NewValidated(1,"BENE-000001","CR-900001","School Supplies Drive","", "BrightFuture","Male","", "PWD","Admin","", "Endorsed"),
                NewValidated(2,"BENE-000002","CR-900002","Scholarship Grants","", "BrightFuture","Male","", "PWD","Admin","", "Endorsed"),
                NewValidated(3,"BENE-000003","CR-900003","PWD Assistance","", "BrightFuture","Male","", "PWD","Donation","", "Endorsed"),
                NewValidated(4,"BENE-000004","CR-900004","Farmers’ Seed","", "SafeHome PH","Female","", "Indigenous","Donation","", "Endorsed"),
                NewValidated(5,"BENE-000005","CR-900005","Emergency Shelter","", "SafeHome PH","Female","", "Senior Citizen","Admin","", "Pending"),
                NewValidated(6,"BENE-000006","CR-900006","Community Pantry","", "SafeHome PH","Female","", "None","Admin","", "Pending"),
                NewValidated(7,"BENE-000007","CR-900007","Coastal Clean-Up","", "SafeHome PH","Male","", "None","Donation","", "Rejected"),
                NewValidated(8,"BENE-000008","CR-900008","Water Filter","", "SafeHome PH","Male","", "None","Admin","", "Endorsed"),
            });
        }

        private static ValidatorRecord NewPerson(
            int id, string beneId, string civilId, string fn, string mn, string ln,
            string gender, string dob, string classification, string barangay, string address)
        {
            return new ValidatorRecord
            {
                Id = id,
                BeneficiaryId = beneId,
                CivilRegistryId = civilId,
                FirstName = fn,
                MiddleName = mn,
                LastName = ln,
                Gender = gender,
                DateOfBirth = dob,
                Classification = classification,
                Barangay = barangay,
                PresentAddress = address,
                Status = ""
            };
        }

        private static ValidatorRecord NewValidated(
            int id, string beneId, string civilId, string fn, string mn, string ln,
            string gender, string dob, string classification, string barangay, string address, string status)
        {
            return new ValidatorRecord
            {
                Id = id,
                BeneficiaryId = beneId,
                CivilRegistryId = civilId,
                FirstName = fn,
                MiddleName = mn,
                LastName = ln,
                Gender = gender,
                DateOfBirth = dob,
                Classification = classification,
                Barangay = barangay,
                PresentAddress = address,
                Status = status
            };
        }

        private void LoadNotYet()
        {
            NotYetItems.Clear();

            var q = _allNotYet.AsEnumerable();

            var s = (SearchNotYetText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(s))
            {
                q = q.Where(x =>
                    x.FirstName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    x.LastName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    x.BeneficiaryId.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in q)
                NotYetItems.Add(item);

            OnPropertyChanged(nameof(NotYetFoundText));
        }

        private void LoadValidated()
        {
            ValidatedItems.Clear();

            var status = ActiveStatusTab switch
            {
                ValidatorsStatusTab.Endorsed => "Endorsed",
                ValidatorsStatusTab.Pending => "Pending",
                ValidatorsStatusTab.Rejected => "Rejected",
                _ => "Endorsed"
            };

            var q = _allValidated.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

            var s = (SearchValidatedText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(s))
            {
                q = q.Where(x =>
                    x.FirstName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    x.LastName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    x.BeneficiaryId.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in q)
                ValidatedItems.Add(item);

            OnPropertyChanged(nameof(ValidatedFoundText));
        }

        // ===== Commands =====

        [RelayCommand]
        private void SwitchMainTab(string tab)
        {
            ActiveMainTab = tab == "Validated" ? ValidatorsMainTab.Validated : ValidatorsMainTab.NotYetValidated;

            // set a sensible selection when switching
            if (ActiveMainTab == ValidatorsMainTab.NotYetValidated)
                SelectedPerson = NotYetItems.FirstOrDefault();
            else
                SelectedPerson = ValidatedItems.FirstOrDefault();
        }

        [RelayCommand]
        private void SwitchStatusTab(string tab)
        {
            ActiveStatusTab = tab switch
            {
                "Pending" => ValidatorsStatusTab.Pending,
                "Rejected" => ValidatorsStatusTab.Rejected,
                _ => ValidatorsStatusTab.Endorsed
            };

            LoadValidated();
            SelectedPerson = ValidatedItems.FirstOrDefault();
        }

        [RelayCommand]
        private void SearchNotYet() => LoadNotYet();

        [RelayCommand]
        private void SearchValidated() => LoadValidated();

        [RelayCommand]
        private void SelectNotYetPerson(ValidatorRecord? person)
        {
            if (person is null) return;
            SelectedPerson = person;
        }

        [RelayCommand]
        private void SelectValidatedPerson(ValidatorRecord? person)
        {
            if (person is null) return;
            SelectedPerson = person;
        }

        [RelayCommand]
        private void SaveProfile()
        {
            // dummy: do nothing; bindings already updated
            // later we will call repository update here.
        }

        [RelayCommand]
        private void OpenValidateModal()
        {
            if (SelectedPerson is null) return;

            // default dropdown based on current status (or Endorsed if empty)
            ValidateSelectedStatus = string.IsNullOrWhiteSpace(SelectedPerson.Status) ? "Endorsed" : SelectedPerson.Status;
            IsValidateModalOpen = true;
        }

        [RelayCommand]
        private void CloseValidateModal() => IsValidateModalOpen = false;

        [RelayCommand]
        private void OpenProfileModal()
        {
            if (SelectedPerson is null) return;
            IsProfileModalOpen = true;
        }

        [RelayCommand]
        private void CloseProfileModal() => IsProfileModalOpen = false;

        [RelayCommand]
        private void CloseAllModals()
        {
            IsValidateModalOpen = false;
            IsProfileModalOpen = false;
        }

        [RelayCommand]
        private void ConfirmValidate()
        {
            if (SelectedPerson is null) return;
            if (string.IsNullOrWhiteSpace(ValidateSelectedStatus)) return;

            var newStatus = ValidateSelectedStatus.Trim();

            // If currently not yet validated → move to validated list
            if (ActiveMainTab == ValidatorsMainTab.NotYetValidated)
            {
                SelectedPerson.Status = newStatus;

                // move from _allNotYet to _allValidated
                _allNotYet.Remove(SelectedPerson);
                _allValidated.Add(SelectedPerson);

                LoadNotYet();

                // switch to validated view and filter to new status
                ActiveMainTab = ValidatorsMainTab.Validated;
                ActiveStatusTab = newStatus switch
                {
                    "Pending" => ValidatorsStatusTab.Pending,
                    "Rejected" => ValidatorsStatusTab.Rejected,
                    _ => ValidatorsStatusTab.Endorsed
                };

                LoadValidated();
                SelectedPerson = ValidatedItems.FirstOrDefault(x => x.Id == SelectedPerson.Id) ?? ValidatedItems.FirstOrDefault();
            }
            else
            {
                // already validated → just update status, might move between filters
                SelectedPerson.Status = newStatus;

                ActiveStatusTab = newStatus switch
                {
                    "Pending" => ValidatorsStatusTab.Pending,
                    "Rejected" => ValidatorsStatusTab.Rejected,
                    _ => ValidatorsStatusTab.Endorsed
                };

                LoadValidated();
                SelectedPerson = ValidatedItems.FirstOrDefault(x => x.Id == SelectedPerson.Id) ?? ValidatedItems.FirstOrDefault();
            }

            IsValidateModalOpen = false;
        }
    }
}
