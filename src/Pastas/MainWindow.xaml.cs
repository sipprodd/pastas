using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using System.Globalization;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Diagnostics;
using Pastas.Infrastructure.Files;
using Pastas.Infrastructure.Hotkeys;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Infrastructure.Tray;
using Pastas.Presentation.Design;
using Pastas.Presentation.Services;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    private static readonly IReadOnlyDictionary<ThemeMode, IReadOnlyDictionary<string, string>> ThemePalettes = new Dictionary<ThemeMode, IReadOnlyDictionary<string, string>>
    {
        [ThemeMode.Chocolate] = new Dictionary<string, string>
        {
            ["ShellBrush"] = "#FF171613", ["SurfaceBrush"] = "#FF23201C", ["SurfaceElevatedBrush"] = "#FF2A2622",
            ["CardBackgroundBrush"] = "#FF2A241F", ["CardHoverBackgroundBrush"] = "#FF342D26", ["CardSelectedBackgroundBrush"] = "#FF47392B",
            ["SubtleBorderBrush"] = "#FF3F3932", ["HoverBorderBrush"] = "#FF6A5A4C", ["SelectedBorderBrush"] = "#FFCDAF7A",
            ["SelectedAccentMarkerBrush"] = "#FFE8C47A", ["CreamTextBrush"] = "#FFF4EBDD", ["MutedTextBrush"] = "#FFC6B9A6",
            ["AccentBrush"] = "#FFD1A764", ["PinnedBorderBrush"] = "#FFE1BC7A", ["InputBackgroundBrush"] = "#FF2A2622",
            ["ButtonBackgroundBrush"] = "#FF2D2823", ["ButtonHoverBrush"] = "#FF373029", ["ButtonPressedBrush"] = "#FF221E1A",
            ["SortPopupBackgroundBrush"] = "#FF2E2924", ["ScrollbarTrackBrush"] = "#FF1C1916", ["ScrollbarThumbBrush"] = "#FF625648",
            ["ScrollbarThumbHoverBrush"] = "#FF766857", ["OverlayBrush"] = "#AA151310"
        },
        [ThemeMode.White] = new Dictionary<string, string>
        {
            ["ShellBrush"] = "#FFF7F7F5", ["SurfaceBrush"] = "#FFFFFFFF", ["SurfaceElevatedBrush"] = "#FFF2F2EF",
            ["CardBackgroundBrush"] = "#FFFFFFFF", ["CardHoverBackgroundBrush"] = "#FFF1F1EE", ["CardSelectedBackgroundBrush"] = "#FFEAEAE6",
            ["SubtleBorderBrush"] = "#FFC8C8C2", ["HoverBorderBrush"] = "#FF4A4A46", ["SelectedBorderBrush"] = "#FF000000",
            ["SelectedAccentMarkerBrush"] = "#FF000000", ["CreamTextBrush"] = "#FF0B0B0B", ["MutedTextBrush"] = "#FF4B4B47",
            ["AccentBrush"] = "#FF000000", ["PinnedBorderBrush"] = "#FF1D1D1B", ["InputBackgroundBrush"] = "#FFFFFFFF",
            ["ButtonBackgroundBrush"] = "#FFF4F4F1", ["ButtonHoverBrush"] = "#FFE9E9E4", ["ButtonPressedBrush"] = "#FFDCDCD5",
            ["SortPopupBackgroundBrush"] = "#FFFFFFFF", ["ScrollbarTrackBrush"] = "#FFE2E2DD", ["ScrollbarThumbBrush"] = "#FF3F3F3B",
            ["ScrollbarThumbHoverBrush"] = "#FF000000", ["OverlayBrush"] = "#99E8E8E2"
        },
        [ThemeMode.Black] = new Dictionary<string, string>
        {
            ["ShellBrush"] = "#FF000000", ["SurfaceBrush"] = "#FF000000", ["SurfaceElevatedBrush"] = "#FF070707",
            ["CardBackgroundBrush"] = "#FF080808", ["CardHoverBackgroundBrush"] = "#FF111111", ["CardSelectedBackgroundBrush"] = "#FF171717",
            ["SubtleBorderBrush"] = "#FF242424", ["HoverBorderBrush"] = "#FF7A7A7A", ["SelectedBorderBrush"] = "#FFFFFFFF",
            ["SelectedAccentMarkerBrush"] = "#FFFFFFFF", ["CreamTextBrush"] = "#FFF7F7F7", ["MutedTextBrush"] = "#FFA7A7A7",
            ["AccentBrush"] = "#FFFFFFFF", ["PinnedBorderBrush"] = "#FFFFFFFF", ["InputBackgroundBrush"] = "#FF050505",
            ["ButtonBackgroundBrush"] = "#FF0B0B0B", ["ButtonHoverBrush"] = "#FF171717", ["ButtonPressedBrush"] = "#FF000000",
            ["SortPopupBackgroundBrush"] = "#FF060606", ["ScrollbarTrackBrush"] = "#FF050505", ["ScrollbarThumbBrush"] = "#FFBDBDBD",
            ["ScrollbarThumbHoverBrush"] = "#FFFFFFFF", ["OverlayBrush"] = "#AA000000"
        }
    };
    private MainViewModel? _viewModel;
    private IDiagnosticsLogger _diagnosticsLogger;
    private ClipboardCaptureNotificationHandler? _clipboardCaptureNotificationHandler;
    private IClipboardChangeWatcher? _clipboardChangeWatcher;
    private IGlobalHotkeyService? _hotkeyService;
    private ITrayService? _trayService;
    private ISettingsRepository? _settingsRepository;
    private ClipboardCleanupService? _clipboardCleanupService;
    private StorageStatsService? _storageStatsService;
    private ClipboardCaptureState? _captureState;
    private AppSettings _settings = new();
    private string _pendingHotkey = "Alt+V";
    private ThemeMode _pendingThemeMode = ThemeMode.Chocolate;

    private bool _isExiting;
    private bool _isCleanedUp;
    private bool _isCompositionInitialized;
    private bool _runtimeServicesStarted;
    private bool _suppressSortToggle;

    public MainWindow()
    {
        var startupLogger = new FileDiagnosticsLogger();
        _diagnosticsLogger = startupLogger;

        startupLogger.Info("MainWindow ctor: before InitializeComponent().");
        InitializeComponent();
        startupLogger.Info("MainWindow ctor: after InitializeComponent().");

        _diagnosticsLogger.Info("MainWindow ctor: before Loaded event subscription.");
        Loaded += OnLoadedAsync;
        _diagnosticsLogger.Info("MainWindow ctor: after Loaded event subscription.");

        _diagnosticsLogger.Info("MainWindow ctor: before Closing event subscription.");
        Closing += OnClosing;
        _diagnosticsLogger.Info("MainWindow ctor: after Closing event subscription.");

        _diagnosticsLogger.Info("MainWindow ctor: before Closed event subscription.");
        Closed += OnClosedAsync;
        _diagnosticsLogger.Info("MainWindow ctor: after Closed event subscription.");

        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void SortMenuButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindName("SortMenuPopup") is not Popup popup || !popup.IsOpen)
        {
            return;
        }

        popup.IsOpen = false;
        _suppressSortToggle = true;
        e.Handled = true;
    }

    private void SortMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_suppressSortToggle)
        {
            _suppressSortToggle = false;
            return;
        }

        var popup = FindName("SortMenuPopup") as System.Windows.Controls.Primitives.Popup;
        if (popup is null)
        {
            return;
        }

        popup.IsOpen = !popup.IsOpen;
    }

    private void SortRecent_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.Recent, "Recent");
    }

    private void SortOldest_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.Oldest, "Oldest");
    }

    private void SortMostCopied_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.MostCopied, "Most copied");
    }

    private void SetSortMode(SortMode sortMode, string label)
    {
        if (_viewModel?.SetSortModeCommand is { } sortCommand && sortCommand.CanExecute(sortMode))
        {
            sortCommand.Execute(sortMode);
        }

        if (FindName("SortMenuButton") is System.Windows.Controls.Button sortMenuButton)
        {
            sortMenuButton.Content = label;
        }

        if (FindName("SortMenuPopup") is System.Windows.Controls.Primitives.Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    private void SortMenuPopup_OnOpened(object sender, EventArgs e)
    {
        if (FindName("SortMenuButton") is System.Windows.Controls.Button sortMenuButton)
        {
            sortMenuButton.Tag = "Open";
        }
    }

    private void SortMenuPopup_OnClosed(object sender, EventArgs e)
    {
        if (FindName("SortMenuButton") is System.Windows.Controls.Button sortMenuButton)
        {
            sortMenuButton.Tag = null;
        }
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var isTextInput = e.OriginalSource is DependencyObject source && IsTextInputSource(source);

        if (!isTextInput && e.Key == Key.Enter && !_viewModel.IsSettingsOpen && !_viewModel.IsClearDataOpen)
        {
            if (_viewModel.SelectedItem is not null && _viewModel.CopyItemCommand.CanExecute(_viewModel.SelectedItem))
            {
                _viewModel.CopyItemCommand.Execute(_viewModel.SelectedItem);
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Space && _viewModel.IsMainContentVisible)
        {
            if (isTextInput)
            {
                return;
            }

            _viewModel.OpenPreview();
            e.Handled = true;
            return;
        }

        if (!isTextInput && _viewModel.IsMainContentVisible && e.Key is Key.Down or Key.Up)
        {
            if (e.Key == Key.Down)
            {
                _viewModel.SelectNextItem();
            }
            else
            {
                _viewModel.SelectPreviousItem();
            }

            e.Handled = true;
            return;
        }

        if (e.Key != Key.Escape)
        {
            return;
        }

        if (_viewModel.IsPreviewOpen)
        {
            _viewModel.ClosePreview();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsClearDataOpen)
        {
            _viewModel.CloseClearData();
            e.Handled = true;
            return;
        }

        if (_viewModel.IsSettingsOpen)
        {
            _viewModel.CloseSettings();
            e.Handled = true;
            return;
        }

        Hide();
        e.Handled = true;
    }

    private static bool IsTextInputSource(DependencyObject source)
    {
        var node = source;
        while (node is not null)
        {
            if (node is System.Windows.Controls.TextBox
                or System.Windows.Controls.PasswordBox
                or System.Windows.Controls.ComboBox)
            {
                return true;
            }

            node = GetParentObject(node);
        }

        return false;
    }


    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    TryDragWindowFromMouseDown(e, allowTextBlockDrag: true);
}

    private void WindowSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    TryDragWindowFromMouseDown(e, allowTextBlockDrag: false);
}

    private void TryDragWindowFromMouseDown(MouseButtonEventArgs e, bool allowTextBlockDrag)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (IsInteractiveDragSource(source, allowTextBlockDrag))
        {
            return;
        }

        DragMove();
    }

    private static bool IsInteractiveDragSource(DependencyObject source, bool allowTextBlockDrag)
{
    var node = source;
    while (node is not null)
    {
        if (node is System.Windows.Controls.Button
            or System.Windows.Controls.TextBox
            or System.Windows.Controls.CheckBox
            or System.Windows.Controls.RadioButton
            or System.Windows.Controls.ComboBox
            or System.Windows.Controls.Primitives.ScrollBar
            or System.Windows.Controls.ScrollViewer
            or System.Windows.Controls.ListBox
            or System.Windows.Controls.ListView
            or System.Windows.Controls.MenuItem
            or Hyperlink
            or System.Windows.Controls.Image)
        {
            return true;
        }

        if (!allowTextBlockDrag && node is System.Windows.Controls.TextBlock)
        {
            return true;
        }

        node = GetParentObject(node);
    }

    return false;
}

