using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JobSearch.Desktop.Services;
using JobSearch.Desktop.ViewModels;

namespace JobSearch.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ApiHostService _apiHostService;
    private readonly CancellationTokenSource _startupCancellationTokenSource = new();

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
        NormalizeLocalizedTexts();
        _viewModel.UpdateStartupState(
            "\u0053\u0065\u0061\u0072\u0063\u0068\u0020\u0041\u0050\u0049\uB97C\u0020\uBC31\uADF8\uB77C\uC6B4\uB4DC\uC5D0\uC11C\u0020\uC2DC\uC791\uD558\uACE0\u0020\uC788\uC2B5\uB2C8\uB2E4\u002E\u002E\u002E",
            "\u0041\u0050\u0049\u0020\uC900\uBE44\uAC00\u0020\uB05D\uB098\uBA74\u0020\uCD94\uCC9C\u0020\uB370\uC774\uD130\uC640\u0020\uD1B5\uACC4\uB97C\u0020\uC790\uB3D9\uC73C\uB85C\u0020\uBD88\uB7EC\uC635\uB2C8\uB2E4\u002E");

        await Task.Yield();
        _ = InitializeSearchApiInBackgroundAsync(_startupCancellationTokenSource.Token);
    }

    private async Task InitializeSearchApiInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _apiHostService.EnsureApiAvailableAsync(cancellationToken).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(
                    () => _viewModel.UpdateStartupState(
                        "\u0053\u0065\u0061\u0072\u0063\u0068\u0020\u0041\u0050\u0049\u0020\uC5F0\uACB0\u0020\uC644\uB8CC",
                        "\uCD94\uCC9C\u0020\uB370\uC774\uD130\uC640\u0020\uB300\uC2DC\uBCF4\uB4DC\u0020\uD1B5\uACC4\uB97C\u0020\uBD88\uB7EC\uC624\uB294\u0020\uC911\uC785\uB2C8\uB2E4\u002E"),
                    DispatcherPriority.Background,
                    cancellationToken)
                .Task
                .ConfigureAwait(false);

            await Dispatcher.InvokeAsync(
                    () => _viewModel.InitializeAsync(),
                    DispatcherPriority.Background,
                    cancellationToken)
                .Task
                .Unwrap()
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Dispatcher.InvokeAsync(
                    () => _viewModel.ApplyStartupFailure($"\uC624\uB958: {exception.Message}"),
                    DispatcherPriority.Background,
                    cancellationToken)
                .Task
                .ConfigureAwait(false);
        }
    }

    private void NormalizeLocalizedTexts()
    {
        if (FindRealtimeSearchButton(this) is Button realtimeSearchButton)
        {
            realtimeSearchButton.Content = "\uC2E4\uC2DC\uAC04\u0020\uAC80\uC0C9\u0020\uC2DC\uC791";
        }
    }

    private Button? FindRealtimeSearchButton(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button && ReferenceEquals(button.Command, _viewModel.StartRealtimeSearchCommand))
            {
                return button;
            }

            if (child is DependencyObject dependencyObject)
            {
                var nestedButton = FindRealtimeSearchButton(dependencyObject);
                if (nestedButton is not null)
                {
                    return nestedButton;
                }
            }
        }

        return null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _startupCancellationTokenSource.Cancel();
        _startupCancellationTokenSource.Dispose();
        _apiHostService.Dispose();
        base.OnClosed(e);
    }
}
