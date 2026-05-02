using System.Collections.ObjectModel;
using System.Windows.Input;
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
    private string _searchQuery = string.Empty;
    private ClipboardFilter _selectedFilter = ClipboardFilter.All;
    private SortMode _selectedSortMode = SortMode.Recent;
    private bool _isLoading;
    private string _emptyStateText = "No clipboard items yet.";
    private string _statusMessage = string.Empty;

    public MainViewModel(
        IClipboardItemRepository clipboardItemRepository,
        ICopyTextItemToClipboardUseCase copyTextItemToClipboardUseCase)
    {
        _clipboardItemRepository = clipboardItemRepository;
        _copyTextItemToClipboardUseCase = copyTextItemToClipboardUseCase;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SearchCommand = new AsyncRelayCommand(RefreshAsync);
        SetFilterCommand = new AsyncRelayCommand(SetFilterAsync);
        SetSortModeCommand = new AsyncRelayCommand(SetSortModeAsync);
        DeleteItemCommand = new AsyncRelayCommand(DeleteItemAsync);
        TogglePinCommand = new AsyncRelayCommand(TogglePinAsync);
        CopyItemCommand = new AsyncRelayCommand(CopyItemAsync);
    }

    public ObservableCollection<ClipboardItemViewModel> Items { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public ClipboardFilter SelectedFilter
    {
        get => _selectedFilter;
        set => SetProperty(ref _selectedFilter, value);
    }

    public SortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set => SetProperty(ref _selectedSortMode, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

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

    private async Task SetFilterAsync(object? parameter)
    {
        if (parameter is ClipboardFilter filter)
        {
            SelectedFilter = filter;
            await RefreshAsync();
        }
    }

    private async Task SetSortModeAsync(object? parameter)
    {
        if (parameter is SortMode sortMode)
        {
            SelectedSortMode = sortMode;
            await RefreshAsync();
        }
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

    private void ApplyItems(IReadOnlyList<ClipboardItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(ClipboardItemViewModel.FromEntity(item));
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
