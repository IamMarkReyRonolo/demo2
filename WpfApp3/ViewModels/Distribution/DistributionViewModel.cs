using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.ViewModels.Distribution
{
    public partial class DistributionViewModel : ObservableObject
    {
        private readonly AllotmentsRepository _allotmentRepo = new();
        private readonly AllotmentBeneficiariesRepository _assignRepo = new();

        private List<BeneficiaryRecord> _cache = new();

        [ObservableProperty] private int currentPage = 1;
        public int PageSize { get; } = 8;

        public ObservableCollection<AllotmentProjectOption> Projects { get; } = new();
        [ObservableProperty] private AllotmentProjectOption? selectedProject;

        public ObservableCollection<BeneficiaryRecord> Items { get; } = new();
        public ObservableCollection<int> PageNumbers { get; } = new();

        public int TotalRecords => _cache.Count;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        public string FoundText => $"Found {TotalRecords} records";

        public string TotalBudgetText =>
            SelectedProject is null ? "Total Budget: -" : $"Total Budget: {SelectedProject.TotalBudgetText}";

        // ===== Toast =====
        [ObservableProperty] private bool isToastVisible;
        [ObservableProperty] private string toastMessage = "";
        [ObservableProperty] private string toastBackground = "#2E3A59";
        private CancellationTokenSource? _toastCts;

        // ===== Release session modal =====
        [ObservableProperty] private bool isReleaseSessionOpen;
        public ObservableCollection<BeneficiaryRecord> ReleaseItems { get; } = new();
        [ObservableProperty] private BeneficiaryRecord? selectedReleaseRow;
        [ObservableProperty] private string scanInput = "";

        public string ReleaseProjectText => SelectedProject is null ? "" : $"Project: {SelectedProject.ProjectName}";
        public string ReleaseBudgetText => SelectedProject is null ? "" : $"Budget: {SelectedProject.TotalBudgetText}";
        public string ReleaseProgressText =>
            $"Released: {ReleaseItems.Count(x => x.IsReleased)}/{ReleaseItems.Count}";

        // ===== Confirm release modal =====
        [ObservableProperty] private bool isConfirmReleaseOpen;

        private BeneficiaryRecord? _pendingRelease;

        [ObservableProperty] private string confirmId = "";
        [ObservableProperty] private string confirmName = "";
        [ObservableProperty] private string confirmBarangay = "";
        [ObservableProperty] private string confirmClassification = "";
        [ObservableProperty] private string confirmShare = "";

        private bool _ready;

        public DistributionViewModel()
        {
            LoadProjectsFromDb();
            _ready = true;

            SelectedProject = Projects.FirstOrDefault();
            Reload();
        }

        partial void OnSelectedProjectChanged(AllotmentProjectOption? value)
        {
            if (!_ready) return;
            CurrentPage = 1;
            Reload();
            OnPropertyChanged(nameof(TotalBudgetText));
        }

        partial void OnCurrentPageChanged(int value) => ApplyPaging();

        private void LoadProjectsFromDb()
        {
            Projects.Clear();
            foreach (var p in _allotmentRepo.GetAllProjects())
                Projects.Add(p);
        }

        private void Reload()
        {
            _cache.Clear();

            if (SelectedProject is null)
            {
                ApplyPaging();
                return;
            }

            _cache = _assignRepo.GetAssignedEndorsed(SelectedProject.Id);
            ApplyPaging();
        }

        private void ApplyPaging()
        {
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Items.Clear();
            foreach (var it in _cache.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                Items.Add(it);

            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++) PageNumbers.Add(i);

            OnPropertyChanged(nameof(FoundText));
        }

        private async void ShowToast(string msg, string kind)
        {
            _toastCts?.Cancel();
            _toastCts = new CancellationTokenSource();
            var token = _toastCts.Token;

            ToastMessage = msg;
            ToastBackground = kind switch
            {
                "success" => "#16A34A",
                "error" => "#E11D48",
                "warning" => "#F59E0B",
                _ => "#2E3A59"
            };

            IsToastVisible = true;

            try
            {
                await Task.Delay(2200, token);
                IsToastVisible = false;
            }
            catch { }
        }

        // ================= Commands =================

        [RelayCommand]
        private void OpenProjectDetails()
        {
            // Optional: you can reuse the Beneficiaries project details modal later
            ShowToast("Project details is optional here.", "info");
        }

        [RelayCommand]
        private void OpenReleaseSession()
        {
            if (SelectedProject is null) return;

            ReleaseItems.Clear();
            foreach (var r in _assignRepo.GetAssignedEndorsed(SelectedProject.Id))
                ReleaseItems.Add(r);

            OnPropertyChanged(nameof(ReleaseProjectText));
            OnPropertyChanged(nameof(ReleaseBudgetText));
            OnPropertyChanged(nameof(ReleaseProgressText));

            IsReleaseSessionOpen = true;
        }

        [RelayCommand]
        private void CloseReleaseSession()
        {
            IsReleaseSessionOpen = false;
        }

        // called by code-behind on Enter key
        [RelayCommand]
        private void Scan(string? scanned)
        {
            var raw = (scanned ?? "").Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return;

            if (!int.TryParse(raw, out var id))
            {
                ShowToast($"Scan error: {raw}", "error");
                return;
            }

            var hit = ReleaseItems.FirstOrDefault(x => x.Id == id);
            if (hit is null)
            {
                ShowToast($"Scan not found: {raw}", "error");
                return;
            }

            if (hit.IsReleased)
            {
                ShowToast($"Already released: {raw}", "warning");
                return;
            }

            _pendingRelease = hit;
            SelectedReleaseRow = hit;

            ConfirmId = hit.Id.ToString(CultureInfo.InvariantCulture);
            ConfirmName = $"{hit.FirstName} {hit.LastName}".Trim();
            ConfirmBarangay = hit.Barangay;
            ConfirmClassification = hit.Classification;
            ConfirmShare = hit.ShareText;

            IsConfirmReleaseOpen = true;
            ShowToast($"Scan success: {raw}", "success");
        }

        [RelayCommand]
        private void CloseConfirmRelease()
        {
            IsConfirmReleaseOpen = false;
            _pendingRelease = null;
        }

        [RelayCommand]
        private void ConfirmRelease()
        {
            if (SelectedProject is null || _pendingRelease is null) return;

            _assignRepo.MarkReleased(SelectedProject.Id, _pendingRelease.Id);

            // update local view
            _pendingRelease.IsReleased = true;
            OnPropertyChanged(nameof(ReleaseProgressText));

            // refresh main table
            Reload();

            IsConfirmReleaseOpen = false;
            ShowToast($"Released to ID {_pendingRelease.Id}", "success");
            _pendingRelease = null;
        }

        // paging
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }
    }
}