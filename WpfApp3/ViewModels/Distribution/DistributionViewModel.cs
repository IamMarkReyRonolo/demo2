using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using WpfApp3.Models;
using WpfApp3.Services;
using static WpfApp3.ViewModels.Validators.ValidatorsViewModel;

namespace WpfApp3.ViewModels.Distribution
{
    public partial class DistributionViewModel : ObservableObject
    {
        private readonly AllotmentsRepository _allotmentRepo = new();
        private readonly AllotmentBeneficiariesRepository _assignRepo = new();

        private List<BeneficiaryRecord> _cache = new();

        // paging (main page)
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

        // shows scanned text in UI textbox
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

        public ObservableCollection<string> ClassificationOptions { get; } = new();
        [ObservableProperty] private string? selectedClassification;

        // ===== Release modal paging =====
        [ObservableProperty] private int releaseCurrentPage = 1;
        public int ReleasePageSize { get; } = 8;

        public ObservableCollection<BeneficiaryRecord> ReleasePagedItems { get; } = new();
        public ObservableCollection<int> ReleasePageNumbers { get; } = new();

        public int ReleaseTotalRecords => ReleaseFiltered().Count();
        public int ReleaseTotalPages => Math.Max(1, (int)Math.Ceiling(ReleaseTotalRecords / (double)ReleasePageSize));

        [ObservableProperty] private string? releaseSelectedClassification = "All";

        [ObservableProperty] private BeneficiaryRecord? pendingRelease;

        private readonly BeneficiariesRepository _beneRepo = new();

        [ObservableProperty] private BitmapImage? confirmProfileImagePreview;

        public bool ConfirmHasProfileImage => ConfirmProfileImagePreview != null;
        public ObservableCollection<ReleaseHistoryItem> ConfirmReleaseHistory { get; } = new();
        public bool HasConfirmReleaseHistory => ConfirmReleaseHistory.Count > 0;

        public DistributionViewModel()
        {
            LoadProjectsFromDb();


            ClassificationOptions.Add("All");
            ClassificationOptions.Add("PWD");
            ClassificationOptions.Add("Senior Citizen");
            ClassificationOptions.Add("Indigenous");
            ClassificationOptions.Add("Farmer");
            ClassificationOptions.Add("Vendor");
            ClassificationOptions.Add("None");

            SelectedClassification = "All";
            _ready = true;

            SelectedProject = Projects.FirstOrDefault();
            Reload();
        }

        partial void OnSelectedClassificationChanged(string? value)
        {
            if (!_ready) return;
            CurrentPage = 1;
            ApplyPaging();
        }

        private List<BeneficiaryRecord> Filtered()
        {
            IEnumerable<BeneficiaryRecord> src = _cache;

            var cls = (SelectedClassification ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(cls) && !cls.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (cls.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    src = src.Where(x =>
                    {
                        var v = (x.Classification ?? "").Trim();
                        return string.IsNullOrWhiteSpace(v) || v.Equals("None", StringComparison.OrdinalIgnoreCase);
                    });
                }
                else
                {
                    src = src.Where(x =>
                        string.Equals((x.Classification ?? "").Trim(), cls, StringComparison.OrdinalIgnoreCase));
                }
            }

            return src.ToList();
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
            var filtered = Filtered();

            if (CurrentPage < 1) CurrentPage = 1;
            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
            if (CurrentPage > totalPages) CurrentPage = totalPages;

            Items.Clear();
            foreach (var it in filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                Items.Add(it);

            PageNumbers.Clear();
            for (int i = 1; i <= totalPages; i++) PageNumbers.Add(i);

            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(FoundText));
        }

