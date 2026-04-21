using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using JobSearch.Desktop.Commands;
using JobSearch.Desktop.Models;
using JobSearch.Desktop.Services;

namespace JobSearch.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly RecommendationApiService _apiService;
    private readonly CollectorHostService _collectorHostService;
    private readonly UserSettingsService _settingsService;
    private readonly AsyncRelayCommand _searchCommand;
    private readonly AsyncRelayCommand _startRealtimeSearchCommand;
    private readonly AsyncRelayCommand _applySearchProfileCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand<string> _openJobUrlCommand;
    private readonly AsyncRelayCommand _saveKeywordsCommand;

    private AgeGroupOption? _selectedAgeGroup;
    private RecommendationItem? _selectedRecommendation;
    private SearchProfileOption? _selectedSearchProfile;
    private string _experienceLevel = "\uC804\uCCB4";
    private string _employmentType = "\uC804\uCCB4";
    private string _region = "\uC804\uCCB4";
    private string _industry = "\uC804\uCCB4";
    private int? _minSalary;
    private string _statusMessage = "\uC2DC\uC2A4\uD15C \uC900\uBE44 \uC644\uB8CC";
    private string _footerMessage = "SQLite DB\uC640 API \uC11C\uBC84\uB97C \uD1B5\uD574 \uC2E4\uC2DC\uAC04 \uB370\uC774\uD130\uB97C \uC870\uD68C\uD569\uB2C8\uB2E4.";
    private string _resultSummary = "\uC544\uC9C1 \uC870\uD68C\uB41C \uACB0\uACFC\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
    private string _dashboardStatsText = "DB \uD1B5\uACC4\uB97C \uBD88\uB7EC\uC624\uB294 \uC911...";
    private string _interestKeywordsText = string.Empty;
    private string _notificationMessage = string.Empty;
    private bool _isEmpty = true;
    private bool _isResettingFilters;
    private readonly DispatcherTimer _autoRefreshTimer;
    private RefreshOption _selectedRefreshOption = default!;

    public MainViewModel()
    {
        _apiService = new RecommendationApiService("http://localhost:5058/");
        _collectorHostService = new CollectorHostService();
        _settingsService = new UserSettingsService();

        Recommendations = [];
        AgeGroupOptions =
        [
            new AgeGroupOption(null, "\uC804\uCCB4"),
            new AgeGroupOption("Twenties", "20\uB300"),
            new AgeGroupOption("Thirties", "30\uB300"),
            new AgeGroupOption("Forties", "40\uB300"),
            new AgeGroupOption("FiftiesAndAbove", "50\uB300 \uC774\uC0C1")
        ];

        ExperienceOptions = ["\uC804\uCCB4", "\uC2E0\uC785", "\uACBD\uB825", "\uC2E0\uC785~\uACBD\uB825", "\uC778\uD134"];
        EmploymentOptions = ["\uC804\uCCB4", "\uC815\uADDC\uC9C1", "\uACC4\uC57D\uC9C1", "\uC778\uD134\uC9C1", "\uD504\uB9AC\uB79C\uC11C", "\uC544\uB974\uBC14\uC774\uD2B8"];
        RegionOptions = ["\uC804\uCCB4", "\uC11C\uC6B8", "\uACBD\uAE30", "\uC778\uCC9C", "\uB300\uC804", "\uB300\uAD6C", "\uBD80\uC0B0", "\uC6B8\uC0B0", "\uAD11\uC8FC", "\uAC15\uC6D0", "\uC81C\uC8FC", "\uCDA9\uB0A8", "\uCDA9\uBD81", "\uC804\uB0A8", "\uC804\uBD81", "\uACBD\uB0A8", "\uACBD\uBD81", "\uC138\uC885"];
        IndustryOptions = ["\uC804\uCCB4", "IT", "\uBC31\uC5D4\uB4DC", "\uD504\uB860\uD2B8\uC5D4\uB4DC", "\uB370\uC774\uD130", "\uAC8C\uC784", "\uD558\uB4DC\uC6E8\uC5B4", "\uB9C8\uCF00\uD305", "\uAE30\uD68D", "\uC601\uC5C5", "\uACBD\uC601/\uC0AC\uBB34"];

        RefreshOptions =
        [
            new RefreshOption(0, "\uC0AC\uC6A9 \uC548 \uD568"),
            new RefreshOption(1, "1\uBD84 \uB9C8\uB2E4"),
            new RefreshOption(5, "5\uBD84 \uB9C8\uB2E4"),
            new RefreshOption(10, "10\uBD84 \uB9C8\uB2E4"),
            new RefreshOption(15, "15\uBD84 \uB9C8\uB2E4"),
            new RefreshOption(30, "30\uBD84 \uB9C8\uB2E4"),
            new RefreshOption(60, "1\uC2DC\uAC04 \uB9C8\uB2E4"),
            new RefreshOption(180, "3\uC2DC\uAC04 \uB9C8\uB2E4"),
            new RefreshOption(360, "6\uC2DC\uAC04 \uB9C8\uB2E4"),
            new RefreshOption(720, "12\uC2DC\uAC04 \uB9C8\uB2E4"),
            new RefreshOption(1440, "24\uC2DC\uAC04 \uB9C8\uB2E4")
        ];
        _selectedRefreshOption = RefreshOptions[0];

        SearchProfileOptions =
        [
            new SearchProfileOption(
                "general",
                "\uC804\uCCB4 \uD0D0\uC0C9",
                [],
                0),
            new SearchProfileOption(
                "automation-equipment",
                "\uC790\uB3D9\uD654 \uC7A5\uBE44",
                [
                    "\uC790\uB3D9\uD654", "FA", "PLC", "HMI", "\uC81C\uC5B4", "\uBAA8\uC158\uC81C\uC5B4", "\uC11C\uBCF4", "\uC778\uBC84\uD130", "\uC0B0\uC5C5\uC6A9\uC7A5\uBE44"
                ],
                1),
            new SearchProfileOption(
                "semiconductor-inspection",
                "\uBC18\uB3C4\uCCB4/\uAC80\uC0AC \uC7A5\uBE44",
                [
                    "\uBC18\uB3C4\uCCB4\uC7A5\uBE44", "\uAC80\uC0AC\uC7A5\uBE44", "\uBE44\uC804\uAC80\uC0AC", "\uBA38\uC2E0\uBE44\uC804", "\uACC4\uCE21", "\uC13C\uC11C", "\uC124\uBE44", "\uC7A5\uBE44\uAC1C\uBC1C", "\uC81C\uC5B4\uAC1C\uBC1C"
                ],
                1),
            new SearchProfileOption(
                "manufacturing-facility",
                "\uC124\uBE44/\uC81C\uC870 \uC7A5\uBE44",
                [
                    "\uC124\uBE44", "\uC7A5\uBE44", "\uACF5\uC7A5\uC790\uB3D9\uD654", "\uC0DD\uC0B0\uAE30\uC220", "\uC124\uBE44\uC81C\uC5B4", "\uC720\uC9C0\uBCF4\uC218", "\uC2DC\uC6B4\uC804", "\uD544\uB4DC\uC5D4\uC9C0\uB2C8\uC5B4", "\uC0B0\uC5C5\uC790\uB3D9\uD654"
                ],
                5)
        ];

        _selectedAgeGroup = AgeGroupOptions[0];
        _selectedSearchProfile = SearchProfileOptions[0];

        _autoRefreshTimer = new DispatcherTimer();
        _autoRefreshTimer.Tick += (s, e) => _ = SearchAsync();

        _searchCommand = new AsyncRelayCommand(SearchAsync);
        _startRealtimeSearchCommand = new AsyncRelayCommand(StartRealtimeSearchAsync);
        _applySearchProfileCommand = new AsyncRelayCommand(ApplySelectedSearchProfileAsync);
        _resetFiltersCommand = new RelayCommand(ResetFilters);
        _openJobUrlCommand = new RelayCommand<string>(OpenJobUrl);
        _saveKeywordsCommand = new AsyncRelayCommand(SaveKeywordsAsync);
    }

    public ObservableCollection<RecommendationItem> Recommendations { get; }
    public IReadOnlyList<AgeGroupOption> AgeGroupOptions { get; }
    public IReadOnlyList<string> ExperienceOptions { get; }
    public IReadOnlyList<string> EmploymentOptions { get; }
    public IReadOnlyList<string> RegionOptions { get; }
    public IReadOnlyList<string> IndustryOptions { get; }
    public IReadOnlyList<RefreshOption> RefreshOptions { get; }
    public IReadOnlyList<SearchProfileOption> SearchProfileOptions { get; }
    public ICommand SearchCommand => _searchCommand;
    public ICommand StartRealtimeSearchCommand => _startRealtimeSearchCommand;
    public ICommand ApplySearchProfileCommand => _applySearchProfileCommand;
    public ICommand ResetFiltersCommand => _resetFiltersCommand;
    public ICommand OpenJobUrlCommand => _openJobUrlCommand;
    public ICommand SaveKeywordsCommand => _saveKeywordsCommand;

    public AgeGroupOption? SelectedAgeGroup
    {
        get => _selectedAgeGroup;
        set
        {
            if (SetProperty(ref _selectedAgeGroup, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public RecommendationItem? SelectedRecommendation
    {
        get => _selectedRecommendation;
        set => SetProperty(ref _selectedRecommendation, value);
    }

    public SearchProfileOption? SelectedSearchProfile
    {
        get => _selectedSearchProfile;
        set => SetProperty(ref _selectedSearchProfile, value);
    }

    public string ExperienceLevel
    {
        get => _experienceLevel;
        set
        {
            if (SetProperty(ref _experienceLevel, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public string EmploymentType
    {
        get => _employmentType;
        set
        {
            if (SetProperty(ref _employmentType, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public string Region
    {
        get => _region;
        set
        {
            if (SetProperty(ref _region, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public string Industry
    {
        get => _industry;
        set
        {
            if (SetProperty(ref _industry, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public int? MinSalary
    {
        get => _minSalary;
        set
        {
            if (SetProperty(ref _minSalary, value))
            {
                TriggerAutoSearch();
            }
        }
    }

    public RefreshOption SelectedRefreshOption
    {
        get => _selectedRefreshOption;
        set
        {
            if (SetProperty(ref _selectedRefreshOption, value))
            {
                UpdateTimer();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string FooterMessage
    {
        get => _footerMessage;
        private set => SetProperty(ref _footerMessage, value);
    }

    public string ResultSummary
    {
        get => _resultSummary;
        private set => SetProperty(ref _resultSummary, value);
    }

    public string DashboardStatsText
    {
        get => _dashboardStatsText;
        private set => SetProperty(ref _dashboardStatsText, value);
    }

    public string InterestKeywordsText
    {
        get => _interestKeywordsText;
        set => SetProperty(ref _interestKeywordsText, value);
    }

    public string NotificationMessage
    {
        get => _notificationMessage;
        private set => SetProperty(ref _notificationMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public void UpdateStartupState(string statusMessage, string footerMessage)
    {
        StatusMessage = statusMessage;
        FooterMessage = footerMessage;
    }

    public void ApplyStartupFailure(string errorMessage)
    {
        Recommendations.Clear();
        SelectedRecommendation = null;
        IsEmpty = true;
        NotificationMessage = string.Empty;
        ResultSummary = "\u0053\u0065\u0061\u0072\u0063\u0068\u0020\u0041\u0050\u0049\uAC00 \uC544\uC9C1 \uC900\uBE44\uB418\uC9C0 \uC54A\uC544 \uCD94\uCC9C \uB370\uC774\uD130\uB97C \uBD88\uB7EC\uC624\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4\u002E";
        DashboardStatsText = "\uD1B5\uACC4 \uC815\uBCF4\uB97C \uBD88\uB7EC\uC624\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4\u002E";
        StatusMessage = "\uBC31\uADF8\uB77C\uC6B4\uB4DC \uC2DC\uC791 \uC2E4\uD328";
        FooterMessage = errorMessage;
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(settings.SelectedSearchProfileId))
        {
            var savedProfile = SearchProfileOptions.FirstOrDefault(option =>
                string.Equals(option.Id, settings.SelectedSearchProfileId, StringComparison.OrdinalIgnoreCase));

            if (savedProfile is not null)
            {
                SelectedSearchProfile = savedProfile;
            }
        }

        if (settings.InterestKeywords.Length > 0)
        {
            InterestKeywordsText = string.Join(", ", settings.InterestKeywords);
        }

        var option = RefreshOptions.FirstOrDefault(o => o.Minutes == settings.AutoRefreshMinutes);
        if (option is not null)
        {
            SelectedRefreshOption = option;
        }

        await LoadDashboardStatsAsync().ConfigureAwait(true);
        await SearchAsync().ConfigureAwait(true);
    }

    private async Task StartRealtimeSearchAsync()
    {
        try
        {
            var realtimeOption = RefreshOptions.FirstOrDefault(option => option.Minutes == 1) ?? RefreshOptions[0];

            if (!ReferenceEquals(SelectedRefreshOption, realtimeOption))
            {
                SelectedRefreshOption = realtimeOption;
            }
            else
            {
                UpdateTimer();
            }

            StatusMessage = "\uC2E4\uC2DC\uAC04 \uAC80\uC0C9\uC744 \uC2DC\uC791\uD558\uACE0 \uC788\uC2B5\uB2C8\uB2E4...";
            FooterMessage = "\uC0AC\uB78C\uC778\uACFC \uC7A1\uCF54\uB9AC\uC544 \uB370\uC774\uD130\uB97C \uBC31\uADF8\uB77C\uC6B4\uB4DC\uC5D0\uC11C \uC218\uC9D1\uD558\uB294 \uC911\uC785\uB2C8\uB2E4.";

            var collectorResult = await _collectorHostService.RunAsync(CancellationToken.None).ConfigureAwait(true);
            if (!collectorResult.Succeeded)
            {
                StatusMessage = "\uC2E4\uC2DC\uAC04 \uC218\uC9D1 \uC2E4\uD328";
                FooterMessage = string.IsNullOrWhiteSpace(collectorResult.StandardError)
                    ? $"\uC218\uC9D1\uAE30 \uC885\uB8CC \uCF54\uB4DC: {collectorResult.ExitCode}"
                    : collectorResult.StandardError;
                return;
            }

            StatusMessage = "\uC218\uC9D1 \uC644\uB8CC, \uCD5C\uC2E0 \uACF5\uACE0\uB97C \uB2E4\uC2DC \uBD88\uB7EC\uC624\uB294 \uC911\uC785\uB2C8\uB2E4.";
            FooterMessage = string.IsNullOrWhiteSpace(collectorResult.StandardOutput)
                ? $"{realtimeOption.DisplayName} \uC8FC\uAE30 \uC2E4\uC2DC\uAC04 \uAC80\uC0C9 \uACB0\uACFC\uB97C \uC801\uC6A9\uD569\uB2C8\uB2E4."
                : collectorResult.StandardOutput;

            await SearchAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            StatusMessage = "\uC2E4\uC2DC\uAC04 \uC218\uC9D1 \uC2E4\uD328";
            FooterMessage = $"\uC624\uB958: {exception.Message}";
        }
    }

    private async Task ApplySelectedSearchProfileAsync()
    {
        var profile = SelectedSearchProfile ?? SearchProfileOptions[0];

        InterestKeywordsText = string.Join(", ", profile.DefaultKeywords);

        var refreshOption = RefreshOptions.FirstOrDefault(option => option.Minutes == profile.RecommendedRefreshMinutes)
            ?? RefreshOptions[0];
        SelectedRefreshOption = refreshOption;

        StatusMessage = "\uD0D0\uC0C9 \uC124\uC815\uC744 \uC801\uC6A9\uD588\uC2B5\uB2C8\uB2E4.";
        FooterMessage = profile.Id == "general"
            ? "\uAE30\uBCF8 \uD0D0\uC0C9 \uC124\uC815\uC744 \uC801\uC6A9\uD588\uC2B5\uB2C8\uB2E4."
            : $"{profile.DisplayName} \uD504\uB85C\uD544\uC5D0 \uB9DE\uB294 \uD0A4\uC6CC\uB4DC\uC640 \uAC31\uC2E0 \uC8FC\uAE30\uB97C \uC801\uC6A9\uD588\uC2B5\uB2C8\uB2E4.";

        await SaveCurrentSettingsAsync().ConfigureAwait(true);
        await SearchAsync().ConfigureAwait(true);
    }

    private async Task SaveKeywordsAsync()
    {
        await SaveCurrentSettingsAsync().ConfigureAwait(true);

        StatusMessage = "\uC124\uC815 \uBC0F \uAD00\uC2EC \uD0A4\uC6CC\uB4DC\uAC00 \uC800\uC7A5\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
        FooterMessage = "\uC800\uC7A5\uD55C \uC815\uBCF4\uB97C \uBC14\uD0D5\uC73C\uB85C \uAC80\uC0C9\uACFC \uC8FC\uAE30\uBCC4 \uC54C\uB9BC\uC744 \uC81C\uACF5\uD569\uB2C8\uB2E4.";

        await SearchAsync().ConfigureAwait(true);
    }

    private async Task SaveCurrentSettingsAsync()
    {
        var keywords = InterestKeywordsText
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var settings = new UserSettings
        {
            InterestKeywords = keywords,
            AutoRefreshMinutes = SelectedRefreshOption.Minutes,
            SelectedSearchProfileId = SelectedSearchProfile?.Id
        };

        await _settingsService.SaveAsync(settings).ConfigureAwait(true);
    }

    private async Task LoadDashboardStatsAsync()
    {
        var stats = await _apiService.GetDashboardStatsAsync(CancellationToken.None).ConfigureAwait(true);
        if (stats is not null)
        {
            DashboardStatsText = $"\uCD1D \uB370\uC774\uD130 {stats.TotalCount}\uAC74 (\uC0AC\uB78C\uC778 {stats.SaraminCount}\uAC74 / \uC7A1\uCF54\uB9AC\uC544 {stats.JobKoreaCount}\uAC74) | \uAC31\uC2E0: {stats.LastUpdatedTime}";
        }
        else
        {
            DashboardStatsText = "\uD1B5\uACC4 \uC815\uBCF4\uB97C \uBD88\uB7EC\uC624\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.";
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            StatusMessage = "\uB370\uC774\uD130\uBCA0\uC774\uC2A4\uC5D0\uC11C \uCD94\uCC9C \uACF5\uACE0\uB97C \uC870\uD68C\uD558\uACE0 \uC788\uC2B5\uB2C8\uB2E4...";

            var reqExp = ExperienceLevel == "\uC804\uCCB4" ? string.Empty : ExperienceLevel;
            var reqEmp = EmploymentType == "\uC804\uCCB4" ? string.Empty : EmploymentType;
            var reqReg = Region == "\uC804\uCCB4" ? string.Empty : Region;
            var reqInd = Industry == "\uC804\uCCB4" ? string.Empty : Industry;

            var items = await _apiService.GetRecommendationsAsync(
                    SelectedAgeGroup?.Value,
                    reqExp,
                    reqEmp,
                    reqReg,
                    reqInd,
                    MinSalary,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Recommendations.Clear();
            foreach (var item in items)
            {
                Recommendations.Add(item);
            }

            SelectedRecommendation = Recommendations.FirstOrDefault();
            IsEmpty = Recommendations.Count == 0;
            ResultSummary = Recommendations.Count == 0
                ? "\uC870\uAC74\uC5D0 \uB9DE\uB294 \uCD94\uCC9C \uACB0\uACFC\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4. \uD544\uD130\uB97C \uC644\uD654\uD558\uAC70\uB098 \uD0A4\uC6CC\uB4DC\uB97C \uC870\uC815\uD574 \uBCF4\uC138\uC694."
                : $"DB \uC870\uD68C \uACB0\uACFC {Recommendations.Count}\uAC74\uC758 \uACF5\uACE0\uB97C \uCC3E\uC558\uC2B5\uB2C8\uB2E4.";
            StatusMessage = Recommendations.Count == 0 ? "\uC870\uD68C \uC644\uB8CC: \uACB0\uACFC \uC5C6\uC74C" : "\uC870\uD68C \uC644\uB8CC";
            FooterMessage = $"SQLite DB \uC5F0\uB3D9 \uBAA8\uB4DC - \uB9C8\uC9C0\uB9C9 \uC870\uD68C {DateTime.Now:HH:mm:ss}";

            CheckForPersonalizedMatches();
        }
        catch (Exception exception)
        {
            Recommendations.Clear();
            SelectedRecommendation = null;
            IsEmpty = true;
            ResultSummary = "API \uC11C\uBC84 \uB610\uB294 DB \uC5F0\uACB0\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.";
            StatusMessage = "\uB370\uC774\uD130 \uB85C\uB529 \uC2E4\uD328";
            FooterMessage = $"\uC624\uB958: {exception.Message}";
        }

        await LoadDashboardStatsAsync().ConfigureAwait(true);
    }

    private void CheckForPersonalizedMatches()
    {
        if (string.IsNullOrWhiteSpace(InterestKeywordsText) || Recommendations.Count == 0)
        {
            NotificationMessage = string.Empty;
            return;
        }

        var keywords = InterestKeywordsText.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var matchedCount = Recommendations.Count(r =>
            keywords.Any(k =>
                (r.JobTitle != null && r.JobTitle.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                (r.Summary != null && r.Summary.Contains(k, StringComparison.OrdinalIgnoreCase))));

        if (matchedCount > 0)
        {
            NotificationMessage = $"\uAD00\uC2EC \uD0A4\uC6CC\uB4DC \uB9E4\uCE6D: \uCD1D {matchedCount}\uAC74\uC758 \uCD94\uCC9C \uACF5\uACE0\uAC00 \uBC1C\uACAC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
        }
        else
        {
            NotificationMessage = string.Empty;
        }
    }

    private void OpenJobUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            FooterMessage = $"\uBE0C\uB77C\uC6B0\uC800 \uC2E4\uD589 \uC2E4\uD328: {ex.Message}";
        }
    }

    private void ResetFilters()
    {
        _isResettingFilters = true;
        try
        {
            SelectedAgeGroup = AgeGroupOptions[0];
            ExperienceLevel = "\uC804\uCCB4";
            EmploymentType = "\uC804\uCCB4";
            Region = "\uC804\uCCB4";
            Industry = "\uC804\uCCB4";
            MinSalary = null;
        }
        finally
        {
            _isResettingFilters = false;
        }

        TriggerAutoSearch();

        StatusMessage = "\uD544\uD130\uAC00 \uCD08\uAE30\uD654\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
        FooterMessage = "\uC0C8\uB85C\uC6B4 \uAC80\uC0C9 \uC870\uAC74\uC744 \uC785\uB825\uD574 \uBCF4\uC138\uC694.";
    }

    private void TriggerAutoSearch()
    {
        if (_isResettingFilters)
        {
            return;
        }

        _ = SearchAsync();
    }

    private void UpdateTimer()
    {
        _autoRefreshTimer.Stop();
        if (SelectedRefreshOption.Minutes > 0)
        {
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(SelectedRefreshOption.Minutes);
            _autoRefreshTimer.Start();
        }
    }
}
