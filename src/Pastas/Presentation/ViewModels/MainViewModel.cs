using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Presentation.Commands;

namespace Pastas.Presentation.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly ICopyTextItemToClipboardUseCase _copyTextItemToClipboardUseCase;
    private readonly IClipboardGateway _clipboardGateway;
    private readonly ClipboardCaptureState _captureState;
    private string _searchQuery = string.Empty;
    private ClipboardFilter _selectedFilter = ClipboardFilter.All;
    private SortMode _selectedSortMode = SortMode.Recent;
    private bool _isLoading;
    private bool _isPreviewOpen;
    private bool _isSettingsOpen;
    private ClipboardItemViewModel? _selectedItem;
    private bool _isClearDataOpen;
    private bool _clearTextSelected;
    private bool _clearImagesSelected;
    private bool _clearPinnedSelected;
    private string _emptyStateText = "No clipboard items yet.";
    private string _statusMessage = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;

    public MainViewModel(
        IClipboardItemRepository clipboardItemRepository,
        ICopyTextItemToClipboardUseCase copyTextItemToClipboardUseCase,
        IClipboardGateway clipboardGateway,
        ClipboardCaptureState captureState)
    {
        _clipboardItemRepository = clipboardItemRepository;
        _copyTextItemToClipboardUseCase = copyTextItemToClipboardUseCase;
        _clipboardGateway = clipboardGateway;
        _captureState = captureState;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SearchCommand = new AsyncRelayCommand(RefreshAsync);
        SetFilterCommand = new AsyncRelayCommand(SetFilterAsync);
        SetSortModeCommand = new AsyncRelayCommand(SetSortModeAsync);
        DeleteItemCommand = new AsyncRelayCommand(DeleteItemAsync);
        TogglePinCommand = new AsyncRelayCommand(TogglePinAsync);
        CopyItemCommand = new AsyncRelayCommand(CopyItemAsync);
        SelectItemCommand = new RelayCommand(SelectItem);
        OpenPreviewCommand = new RelayCommand(OpenPreview);
        ClosePreviewCommand = new RelayCommand(_ => ClosePreview());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        CloseSettingsCommand = new RelayCommand(_ => CloseSettings());
        OpenClearDataCommand = new RelayCommand(_ => OpenClearData());
        CloseClearDataCommand = new RelayCommand(_ => CloseClearData());
        ConfirmClearDataCommand = new AsyncRelayCommand(ConfirmClearDataAsync);
    }

    public ObservableCollection<ClipboardItemViewModel> Items { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value))
            {
                return;
            }

            _ = DebouncedRefreshAsync();
        }
    }

    public ClipboardFilter SelectedFilter
    {
        get => _selectedFilter;
        set => SetProperty(ref _selectedFilter, value);
    }

    public SortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (!SetProperty(ref _selectedSortMode, value))
            {
                return;
            }

            _ = RefreshAsync();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ClipboardItemViewModel? SelectedItem
    {
        get => _selectedItem;
        private set => SetProperty(ref _selectedItem, value);
    }

    public bool IsPreviewOpen
    {
        get => _isPreviewOpen;
        private set
        {
            if (SetProperty(ref _isPreviewOpen, value))
            {
                OnPropertyChanged(nameof(IsMainContentVisible));
            }
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set
        {
            if (SetProperty(ref _isSettingsOpen, value))
            {
                OnPropertyChanged(nameof(IsMainContentVisible));
            }
        }
    }

    public bool IsMainContentVisible => !IsPreviewOpen && !IsSettingsOpen;
    public bool IsClearDataOpen
    {
        get => _isClearDataOpen;
        private set => SetProperty(ref _isClearDataOpen, value);
    }
    public bool ClearTextSelected
    {
        get => _clearTextSelected;
        set
        {
            if (SetProperty(ref _clearTextSelected, value))
            {
                OnPropertyChanged(nameof(CanConfirmClearData));
            }
        }
    }
    public bool ClearImagesSelected
    {
        get => _clearImagesSelected;
        set
        {
            if (SetProperty(ref _clearImagesSelected, value))
            {
                OnPropertyChanged(nameof(CanConfirmClearData));
            }
        }
    }
    public bool ClearPinnedSelected
    {
        get => _clearPinnedSelected;
        set
        {
            if (SetProperty(ref _clearPinnedSelected, value))
            {
                OnPropertyChanged(nameof(CanConfirmClearData));
            }
        }
    }
    public bool CanConfirmClearData => ClearTextSelected || ClearImagesSelected || ClearPinnedSelected;

    public string EmptyStateText
    {
        get => _emptyStateText;
        private set => SetProperty(ref _emptyStateText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand SetSortModeCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand CopyItemCommand { get; }
    public ICommand SelectItemCommand { get; }
    public ICommand OpenPreviewCommand { get; }
    public ICommand ClosePreviewCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CloseSettingsCommand { get; }
    public ICommand OpenClearDataCommand { get; }
    public ICommand CloseClearDataCommand { get; }
    public ICommand ConfirmClearDataCommand { get; }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message;
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var query = new ClipboardSearchQuery
            {
                Query = SearchQuery,
                Filter = SelectedFilter,
                SortMode = SelectedSortMode,
                Limit = 100,
                Offset = 0
            };

            var items = await _clipboardItemRepository.SearchAsync(query);
            ApplyItems(items);
        }
        catch
        {
            Items.Clear();
            EmptyStateText = "Could not load clipboard history.";
        }
        finally
        {
            IsLoading = false;
        }
    }


    private async Task DebouncedRefreshAsync()
    {
        _searchDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        try
        {
            await Task.Delay(250, cts.Token);
            if (!cts.IsCancellationRequested)
            {
                await RefreshAsync();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task SetFilterAsync(object? parameter)
    {
        if (parameter is ClipboardFilter filter)
        {
            SelectedFilter = filter;
            await RefreshAsync();
        }
    }

    private Task SetSortModeAsync(object? parameter)
    {
        if (parameter is SortMode sortMode)
        {
            SelectedSortMode = sortMode;
        }

        return Task.CompletedTask;
    }

    private async Task DeleteItemAsync(object? parameter)
    {
        if (parameter is not ClipboardItemViewModel item)
        {
            return;
        }

        await _clipboardItemRepository.DeleteAsync(item.Id);
        await RefreshAsync();
    }

    private async Task TogglePinAsync(object? parameter)
    {
        if (parameter is not ClipboardItemViewModel item)
        {
            return;
        }

        var entity = await _clipboardItemRepository.GetByIdAsync(item.Id);
        if (entity is null)
        {
            return;
        }

        var updated = CloneWithPinned(entity, !entity.IsPinned);
        await _clipboardItemRepository.UpdateAsync(updated);
        await RefreshAsync();
    }

    private async Task CopyItemAsync(object? parameter)
    {
        if (parameter is not ClipboardItemViewModel item)
        {
            return;
        }

        try
        {
            var result = await _copyTextItemToClipboardUseCase.ExecuteAsync(item.Id);
            SetStatusMessage(result.IsSuccess ? "Copied to clipboard." : "Could not copy item.");
        }
        catch
        {
            SetStatusMessage("Could not copy item.");
        }
    }

    private void SelectItem(object? parameter)
    {
        foreach (var entry in Items)
        {
            entry.IsSelected = false;
        }

        if (parameter is ClipboardItemViewModel item)
        {
            item.IsSelected = true;
            SelectedItem = item;
        }
    }

    public void OpenPreview(object? parameter = null)
    {
        if (parameter is ClipboardItemViewModel item)
        {
            SelectItem(item);
        }

        if (SelectedItem is null)
        {
            return;
        }

        IsSettingsOpen = false;
        IsPreviewOpen = true;
    }

    public void ClosePreview()
    {
        IsPreviewOpen = false;
    }

    public void OpenSettings()
    {
        IsPreviewOpen = false;
        IsSettingsOpen = true;
    }

    public void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    public void OpenClearData() => IsClearDataOpen = true;
    public void CloseClearData() => IsClearDataOpen = false;

    private async Task ConfirmClearDataAsync(object? parameter)
    {
        if (!CanConfirmClearData)
        {
            return;
        }

        var deletedItems = await _clipboardItemRepository.DeleteByCategoriesAsync(ClearTextSelected, ClearImagesSelected, ClearPinnedSelected);
        if (deletedItems.Count > 0)
        {
            var capture = await _clipboardGateway.ReadAsync();
            if (capture is not null)
            {
                string? hash = capture.Type switch
                {
                    ClipboardItemType.Text when !string.IsNullOrEmpty(capture.Text) => ClipboardTextHasher.Compute(capture.Text),
                    ClipboardItemType.Image when capture.ImageBytes is { Length: > 0 } => ClipboardImageHasher.Compute(capture.ImageBytes),
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(hash) && deletedItems.Any(x => x.Hash.Equals(hash, StringComparison.Ordinal)))
                {
                    _captureState.MarkInternalClipboardWrite();
                    await _clipboardGateway.ClearAsync();
                }
            }
        }

        await RefreshAsync();
        IsClearDataOpen = false;
    }

    private void ApplyItems(IReadOnlyList<ClipboardItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(ClipboardItemViewModel.FromEntity(item));
        }

        if (SelectedItem is not null)
        {
            var selectedId = SelectedItem.Id;
            SelectedItem = Items.FirstOrDefault(x => x.Id == SelectedItem.Id);
            if (SelectedItem is null)
            {
                IsPreviewOpen = false;
            }
            else
            {
                SelectedItem.IsSelected = true;
            }

            foreach (var item in Items.Where(x => x.Id != selectedId))
            {
                item.IsSelected = false;
            }
        }

        EmptyStateText = Items.Count == 0
            ? "No items match your current search and filters."
            : string.Empty;
    }

    private static ClipboardItem CloneWithPinned(ClipboardItem item, bool isPinned)
    {
        return new ClipboardItem
        {
            Id = item.Id,
            Type = item.Type,
            PreviewText = item.PreviewText,
            ContentText = item.ContentText,
            EncryptedContent = item.EncryptedContent,
            ImagePath = item.ImagePath,
            ThumbnailPath = item.ThumbnailPath,
            SourceApp = item.SourceApp,
            SourceWindowTitle = item.SourceWindowTitle,
            Hash = item.Hash,
            IsPinned = isPinned,
            IsProtected = item.IsProtected,
            CopyCount = item.CopyCount,
            SizeBytes = item.SizeBytes,
            CreatedAt = item.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            LastCopiedAt = item.LastCopiedAt
        };
    }
}
