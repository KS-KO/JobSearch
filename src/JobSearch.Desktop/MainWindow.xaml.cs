using System.Windows;
using JobSearch.Desktop.ViewModels;
using JobSearch.Desktop.Services;

namespace JobSearch.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ApiHostService _apiHostService;

    public MainWindow()
    {
        InitializeComponent();
        _apiHostService = new ApiHostService();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _apiHostService.EnsureApiAvailableAsync(CancellationToken.None).ConfigureAwait(true);
            await _viewModel.InitializeAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"API 자동 실행 또는 초기화에 실패했습니다.{Environment.NewLine}{exception.Message}",
                "JobSearch 시작 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _apiHostService.Dispose();
        base.OnClosed(e);
    }
}
