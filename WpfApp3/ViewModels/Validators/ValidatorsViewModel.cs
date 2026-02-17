using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WpfApp3.Models;
using WpfApp3.Services;

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
        private readonly BeneficiariesRepository _repo = new();

        // This represents the EXTERNAL DATABASE source (replace later with real external DB pull)
        private readonly List<ValidatorRecord> _externalPeople = new();

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
        [ObservableProperty] private bool isSaveConfirmOpen = false;

        [ObservableProperty] private string validateSelectedStatus = ""; // Endorsed/Pending/Rejected

        // ===== UI text =====
        public string NotYetFoundText => $"Found {NotYetItems.Count} records";
        public string ValidatedFoundText => $"Found {ValidatedItems.Count} records";

        // ===== Dropdown Sources =====
        public ObservableCollection<string> GenderOptions { get; } = new() { "Male", "Female" };

        // ✅ updated classification list (Farmer, Vendor)
        public ObservableCollection<string> ClassificationOptions { get; } =
            new() { "PWD", "Senior Citizen", "Indigenous", "Farmer", "Vendor", "None" };

        public ObservableCollection<string> ValidateStatusOptions { get; } = new() { "Endorsed", "Pending", "Rejected" };

        public ValidatorsViewModel()
        {
            _repo.EnsureTable();

            SeedExternalPeople(); // replace later with actual external DB pull

            LoadNotYet();
            LoadValidated();

            SelectedPerson = NotYetItems.FirstOrDefault() ?? ValidatedItems.FirstOrDefault();
        }

        [RelayCommand]
        private void CloseAllModals()
        {
            IsValidateModalOpen = false;
            IsProfileModalOpen = false;
            IsSaveConfirmOpen = false;
        }

        private void SeedExternalPeople()
        {
            _externalPeople.Clear();

            // External DB dummy list (left side)
            _externalPeople.AddRange(new[]
            {
                NewExternal(101,"BENE-000101","CR-900101","Arjun","M.","Codilla","Male","25 January 1990","PWD","San Jose","San Jose, California, USA"),
                NewExternal(102,"BENE-000102","CR-900102","Maria","L.","Santos","Female","03 March 1992","Senior Citizen","Quezon City","Quezon City, Philippines"),
                NewExternal(103,"BENE-000103","CR-900103","John","A.","Dela Cruz","Male","12 December 1988","PWD","Cebu City","Cebu City, Philippines"),
                NewExternal(104,"BENE-000104","CR-900104","Angel","R.","Reyes","Female","06 June 1996","Indigenous","Davao City","Davao City, Philippines"),
                NewExternal(105,"BENE-000105","CR-900105","Paolo","S.","Garcia","Male","09 September 1991","None","Baguio City","Baguio City, Philippines"),
                NewExternal(106,"BENE-000106","CR-900106","Kristine","P.","Navarro","Female","21 July 1994","PWD","Iloilo City","Iloilo City, Philippines"),
                NewExternal(107,"BENE-000107","CR-900107","Mark","T.","Flores","Male","10 October 1993","Vendor","Cagayan de Oro","Cagayan de Oro, Philippines"),
                NewExternal(108,"BENE-000108","CR-900108","Lea","G.","Mendoza","Female","14 February 1995","Farmer","Legazpi","Legazpi, Philippines"),
            });
        }

        private static ValidatorRecord NewExternal(
            int sourceId,
            string beneId,
            string civilId,
            string fn,
            string mn,
            string ln,
            string gender,
            string dob,
            string classification,
            string barangay,
            string address)
        {
            return new ValidatorRecord
            {
                Id = sourceId, // external id
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
                Status = "" // external has no status
            };
        }

        private static bool IsValidatedStatus(string status)
        {
            return string.Equals(status, "Endorsed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase);
        }

        private static string CanonicalStatus(string status)
        {
            status = (status ?? "").Trim();
            if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase)) return "Pending";
            if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) return "Rejected";
            if (status.Equals("Endorsed", StringComparison.OrdinalIgnoreCase)) return "Endorsed";
            if (status.Equals("Not Validated", StringComparison.OrdinalIgnoreCase)) return "Not Validated";
            return status;
        }

        private string CurrentValidatedStatus()
        {
            return ActiveStatusTab switch
            {
                ValidatorsStatusTab.Endorsed => "Endorsed",
                ValidatorsStatusTab.Pending => "Pending",
                ValidatorsStatusTab.Rejected => "Rejected",
                _ => "Endorsed"
            };
        }

        private void LoadNotYet()
        {
            NotYetItems.Clear();

            // Pull any saved rows from OUR DB for these external beneficiary IDs
            var saved = _repo.GetByBeneficiaryIds(_externalPeople.Select(x => x.BeneficiaryId));

            // Merge:
            // - if already validated in DB => do NOT show in Not Yet Validated
            // - if saved as Not Validated => show DB version (includes saved edits)
            // - if not in DB => show external version
            var merged = new List<ValidatorRecord>();

            foreach (var ext in _externalPeople)
            {
                if (saved.TryGetValue(ext.BeneficiaryId, out var dbRow))
                {
                    var st = CanonicalStatus(dbRow.Status);
                    if (IsValidatedStatus(st))
                        continue;

                    merged.Add(dbRow); // Not Validated rows show here
                }
                else
                {
                    merged.Add(ext);
                }
            }

            var q = merged.AsEnumerable();
            var s = (SearchNotYetText ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(s))
            {
                q = q.Where(x =>
                    (x.FirstName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (x.LastName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (x.BeneficiaryId ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in q) NotYetItems.Add(item);
            OnPropertyChanged(nameof(NotYetFoundText));
        }

        private void LoadValidated()
        {
            ValidatedItems.Clear();

            var status = CurrentValidatedStatus();
            var rows = _repo.GetByStatus(status);

            var q = rows.AsEnumerable();
            var s = (SearchValidatedText ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(s))
            {
                q = q.Where(x =>
                    (x.FirstName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (x.LastName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (x.BeneficiaryId ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in q) ValidatedItems.Add(item);
            OnPropertyChanged(nameof(ValidatedFoundText));
        }

        // ========= Commands that match your XAML =========

        [RelayCommand]
        private void SetMainTab(ValidatorsMainTab tab)
        {
            ActiveMainTab = tab;

            if (ActiveMainTab == ValidatorsMainTab.NotYetValidated)
            {
                LoadNotYet();
                SelectedPerson = NotYetItems.FirstOrDefault();
            }
            else
            {
                LoadValidated();
                SelectedPerson = ValidatedItems.FirstOrDefault();
            }
        }

        [RelayCommand]
        private void SetStatusTab(ValidatorsStatusTab tab)
        {
            ActiveStatusTab = tab;
            LoadValidated();
            SelectedPerson = ValidatedItems.FirstOrDefault();
        }

        [RelayCommand] private void SearchNotYet() => LoadNotYet();
        [RelayCommand] private void SearchValidated() => LoadValidated();

        // ✅ OPEN PROFILE MODAL FROM TABLE ROW (pencil)
        [RelayCommand]
        private void OpenProfileModal(ValidatorRecord? person)
        {
            if (person is null) return;
            SelectedPerson = person;
            IsProfileModalOpen = true;
        }

        [RelayCommand] private void CloseProfileModal() => IsProfileModalOpen = false;

        // ===== Save profile confirm modal =====
        [RelayCommand]
        private void OpenSaveConfirm()
        {
            if (SelectedPerson is null) return;
            IsSaveConfirmOpen = true;
        }

        [RelayCommand] private void CloseSaveConfirm() => IsSaveConfirmOpen = false;

        [RelayCommand]
        private void ConfirmSaveProfile()
        {
            var person = SelectedPerson;
            if (person is null) return;

            // Save Profile rule:
            // - if already validated => keep current validated status
            // - else => set to Not Validated
            var current = CanonicalStatus(person.Status);
            var statusToSave = IsValidatedStatus(current) ? current : "Not Validated";

            _repo.Upsert(person, statusToSave);
            person.Status = statusToSave;

            IsSaveConfirmOpen = false;

            // refresh lists so overlay stays correct
            LoadNotYet();
            LoadValidated();
        }

        // ===== Validate modal =====
        [RelayCommand]
        private void OpenValidateModal(ValidatorRecord? person)
        {
            var p = person ?? SelectedPerson;
            if (p is null) return;

            SelectedPerson = p;
            ValidateSelectedStatus = string.IsNullOrWhiteSpace(p.Status) || p.Status.Equals("Not Validated", StringComparison.OrdinalIgnoreCase)
                ? "Endorsed"
                : CanonicalStatus(p.Status);

            IsValidateModalOpen = true;
        }

        [RelayCommand] private void CloseValidateModal() => IsValidateModalOpen = false;

        [RelayCommand]
        private void ConfirmValidate()
        {
            var person = SelectedPerson;
            if (person is null) return;

            var newStatus = CanonicalStatus(ValidateSelectedStatus);
            if (!IsValidatedStatus(newStatus)) return;

            // write to DB
            person.Status = newStatus;
            _repo.Upsert(person, newStatus);

            // switch to Validated tab + correct status pill
            ActiveMainTab = ValidatorsMainTab.Validated;
            ActiveStatusTab = newStatus switch
            {
                "Pending" => ValidatorsStatusTab.Pending,
                "Rejected" => ValidatorsStatusTab.Rejected,
                _ => ValidatorsStatusTab.Endorsed
            };

            // refresh lists
            LoadNotYet();
            LoadValidated();

            // keep selection on the newly validated row if present
            SelectedPerson =
                ValidatedItems.FirstOrDefault(x => string.Equals(x.BeneficiaryId, person.BeneficiaryId, StringComparison.OrdinalIgnoreCase))
                ?? ValidatedItems.FirstOrDefault();

            IsValidateModalOpen = false;
        }
    }
}