        // ===== Reload list inside Release modal (keeps Released column updated) =====
        private void ReloadReleaseItems()
        {
            ReleaseItems.Clear();
            if (SelectedProject is null) return;

            foreach (var r in _assignRepo.GetAssignedEndorsed(SelectedProject.Id))
                ReleaseItems.Add(r);

            if (ReleaseCurrentPage > ReleaseTotalPages)
                ReleaseCurrentPage = ReleaseTotalPages;

            ApplyReleasePaging();
            OnPropertyChanged(nameof(ReleaseProgressText));
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
            // optional – reuse Beneficiaries project modal later if you want
            ShowToast("Project details is optional here.", "info");
        }

        [RelayCommand]
        private void OpenReleaseSession()
        {
            if (SelectedProject is null) return;

            ScanInput = "";

            ReleaseCurrentPage = 1;     // ✅ start at page 1
            ReleaseSelectedClassification = SelectedClassification ?? "All";
            ReloadReleaseItems();

            IsReleaseSessionOpen = true;
        }

        [RelayCommand]
        private void CloseReleaseSession()
        {
            IsReleaseSessionOpen = false;
            ScanInput = "";
        }

        // called by code-behind (global scan capture) on Enter
        [RelayCommand]
        private void Scan(string? scanned)
        {
            var raw = (scanned ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            // ✅ always show scanned value in textbox
            ScanInput = raw;

            // ✅ compare as-is (case-insensitive)
            var hit = ReleaseItems.FirstOrDefault(x =>
                string.Equals((x.BeneficiaryId ?? "").Trim(), raw, StringComparison.OrdinalIgnoreCase));

            var idx = ReleaseItems.IndexOf(hit);
            if (idx >= 0)
            {
                ReleaseCurrentPage = (idx / ReleasePageSize) + 1;
                ApplyReleasePaging();
            }

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
            PendingRelease = hit;
            OnPropertyChanged(nameof(PendingRelease));
            SelectedReleaseRow = hit;

            HydratePendingReleaseFromDb(hit);
            LoadConfirmReleaseHistory(hit.Id);

            ConfirmId = hit.BeneficiaryId; // show barcode string
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
            ScanInput = "";
            PendingRelease = null;
            OnPropertyChanged(nameof(PendingRelease));
            ConfirmProfileImagePreview = null;
            ConfirmReleaseHistory.Clear();
            OnPropertyChanged(nameof(HasConfirmReleaseHistory));
        }

        [RelayCommand]
        private void ConfirmRelease()
        {
            if (SelectedProject is null || _pendingRelease is null) return;

            _assignRepo.MarkReleased(SelectedProject.Id, _pendingRelease.Id);

            // ✅ update modal table
            ReloadReleaseItems();

            // ✅ update main page table
            Reload();

            IsConfirmReleaseOpen = false;
            ShowToast($"Released to ID {_pendingRelease.BeneficiaryId}", "success");
            _pendingRelease = null;
            ScanInput = "";
        }

        partial void OnReleaseCurrentPageChanged(int value) => ApplyReleasePaging();

        private void ApplyReleasePaging()
        {
            var filtered = ReleaseFiltered().ToList();

            if (ReleaseCurrentPage < 1) ReleaseCurrentPage = 1;
            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)ReleasePageSize));
            if (ReleaseCurrentPage > totalPages) ReleaseCurrentPage = totalPages;

            ReleasePagedItems.Clear();
            foreach (var it in filtered.Skip((ReleaseCurrentPage - 1) * ReleasePageSize).Take(ReleasePageSize))
                ReleasePagedItems.Add(it);

            ReleasePageNumbers.Clear();
            for (int i = 1; i <= totalPages; i++) ReleasePageNumbers.Add(i);

