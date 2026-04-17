using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using JobSearch.Desktop.Commands;
using JobSearch.Desktop.Models;
using JobSearch.Desktop.Services;

namespace JobSearch.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly RecommendationApiService _apiService;
    private readonly AsyncRelayCommand _searchCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand<string> _openJobUrlCommand;
    
    private AgeGroupOption? _selectedAgeGroup;
    private RecommendationItem? _selectedRecommendation;
    private string _experienceLevel = string.Empty;
    private string _employmentType = string.Empty;
    private string _region = string.Empty;
    private string _industry = string.Empty;
    private string _statusMessage = "시스템 준비 완료";
    private string _footerMessage = "SQLite DB와 API 서버를 통해 실시간 데이터를 조회합니다.";
    private string _resultSummary = "아직 조회된 결과가 없습니다.";
    private bool _isEmpty = true;

    public MainViewModel()
    {
        // 실제 운영 시에는 설정 파일에서 읽어와야 함
        _apiService = new RecommendationApiService("https://localhost:7048/"); 
        
        Recommendations = [];
        AgeGroupOptions =
        [
            new AgeGroupOption(null, "전체"),
            new AgeGroupOption("Twenties", "20대"),
            new AgeGroupOption("Thirties", "30대"),
            new AgeGroupOption("Forties", "40대"),
            new AgeGroupOption("FiftiesAndAbove", "50대 이상")
        ];

        _selectedAgeGroup = AgeGroupOptions[0];

        _searchCommand = new AsyncRelayCommand(SearchAsync);
        _resetFiltersCommand = new RelayCommand(ResetFilters);
        _openJobUrlCommand = new RelayCommand<string>(OpenJobUrl);
    }

    public ObservableCollection<RecommendationItem> Recommendations { get; }
    public IReadOnlyList<AgeGroupOption> AgeGroupOptions { get; }
    public ICommand SearchCommand => _searchCommand;
    public ICommand ResetFiltersCommand => _resetFiltersCommand;
    public ICommand OpenJobUrlCommand => _openJobUrlCommand;

    public AgeGroupOption? SelectedAgeGroup
    {
        get => _selectedAgeGroup;
        set => SetProperty(ref _selectedAgeGroup, value);
    }

    public RecommendationItem? SelectedRecommendation
    {
        get => _selectedRecommendation;
        set => SetProperty(ref _selectedRecommendation, value);
    }

    public string ExperienceLevel
    {
        get => _experienceLevel;
        set => SetProperty(ref _experienceLevel, value);
    }

    public string EmploymentType
    {
        get => _employmentType;
        set => SetProperty(ref _employmentType, value);
    }

    public string Region
    {
        get => _region;
        set => SetProperty(ref _region, value);
    }

    public string Industry
    {
        get => _industry;
        set => SetProperty(ref _industry, value);
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

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public async Task InitializeAsync()
    {
        await SearchAsync().ConfigureAwait(true);
    }

    private async Task SearchAsync()
    {
        try
        {
            StatusMessage = "데이터베이스에서 추천 공고를 조회하고 있습니다...";

            var items = await _apiService.GetRecommendationsAsync(
                SelectedAgeGroup?.Value,
                ExperienceLevel,
                EmploymentType,
                Region,
                Industry,
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
        SelectedAgeGroup = AgeGroupOptions[0];
        ExperienceLevel = string.Empty;
        EmploymentType = string.Empty;
        Region = string.Empty;
        Industry = string.Empty;
        StatusMessage = "필터가 초기화되었습니다.";
        FooterMessage = "새로운 검색 조건을 입력하세요.";
    }
}
