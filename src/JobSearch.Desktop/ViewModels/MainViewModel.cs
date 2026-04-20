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
    private readonly UserSettingsService _settingsService;
    private readonly AsyncRelayCommand _searchCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand<string> _openJobUrlCommand;
    private readonly AsyncRelayCommand _saveKeywordsCommand;
    
    private AgeGroupOption? _selectedAgeGroup;
    private RecommendationItem? _selectedRecommendation;
    private string _experienceLevel = "전체";
    private string _employmentType = "전체";
    private string _region = "전체";
    private string _industry = "전체";
    private int? _minSalary;
    private string _statusMessage = "시스템 준비 완료";
    private string _footerMessage = "SQLite DB와 API 서버를 통해 실시간 데이터를 조회합니다.";
    private string _resultSummary = "아직 조회된 결과가 없습니다.";
    private string _dashboardStatsText = "DB 통계를 불러오는 중...";
    private string _interestKeywordsText = string.Empty;
    private string _notificationMessage = string.Empty;
    private bool _isEmpty = true;
    private bool _isResettingFilters;
    private readonly DispatcherTimer _autoRefreshTimer;
    private RefreshOption _selectedRefreshOption = default!;

    public MainViewModel()
    {
        // 실제 운영 시에는 설정 파일에서 읽어와야 함
        _apiService = new RecommendationApiService("http://localhost:5058/"); 
        _settingsService = new UserSettingsService();
        
        Recommendations = [];
        AgeGroupOptions =
        [
            new AgeGroupOption(null, "전체"),
            new AgeGroupOption("Twenties", "20대"),
            new AgeGroupOption("Thirties", "30대"),
            new AgeGroupOption("Forties", "40대"),
            new AgeGroupOption("FiftiesAndAbove", "50대 이상")
        ];

        ExperienceOptions = [ "전체", "신입", "경력", "신입·경력", "인턴" ];
        EmploymentOptions = [ "전체", "정규직", "계약직", "인턴직", "프리랜서", "아르바이트" ];
        RegionOptions = [ "전체", "서울", "경기", "인천", "대전", "대구", "부산", "울산", "광주", "강원", "제주", "충남", "충북", "전남", "전북", "경남", "경북", "세종" ];
        IndustryOptions = [ "전체", "IT", "백엔드", "프론트엔드", "데이터", "게임", "소프트웨어", "마케팅", "디자인", "기획", "영업", "경영/사무" ];

        RefreshOptions = [
            new RefreshOption(0, "사용 안 함"),
            new RefreshOption(1, "1분 마다"),
            new RefreshOption(5, "5분 마다"),
            new RefreshOption(10, "10분 마다"),
            new RefreshOption(15, "15분 마다"),
            new RefreshOption(30, "30분 마다"),
            new RefreshOption(60, "1시간 마다"),
            new RefreshOption(180, "3시간 마다"),
            new RefreshOption(360, "6시간 마다"),
            new RefreshOption(720, "12시간 마다"),
            new RefreshOption(1440, "24시간 마다")
        ];
        _selectedRefreshOption = RefreshOptions[0];

        _selectedAgeGroup = AgeGroupOptions[0];

        _autoRefreshTimer = new DispatcherTimer();
        _autoRefreshTimer.Tick += (s, e) => _ = SearchAsync();

        _searchCommand = new AsyncRelayCommand(SearchAsync);
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
    public ICommand SearchCommand => _searchCommand;
    public ICommand ResetFiltersCommand => _resetFiltersCommand;
    public ICommand OpenJobUrlCommand => _openJobUrlCommand;
    public ICommand SaveKeywordsCommand => _saveKeywordsCommand;

    public AgeGroupOption? SelectedAgeGroup
    {
        get => _selectedAgeGroup;
        set
        {
            if (SetProperty(ref _selectedAgeGroup, value)) TriggerAutoSearch();
        }
    }

    public RecommendationItem? SelectedRecommendation
    {
        get => _selectedRecommendation;
        set => SetProperty(ref _selectedRecommendation, value);
    }

    public string ExperienceLevel
    {
        get => _experienceLevel;
        set
        {
            if (SetProperty(ref _experienceLevel, value)) TriggerAutoSearch();
        }
    }

    public string EmploymentType
    {
        get => _employmentType;
        set
        {
            if (SetProperty(ref _employmentType, value)) TriggerAutoSearch();
        }
    }

    public string Region
    {
        get => _region;
        set
        {
            if (SetProperty(ref _region, value)) TriggerAutoSearch();
        }
    }

    public string Industry
    {
        get => _industry;
        set
        {
            if (SetProperty(ref _industry, value)) TriggerAutoSearch();
        }
    }

    public int? MinSalary
    {
        get => _minSalary;
        set
        {
            if (SetProperty(ref _minSalary, value)) TriggerAutoSearch();
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

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        if (settings.InterestKeywords.Length > 0)
        {
            InterestKeywordsText = string.Join(", ", settings.InterestKeywords);
        }

        var option = RefreshOptions.FirstOrDefault(o => o.Minutes == settings.AutoRefreshMinutes);
        if (option != null)
        {
            SelectedRefreshOption = option;
        }

        await LoadDashboardStatsAsync().ConfigureAwait(true);
        await SearchAsync().ConfigureAwait(true);
    }

    private async Task SaveKeywordsAsync()
    {
        var keywords = InterestKeywordsText.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var settings = new UserSettings 
        { 
            InterestKeywords = keywords,
            AutoRefreshMinutes = SelectedRefreshOption.Minutes
        };
        await _settingsService.SaveAsync(settings).ConfigureAwait(true);
        
        StatusMessage = "설정 및 관심 키워드가 저장되었습니다.";
        FooterMessage = "저장된 정보를 바탕으로 검색 및 주기적 알림을 제공합니다.";
        
        await SearchAsync().ConfigureAwait(true);
    }

    private async Task LoadDashboardStatsAsync()
    {
        var stats = await _apiService.GetDashboardStatsAsync(CancellationToken.None).ConfigureAwait(true);
        if (stats != null)
        {
            DashboardStatsText = $"총 데이터: {stats.TotalCount}건 (사람인 {stats.SaraminCount}건, 잡코리아 {stats.JobKoreaCount}건) | 갱신: {stats.LastUpdatedTime}";
        }
        else
        {
            DashboardStatsText = "통계 정보를 불러올 수 없습니다.";
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            StatusMessage = "데이터베이스에서 추천 공고를 조회하고 있습니다...";

            var reqExp = ExperienceLevel == "전체" ? string.Empty : ExperienceLevel;
            var reqEmp = EmploymentType == "전체" ? string.Empty : EmploymentType;
            var reqReg = Region == "전체" ? string.Empty : Region;
            var reqInd = Industry == "전체" ? string.Empty : Industry;

            var items = await _apiService.GetRecommendationsAsync(
                SelectedAgeGroup?.Value,
                reqExp,
                reqEmp,
                reqReg,
                reqInd,
                MinSalary,
                CancellationToken.None).ConfigureAwait(true);

            Recommendations.Clear();
            foreach (var item in items)
            {
                Recommendations.Add(item);
            }

            SelectedRecommendation = Recommendations.FirstOrDefault();
            IsEmpty = Recommendations.Count == 0;
            ResultSummary = Recommendations.Count == 0
                ? "조건에 맞는 추천 결과가 없습니다. 수집기를 돌려 데이터를 확보하세요."
                : $"DB 조회 결과 {Recommendations.Count}건의 공고를 찾았습니다.";
            StatusMessage = Recommendations.Count == 0 ? "조회 완료: 결과 없음" : "조회 완료";
            FooterMessage = $"SQLite DB 연동 모드 - 마지막 조회 {DateTime.Now:HH:mm:ss}";

            CheckForPersonalizedMatches();
        }
        catch (Exception exception)
        {
            Recommendations.Clear();
            SelectedRecommendation = null;
            IsEmpty = true;
            ResultSummary = "API 서버 또는 DB 연결에 실패했습니다.";
            StatusMessage = "데이터 로딩 실패";
            FooterMessage = $"오류: {exception.Message}";
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
            NotificationMessage = $"✨ 관심 키워드 매칭: 총 {matchedCount}건의 추천 공고가 발견되었습니다!";
        }
        else
        {
            NotificationMessage = string.Empty;
        }
    }

    private void OpenJobUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
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
            FooterMessage = $"브라우저 실행 실패: {ex.Message}";
        }
    }

    private void ResetFilters()
    {
        _isResettingFilters = true;
        try
        {
            SelectedAgeGroup = AgeGroupOptions[0];
            ExperienceLevel = "전체";
            EmploymentType = "전체";
            Region = "전체";
            Industry = "전체";
            MinSalary = null;
        }
        finally
        {
            _isResettingFilters = false;
        }
        
        TriggerAutoSearch();
        
        StatusMessage = "필터가 초기화되었습니다.";
        FooterMessage = "새로운 검색 조건을 입력하세요.";
    }

    private void TriggerAutoSearch()
    {
        if (_isResettingFilters) return;
        _ = SearchAsync();
    }

    private void UpdateTimer()
    {
        if (_autoRefreshTimer == null) return;
        
        _autoRefreshTimer.Stop();
        if (SelectedRefreshOption != null && SelectedRefreshOption.Minutes > 0)
        {
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(SelectedRefreshOption.Minutes);
            _autoRefreshTimer.Start();
        }
    }
}