private static DependencyObject? GetParentObject(DependencyObject node)
{
    if (node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D)
    {
        return System.Windows.Media.VisualTreeHelper.GetParent(node);
    }

    if (node is FrameworkElement frameworkElement)
    {
        return frameworkElement.Parent;
    }

    if (node is FrameworkContentElement frameworkContentElement)
    {
        return frameworkContentElement.Parent;
    }

    return null;
}

private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (!_isCompositionInitialized)
        {
            _diagnosticsLogger.Info("MainWindow loaded: before CreateCompositionAsync(this).");
            (_viewModel, _diagnosticsLogger, _clipboardCaptureNotificationHandler, _clipboardChangeWatcher, _hotkeyService, _trayService, _settingsRepository, _clipboardCleanupService, _storageStatsService, _captureState) = await CreateCompositionAsync(this);
            _diagnosticsLogger.Info("MainWindow loaded: after CreateCompositionAsync(this).");

            _diagnosticsLogger.Info("MainWindow loaded: before DataContext assignment.");
            DataContext = _viewModel;
            _viewModel.HistoryChanged += OnHistoryChangedAsync;
            _diagnosticsLogger.Info("MainWindow loaded: after DataContext assignment.");

            _diagnosticsLogger.Info("MainWindow loaded: before clipboard watcher event subscription.");
            if (_clipboardCaptureNotificationHandler is not null && _clipboardChangeWatcher is not null)
            {
                _clipboardChangeWatcher.ClipboardChanged += OnClipboardChangedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after clipboard watcher event subscription.");

            _diagnosticsLogger.Info("MainWindow loaded: before hotkey event subscription.");
            if (_hotkeyService is not null)
            {
                _hotkeyService.HotkeyPressed += OnHotkeyPressedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after hotkey event subscription.");

            _diagnosticsLogger.Info("MainWindow loaded: before tray event subscription.");
            if (_trayService is not null)
            {
                _trayService.ShowRequested += OnTrayShowRequestedAsync;
                _trayService.SettingsRequested += OnTraySettingsRequestedAsync;
                _trayService.CapturePauseToggleRequested += OnTrayCapturePauseToggleRequested;
                _trayService.ExitRequested += OnTrayExitRequestedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after tray event subscription.");

            _isCompositionInitialized = true;
        }

        await LoadAndApplySettingsAsync();

        StartRuntimeServicesOnce();
    }

    private void StartRuntimeServicesOnce()
    {
        if (_runtimeServicesStarted)
        {
            return;
        }

        _clipboardChangeWatcher?.Start();
        _trayService?.SetCapturePaused(_captureState?.IsCapturePaused == true);
        _trayService?.Start();
        _runtimeServicesStarted = true;
        _diagnosticsLogger.Info("Runtime services started.");
    }

    private async Task LoadAndApplySettingsAsync()
    {
        try
        {
            if (_settingsRepository is not null)
            {
                _settings = await _settingsRepository.GetAsync();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }

        _pendingHotkey = _settings.Hotkey;
        HotkeyTextBox.Text = _settings.Hotkey;
        MaxItemsTextBox.Text = _settings.MaxItems.ToString(CultureInfo.InvariantCulture);
        _pendingThemeMode = _settings.ThemeMode;
        UpdateThemeChipSelection(_pendingThemeMode);
        ApplyTheme(_settings.ThemeMode);

        if (_hotkeyService is not null)
        {
            await _hotkeyService.RegisterAsync(_settings.Hotkey);
        }

        if (_viewModel is not null)
        {
            await EnforceStorageLimitsAsync();
            await _viewModel.RefreshAsync();
            await UpdateStorageSummaryAsync();
        }
    }

    private void ApplyTheme(ThemeMode mode)
    {
        var palette = ThemePalettes.TryGetValue(mode, out var selectedPalette)
            ? selectedPalette
            : ThemePalettes[ThemeMode.Chocolate];

        foreach (var (key, colorHex) in palette)
        {
            Resources[key] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex));
        }

        UpdateThemeChipSelection(mode);
    }

    private async Task UpdateStorageSummaryAsync()
    {
        if (_storageStatsService is null)
        {
            StorageSummaryText.Text = "Total items: 0\nPinned: 0\nImages: 0\nApprox. usage: 0.00 MB";
            return;
        }

        var stats = await _storageStatsService.GetStatsAsync();
        var usageMegabytes = Math.Max(0, stats.ApproxUsageBytes) / (1024d * 1024d);
        StorageSummaryText.Text = $"Total items: {Math.Max(0, stats.TotalItems)}\nPinned: {Math.Max(0, stats.PinnedItems)}\nImages: {Math.Max(0, stats.ImageItems)}\nApprox. usage: {usageMegabytes:F2} MB";
    }

    private async Task EnforceStorageLimitsAsync()
    {
        if (_clipboardCleanupService is null)
        {
            return;
        }

        await _clipboardCleanupService.CleanupAsync(_settings.MaxItems);
    }

    private async void OnHistoryChangedAsync(object? sender, EventArgs e)
    {
        await UpdateStorageSummaryAsync();
    }

    private async void OnClipboardChangedAsync(object? sender, EventArgs e)
    {
        if (_clipboardCaptureNotificationHandler is null)
        {
            return;
        }

        await _clipboardCaptureNotificationHandler.HandleClipboardChangedAsync();

        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
            await UpdateStorageSummaryAsync();
        }
    }

    private async void OnHotkeyPressedAsync(object? sender, EventArgs e)
    {
        await ToggleWindowVisibilityAsync();
    }

    private async void OnTrayShowRequestedAsync(object? sender, EventArgs e)
    {
        await ToggleWindowVisibilityAsync();
    }

    private async void OnTraySettingsRequestedAsync(object? sender, EventArgs e)
    {
        await ShowWindowAsync(openSettings: true);
    }

    private void OnTrayCapturePauseToggleRequested(object? sender, EventArgs e)
    {
        if (_captureState is null)
        {
            return;
        }

        var isPaused = _captureState.ToggleCapturePause();
        _trayService?.SetCapturePaused(isPaused);
        _viewModel?.SetStatusMessage(isPaused ? "Clipboard capture paused." : "Clipboard capture resumed.");
        _diagnosticsLogger.Info(isPaused ? "Clipboard capture paused." : "Clipboard capture resumed.");
    }

    private async void OnTrayExitRequestedAsync(object? sender, EventArgs e)
    {
        await ExitApplicationAsync();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isExiting || !IsVisible)
        {
            return;
        }

        Hide();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        await CleanupAsync();
    }

    private async Task ToggleWindowVisibilityAsync()
    {
        var shouldRefresh = false;

        await Dispatcher.InvokeAsync(() =>
        {
            if (IsVisible && IsActive)
            {
                Hide();
                return;
            }

            if (!IsVisible)
            {
                PositionNearCursor();
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            shouldRefresh = true;
        });

        if (shouldRefresh && _viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private async Task ShowWindowAsync(bool openSettings = false)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (!IsVisible)
            {
                PositionNearCursor();
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();

            if (openSettings)
            {
                _viewModel?.OpenSettings();
            }
        });

        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }
    }


    private void PositionNearCursor()
    {
        var cursor = System.Windows.Forms.Control.MousePosition;
        var screen = System.Windows.Forms.Screen.FromPoint(cursor);
        var workArea = screen.WorkingArea;

        const double offset = 16;
        var proposedLeft = cursor.X + offset;
        var proposedTop = cursor.Y + offset;

        var maxLeft = workArea.Right - Width;
        var maxTop = workArea.Bottom - Height;

        Left = Math.Max(workArea.Left, Math.Min(proposedLeft, maxLeft));
        Top = Math.Max(workArea.Top, Math.Min(proposedTop, maxTop));
    }

    private async Task ExitApplicationAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        await CleanupAsync();
        await Dispatcher.InvokeAsync(() => System.Windows.Application.Current.Shutdown());
    }

    private async Task CleanupAsync()
    {
        if (_isCleanedUp)
        {
            return;
        }

        _isCleanedUp = true;

        if (_viewModel is not null)
        {
            _viewModel.HistoryChanged -= OnHistoryChangedAsync;
        }

        if (_clipboardChangeWatcher is not null)
        {
            _clipboardChangeWatcher.ClipboardChanged -= OnClipboardChangedAsync;
            _clipboardChangeWatcher.Stop();
        }

        if (_hotkeyService is not null)
        {
            _hotkeyService.HotkeyPressed -= OnHotkeyPressedAsync;
            await _hotkeyService.UnregisterAsync();
        }

        if (_trayService is not null)
        {
            _trayService.ShowRequested -= OnTrayShowRequestedAsync;
            _trayService.SettingsRequested -= OnTraySettingsRequestedAsync;
            _trayService.CapturePauseToggleRequested -= OnTrayCapturePauseToggleRequested;
            _trayService.ExitRequested -= OnTrayExitRequestedAsync;
            _trayService.Stop();
            _trayService.Dispose();
        }
    }

    private static async Task<(MainViewModel ViewModel, IDiagnosticsLogger DiagnosticsLogger, ClipboardCaptureNotificationHandler? NotificationHandler, IClipboardChangeWatcher? Watcher, IGlobalHotkeyService? HotkeyService, ITrayService? TrayService, ISettingsRepository? SettingsRepository, ClipboardCleanupService? CleanupService, StorageStatsService? StorageStatsService, ClipboardCaptureState CaptureState)> CreateCompositionAsync(Window window)
    {
        var diagnosticsLogger = new FileDiagnosticsLogger();

        try
        {
            diagnosticsLogger.Info("CreateComposition: before SqliteDatabasePathProvider.");
            var databasePathProvider = new SqliteDatabasePathProvider();
            diagnosticsLogger.Info("CreateComposition: after SqliteDatabasePathProvider.");

            diagnosticsLogger.Info("CreateComposition: before SqliteConnectionFactory.");
            var connectionFactory = new SqliteConnectionFactory(databasePathProvider);
            diagnosticsLogger.Info("CreateComposition: after SqliteConnectionFactory.");

            diagnosticsLogger.Info("CreateComposition: before SqliteMigrationRunner creation.");
            var migrationRunner = new SqliteMigrationRunner(connectionFactory);
            diagnosticsLogger.Info("CreateComposition: after SqliteMigrationRunner creation.");

            diagnosticsLogger.Info("CreateCompositionAsync: before await migrationRunner.RunAsync().");
            await migrationRunner.RunAsync();
            diagnosticsLogger.Info("CreateCompositionAsync: after await migrationRunner.RunAsync().");

            diagnosticsLogger.Info("CreateComposition: before SqliteClipboardItemRepository.");
            var repository = new SqliteClipboardItemRepository(connectionFactory);
            diagnosticsLogger.Info("CreateComposition: after SqliteClipboardItemRepository.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardRetryPolicy.");
            var retryPolicy = new ClipboardRetryPolicy();
            diagnosticsLogger.Info("CreateComposition: after ClipboardRetryPolicy.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureState.");
            var captureState = new ClipboardCaptureState();
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureState.");

            diagnosticsLogger.Info("CreateComposition: before WindowsClipboardGateway.");
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            diagnosticsLogger.Info("CreateComposition: after WindowsClipboardGateway.");

            diagnosticsLogger.Info("CreateComposition: before LocalFileStorage.");
            var fileStorage = new LocalFileStorage();
            diagnosticsLogger.Info("CreateComposition: after LocalFileStorage.");

            diagnosticsLogger.Info("CreateComposition: before ImageThumbnailBuilder.");
            var thumbnailBuilder = new ImageThumbnailBuilder();
            diagnosticsLogger.Info("CreateComposition: after ImageThumbnailBuilder.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCleanupOptions.");
            var cleanupOptions = new ClipboardCleanupOptions();
            diagnosticsLogger.Info("CreateComposition: after ClipboardCleanupOptions.");

            diagnosticsLogger.Info("CreateComposition: before CaptureClipboardTextUseCase.");
            var captureTextUseCase = new CaptureClipboardTextUseCase(clipboardGateway, repository, captureState, cleanupOptions);
            diagnosticsLogger.Info("CreateComposition: after CaptureClipboardTextUseCase.");

            diagnosticsLogger.Info("CreateComposition: before CaptureClipboardImageUseCase.");
            var captureImageUseCase = new CaptureClipboardImageUseCase(clipboardGateway, repository, fileStorage, thumbnailBuilder, captureState, cleanupOptions);
            diagnosticsLogger.Info("CreateComposition: after CaptureClipboardImageUseCase.");

            diagnosticsLogger.Info("CreateComposition: before CopyTextItemToClipboardUseCase.");
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);
            diagnosticsLogger.Info("CreateComposition: after CopyTextItemToClipboardUseCase.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCleanupService.");
            var cleanupService = new ClipboardCleanupService(repository, fileStorage, cleanupOptions, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCleanupService.");

            var settingsRepository = new SqliteSettingsRepository(connectionFactory);
            var storageStatsService = new StorageStatsService(repository);

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureCoordinator.");
            var coordinator = new ClipboardCaptureCoordinator(captureTextUseCase, captureImageUseCase, cleanupService, diagnosticsLogger, settingsRepository: settingsRepository);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureCoordinator.");

            diagnosticsLogger.Info("CreateComposition: before WindowsClipboardChangeWatcher.");
            var watcher = new WindowsClipboardChangeWatcher(window, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsClipboardChangeWatcher.");

            diagnosticsLogger.Info("CreateComposition: before WindowsHotkeyService.");
            var hotkeyService = new WindowsHotkeyService(window, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsHotkeyService.");

            diagnosticsLogger.Info("CreateComposition: before WindowsTrayService.");
            var trayService = new WindowsTrayService(diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsTrayService.");

            diagnosticsLogger.Info("CreateComposition: before MainViewModel.");
            var viewModel = new MainViewModel(repository, copyUseCase, clipboardGateway, captureState);
            diagnosticsLogger.Info("CreateComposition: after MainViewModel.");

            diagnosticsLogger.Info("CreateComposition: before MainViewModelNotificationService.");
            var notificationService = new MainViewModelNotificationService(viewModel, window.Dispatcher);
            diagnosticsLogger.Info("CreateComposition: after MainViewModelNotificationService.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureNotificationHandler.");
            var notificationHandler = new ClipboardCaptureNotificationHandler(coordinator, notificationService, repository, captureState);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureNotificationHandler.");

            diagnosticsLogger.Info("Application composition succeeded.");
            return (viewModel, diagnosticsLogger, notificationHandler, watcher, hotkeyService, trayService, settingsRepository, cleanupService, storageStatsService, captureState);
        }
        catch (Exception ex)
        {
            diagnosticsLogger.Error("Application composition failed.", ex);

            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            var hotkeyService = new WindowsHotkeyService(window, diagnosticsLogger);
            var trayService = new WindowsTrayService(diagnosticsLogger);

            return (new MainViewModel(repository, copyUseCase, clipboardGateway, captureState), diagnosticsLogger, null, null, hotkeyService, trayService, null, null, new StorageStatsService(repository), captureState);
        }
    }

    private void CaptureHotkey_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        if (e.Key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is < Key.A or > Key.Z) return;
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        _pendingHotkey = string.Join("+", parts);
        HotkeyTextBox.Text = _pendingHotkey;
    }

    private void ThemeChip_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag }) return;
        _pendingThemeMode = tag switch
        {
            "White" => ThemeMode.White,
            "Black" => ThemeMode.Black,
            _ => ThemeMode.Chocolate
        };
        ApplyTheme(_pendingThemeMode);
        HotkeyStatusText.Text = "Theme preview. Click Save to keep changes.";
    }

    private void UpdateThemeChipSelection(ThemeMode mode)
    {
        StyleThemeChip(ThemeChocolateButton, mode == ThemeMode.Chocolate);
        StyleThemeChip(ThemeWhiteButton, mode == ThemeMode.White);
        StyleThemeChip(ThemeBlackButton, mode == ThemeMode.Black);
    }

    private void StyleThemeChip(System.Windows.Controls.Button button, bool isSelected)
    {
        if (button is null) return;
        button.Background = (System.Windows.Media.Brush)FindResource(isSelected ? "CardSelectedBackgroundBrush" : "ButtonBackgroundBrush");
        button.BorderBrush = (System.Windows.Media.Brush)FindResource(isSelected ? "SelectedBorderBrush" : "SubtleBorderBrush");
        button.Foreground = (System.Windows.Media.Brush)FindResource("CreamTextBrush");
    }

    private async void SaveSettings_OnClick(object sender, RoutedEventArgs e)
    {
        var previous = _settings.Hotkey;
        if (!AppSettingsValidator.TryParseMaxItems(MaxItemsTextBox.Text, out var maxItems, out var maxItemsError))
        {
            HotkeyStatusText.Text = maxItemsError;
            return;
        }
        var newTheme = _pendingThemeMode;

        if (!HotkeyGestureParser.TryParse(_pendingHotkey, out _, out _))
        {
            HotkeyStatusText.Text = "Invalid hotkey. Previous hotkey kept.";
            _pendingHotkey = previous;
            HotkeyTextBox.Text = previous;
            return;
        }

        if (_hotkeyService is WindowsHotkeyService windowsHotkeyService)
        {
            var applied = await windowsHotkeyService.TryRegisterAsync(_pendingHotkey);
            if (!applied)
            {
                await windowsHotkeyService.TryRegisterAsync(previous);
                _pendingHotkey = previous;
                HotkeyTextBox.Text = previous;
                HotkeyStatusText.Text = "Could not register hotkey.";
                return;
            }
        }
        else if (_hotkeyService is not null)
        {
            await _hotkeyService.RegisterAsync(_pendingHotkey);
        }

        ApplyTheme(newTheme);

        var savedSettings = new AppSettings
        {
            Hotkey = _pendingHotkey,
            ThemeMode = newTheme,
            MaxItems = maxItems,
            MaxItemSizeBytes = _settings.MaxItemSizeBytes,
            MaxCacheSizeBytes = _settings.MaxCacheSizeBytes,
            NotificationsEnabled = _settings.NotificationsEnabled,
            CopyStreakEnabled = _settings.CopyStreakEnabled,
            ProtectedItemPolicy = _settings.ProtectedItemPolicy,
            HideProtectedOnBlur = _settings.HideProtectedOnBlur,
            RevealProtectedSeconds = _settings.RevealProtectedSeconds,
            ClearProtectedClipboardAfterDelay = _settings.ClearProtectedClipboardAfterDelay,
            ClearProtectedClipboardDelaySeconds = _settings.ClearProtectedClipboardDelaySeconds
        };
        if (_settingsRepository is not null)
        {
            await _settingsRepository.SaveAsync(savedSettings);
        }

        _settings = savedSettings;
        await EnforceStorageLimitsAsync();
        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }

        await UpdateStorageSummaryAsync();
        HotkeyStatusText.Text = "Settings saved.";
    }

    private void BackSettings_OnClick(object sender, RoutedEventArgs e)
    {
        _pendingThemeMode = _settings.ThemeMode;
        _pendingHotkey = _settings.Hotkey;
        ApplyTheme(_settings.ThemeMode);
        HotkeyTextBox.Text = _settings.Hotkey;
        MaxItemsTextBox.Text = _settings.MaxItems.ToString(CultureInfo.InvariantCulture);
        HotkeyStatusText.Text = string.Empty;
        _viewModel?.CloseSettingsCommand.Execute(null);
    }
}