            OnPropertyChanged(nameof(ReleaseTotalRecords));
            OnPropertyChanged(nameof(ReleaseTotalPages));
        }

        partial void OnReleaseSelectedClassificationChanged(string? value)
        {
            if (!_ready) return;
            ReleaseCurrentPage = 1;
            ApplyReleasePaging();
        }

        private IEnumerable<BeneficiaryRecord> ReleaseFiltered()
        {
            IEnumerable<BeneficiaryRecord> src = ReleaseItems;

            var cls = (ReleaseSelectedClassification ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(cls) && !cls.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (cls.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    src = src.Where(x =>
                    {
                        var v = (x.Classification ?? "").Trim();
                        return string.IsNullOrWhiteSpace(v) || v.Equals("None", StringComparison.OrdinalIgnoreCase);
                    });
                }
                else
                {
                    src = src.Where(x =>
                        string.Equals((x.Classification ?? "").Trim(), cls, StringComparison.OrdinalIgnoreCase));
                }
            }

            return src;
        }



        // paging (main page)
        [RelayCommand] private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void GoToPage(int page) { CurrentPage = page; }


        [RelayCommand] private void ReleasePreviousPage() { if (ReleaseCurrentPage > 1) ReleaseCurrentPage--; }
        [RelayCommand] private void ReleaseNextPage() { if (ReleaseCurrentPage < ReleaseTotalPages) ReleaseCurrentPage++; }
        [RelayCommand] private void ReleaseGoToPage(int page) { ReleaseCurrentPage = page; }

        partial void OnConfirmProfileImagePreviewChanged(BitmapImage? value)
        {
            OnPropertyChanged(nameof(ConfirmHasProfileImage));
        }

        private static BitmapImage? ToBitmap(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0) return null;

            try
            {
                using var ms = new MemoryStream(bytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void HydratePendingReleaseFromDb(BeneficiaryRecord target)
        {
            try
            {
                // ✅ internal id is target.Id (b.id)
                var full = _beneRepo.GetDetailsByInternalId(target.Id);
                if (full is null)
                {
                    ConfirmProfileImagePreview = null;
                    return;
                }

                // ✅ fill the missing fields so the modal bindings work
                target.BeneficiaryId = full.BeneficiaryId;
                target.CivilRegistryId = full.CivilRegistryId;
                target.FirstName = full.FirstName;
                target.MiddleName = full.MiddleName;
                target.LastName = full.LastName;
                target.Gender = full.Gender;
                target.Barangay = full.Barangay;
                target.Classification = string.IsNullOrWhiteSpace(full.Classification) ? "None" : full.Classification;
                target.PresentAddress = full.PresentAddress;

                // ✅ image for modal avatar
                ConfirmProfileImagePreview = ToBitmap(full.ProfileImage);
            }
            catch (Exception ex)
            {
                ConfirmProfileImagePreview = null;
                ShowToast($"Failed to load beneficiary details: {ex.Message}", "error");
            }
        }


        public sealed class ReleaseHistoryItem
        {
            public int AllotmentId { get; set; }
            public DateTime ReleasedAt { get; set; }
            public string ShareText { get; set; } = "";
            public bool IsLast { get; set; }

            public string ReleasedAtText =>
                ReleasedAt.ToString("MMM dd, yyyy • hh:mm tt", CultureInfo.InvariantCulture);

            public string Description => $"Allotment #{AllotmentId} • {ShareText}";
        }

        private void LoadConfirmReleaseHistory(int beneficiaryInternalId)
        {
            ConfirmReleaseHistory.Clear();


            try
            {
                var rows = _assignRepo.GetReleaseHistory(beneficiaryInternalId);

                var items = rows.Select(x =>
                {
                    var share = x.ShareAmount is not null
                        ? $"₱ {x.ShareAmount.Value:N2}"
                        : (x.ShareQty is not null
                            ? $"{x.ShareQty.Value} {x.ShareUnit}".Trim()
                            : "-");

                    return new ReleaseHistoryItem
                    {
                        AllotmentId = x.AllotmentId,
                        ReleasedAt = x.ReleasedAt,
                        ShareText = share
                    };
                }).ToList();

                for (int i = 0; i < items.Count; i++)
                    items[i].IsLast = (i == items.Count - 1);

                foreach (var it in items)
                    ConfirmReleaseHistory.Add(it);
            }
            catch
            {
                // ignore history failures; don't block confirm release
            }

            OnPropertyChanged(nameof(HasConfirmReleaseHistory));
        }
    }
}