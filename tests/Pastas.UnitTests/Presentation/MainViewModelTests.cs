using Pastas.Application.UseCases;
using Pastas.Application.State;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Presentation.ViewModels;
using Pastas.Shared.Result;

namespace Pastas.UnitTests.Presentation;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshAsync_LoadsItemsFromRepository()
    {
        var repository = new FakeClipboardItemRepository
        {
            SearchResults =
            {
                new ClipboardItem { PreviewText = "hello", ContentText = "hello", Hash = "a" }
            }
        };
        var viewModel = CreateViewModel(repository);

        await viewModel.RefreshAsync();

        Assert.Single(viewModel.Items);
    }

    [Fact]
    public async Task SearchCommand_UsesSearchQuery()
    {
        var repository = new FakeClipboardItemRepository();
        var viewModel = CreateViewModel(repository);
        viewModel.SearchQuery = "term";

        await viewModel.RefreshAsync();

        Assert.Equal("term", repository.LastSearchQuery?.Query);
    }

    [Fact]
    public async Task RefreshAsync_UsesSelectedFilter()
    {
        var repository = new FakeClipboardItemRepository();
        var viewModel = CreateViewModel(repository);
        viewModel.SelectedFilter = ClipboardFilter.Protected;

        await viewModel.RefreshAsync();

        Assert.Equal(ClipboardFilter.Protected, repository.LastSearchQuery?.Filter);
    }


    [Fact]
    public async Task SelectedSortMode_RefreshesImmediately()
    {
        var repository = new FakeClipboardItemRepository();
        var viewModel = CreateViewModel(repository);

        await viewModel.RefreshAsync();
        var initialCalls = repository.SearchCallCount;

        viewModel.SelectedSortMode = SortMode.Oldest;
        await Task.Delay(20);

        Assert.True(repository.SearchCallCount > initialCalls);
        Assert.Equal(SortMode.Oldest, repository.LastSearchQuery?.SortMode);
    }
    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryAndRefreshes()
    {
        var item = new ClipboardItem { Id = Guid.NewGuid(), PreviewText = "x", ContentText = "x", Hash = "x" };
        var repository = new FakeClipboardItemRepository { SearchResults = { item } };
        var viewModel = CreateViewModel(repository);
        await viewModel.RefreshAsync();

        viewModel.DeleteItemCommand.Execute(viewModel.Items[0]);
        await Task.Delay(20);

        Assert.Contains(item.Id, repository.DeletedIds);
        Assert.True(repository.SearchCallCount >= 2);
    }

    [Fact]
    public async Task TogglePinAsync_UpdatesItemAndRefreshes()
    {
        var item = new ClipboardItem { Id = Guid.NewGuid(), PreviewText = "x", ContentText = "x", Hash = "x", IsPinned = false };
        var repository = new FakeClipboardItemRepository { SearchResults = { item }, ItemsById = { [item.Id] = item } };
        var viewModel = CreateViewModel(repository);
        await viewModel.RefreshAsync();

        viewModel.TogglePinCommand.Execute(viewModel.Items[0]);
        await Task.Delay(20);

        Assert.Single(repository.UpdatedItems);
        Assert.True(repository.UpdatedItems[0].IsPinned);
        Assert.True(repository.SearchCallCount >= 2);
    }

    [Fact]
    public async Task CopyItemCommand_CallsUseCase_ForValidItem()
    {
        var itemId = Guid.NewGuid();
        var useCase = new FakeCopyTextItemToClipboardUseCase();
        var viewModel = CreateViewModel(copyUseCase: useCase);

        await viewModel.RefreshAsync();
        var vmItem = ClipboardItemViewModel.FromEntity(new ClipboardItem
        {
            Id = itemId,
            Type = ClipboardItemType.Text,
            PreviewText = "preview",
            ContentText = "preview",
            Hash = "hash-1",
            LastCopiedAt = DateTime.UtcNow
        });
        viewModel.CopyItemCommand.Execute(vmItem);
        await Task.Delay(20);

        Assert.Equal(itemId, useCase.LastItemId);
    }

    [Fact]
    public async Task CopyItemCommand_SetsSuccessStatusMessage()
    {
        var useCase = new FakeCopyTextItemToClipboardUseCase { ResultToReturn = Result.Success() };
        var viewModel = CreateViewModel(copyUseCase: useCase);

        var vmItem = ClipboardItemViewModel.FromEntity(new ClipboardItem
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Text,
            PreviewText = "preview",
            ContentText = "preview",
            Hash = "hash-2",
            LastCopiedAt = DateTime.UtcNow
        });
        viewModel.CopyItemCommand.Execute(vmItem);
        await Task.Delay(20);

        Assert.Equal("Copied to clipboard.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CopyItemCommand_SetsRestoreStatus_WhenPreviousClipboardIsAvailable()
    {
        var useCase = new FakeCopyTextItemToClipboardUseCase
        {
            ResultToReturn = Result.Success(),
            CanRestorePreviousClipboard = true
        };
        var viewModel = CreateViewModel(copyUseCase: useCase);
        var vmItem = ClipboardItemViewModel.FromEntity(new ClipboardItem
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Text,
            PreviewText = "preview",
            ContentText = "preview",
            Hash = "hash-copy-restore",
            LastCopiedAt = DateTime.UtcNow
        });

        viewModel.CopyItemCommand.Execute(vmItem);
        await Task.Delay(20);

        Assert.Equal("Item copied. Previous clipboard can be restored.", viewModel.StatusMessage);
        Assert.True(viewModel.CanRestorePreviousClipboard);
    }

    [Fact]
    public async Task RestorePreviousClipboardCommand_RestoresAndUpdatesStatus()
    {
        var useCase = new FakeCopyTextItemToClipboardUseCase { CanRestorePreviousClipboard = true };
        var viewModel = CreateViewModel(copyUseCase: useCase);

        viewModel.RestorePreviousClipboardCommand.Execute(null);
        await Task.Delay(20);

        Assert.True(useCase.RestoreCalled);
        Assert.Equal("Previous clipboard restored.", viewModel.StatusMessage);
        Assert.False(viewModel.CanRestorePreviousClipboard);
    }

    [Fact]
    public async Task CopyItemCommand_SetsFailureStatusMessage()
    {
        var useCase = new FakeCopyTextItemToClipboardUseCase
        {
            ResultToReturn = Result.Failure(new Error("copy.failed", "secret clipboard text"))
        };
        var viewModel = CreateViewModel(copyUseCase: useCase);

        var vmItem = ClipboardItemViewModel.FromEntity(new ClipboardItem
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Text,
            PreviewText = "preview",
            ContentText = "secret clipboard text",
            Hash = "hash-3",
            LastCopiedAt = DateTime.UtcNow
        });
        viewModel.CopyItemCommand.Execute(vmItem);
        await Task.Delay(20);

        Assert.Equal("Could not copy item.", viewModel.StatusMessage);
        Assert.DoesNotContain("secret clipboard text", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopyItemCommand_InvalidParameter_DoesNotThrow()
    {
        var viewModel = CreateViewModel();

        var exception = Record.Exception(() => viewModel.CopyItemCommand.Execute("invalid"));
        await Task.Delay(20);

        Assert.Null(exception);
    }


    [Fact]
    public async Task ConfirmClearDataCommand_RefreshesListAndClosesPreview_WhenSelectedItemDeleted()
    {
        var item = new ClipboardItem { Id = Guid.NewGuid(), Type = ClipboardItemType.Text, PreviewText = "x", ContentText = "x", Hash = "x" };
        var repository = new FakeClipboardItemRepository { SearchResults = { item }, DeletedByCategoriesResults = { item } };
        var viewModel = CreateViewModel(repository);
        await viewModel.RefreshAsync();
        viewModel.OpenPreview(viewModel.Items[0]);
        repository.SearchResults.Clear();

        viewModel.OpenClearData();
        viewModel.ClearTextSelected = true;
        viewModel.ConfirmClearDataCommand.Execute(null);
        await Task.Delay(20);

        Assert.True(repository.DeleteByCategoriesCalled);
        Assert.Empty(viewModel.Items);
        Assert.False(viewModel.IsPreviewOpen);
        Assert.False(viewModel.IsClearDataOpen);
    }

    [Fact]
    public void OpenSettings_ClosesPreviewAndClearData()
    {
        var viewModel = CreateViewModel();

        viewModel.OpenClearData();
        viewModel.OpenPreview();
        viewModel.OpenSettings();

        Assert.True(viewModel.IsSettingsOpen);
        Assert.False(viewModel.IsPreviewOpen);
        Assert.False(viewModel.IsClearDataOpen);
    }

    [Fact]
    public void CloseSettings_ReturnsToMainListVisibility()
    {
        var viewModel = CreateViewModel();

        viewModel.OpenSettings();
        viewModel.CloseSettings();

        Assert.False(viewModel.IsSettingsOpen);
        Assert.True(viewModel.IsMainContentVisible);
    }

    [Fact]
    public async Task SelectNextAndPreviousItem_MoveSelectedItem()
    {
        var first = new ClipboardItem { Id = Guid.NewGuid(), PreviewText = "a", ContentText = "a", Hash = "a" };
        var second = new ClipboardItem { Id = Guid.NewGuid(), PreviewText = "b", ContentText = "b", Hash = "b" };
        var repository = new FakeClipboardItemRepository { SearchResults = { first, second } };
        var viewModel = CreateViewModel(repository);
        await viewModel.RefreshAsync();

        viewModel.SelectNextItem();
        Assert.Equal(first.Id, viewModel.SelectedItem?.Id);

        viewModel.SelectNextItem();
        Assert.Equal(second.Id, viewModel.SelectedItem?.Id);

        viewModel.SelectPreviousItem();
        Assert.Equal(first.Id, viewModel.SelectedItem?.Id);
    }

    [Fact]
    public void DoNotSaveNextCommand_MarksCaptureStateOnce()
    {
        var captureState = new ClipboardCaptureState();
        var viewModel = CreateViewModel(captureState: captureState);

        viewModel.DoNotSaveNextCommand.Execute(null);

        Assert.Equal("Next clipboard change will not be saved.", viewModel.StatusMessage);
        Assert.True(captureState.ConsumeDoNotSaveNextFlag());
        Assert.False(captureState.ConsumeDoNotSaveNextFlag());
    }

    private static MainViewModel CreateViewModel(
        FakeClipboardItemRepository? repository = null,
        FakeCopyTextItemToClipboardUseCase? copyUseCase = null,
        ClipboardCaptureState? captureState = null)
    {
        return new MainViewModel(
            repository ?? new FakeClipboardItemRepository(),
            copyUseCase ?? new FakeCopyTextItemToClipboardUseCase(),
            new FakeClipboardGateway(),
            captureState ?? new ClipboardCaptureState());
    }

    private sealed class FakeCopyTextItemToClipboardUseCase : ICopyTextItemToClipboardUseCase
    {
        public Guid? LastItemId { get; private set; }
        public Result ResultToReturn { get; set; } = Result.Success();
        public Result RestoreResultToReturn { get; set; } = Result.Success();
        public bool CanRestorePreviousClipboard { get; set; }
        public bool RestoreCalled { get; private set; }

        public Task<Result> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            LastItemId = itemId;
            return Task.FromResult(ResultToReturn);
        }

        public Task<Result> RestorePreviousClipboardAsync(CancellationToken cancellationToken = default)
        {
            RestoreCalled = true;
            CanRestorePreviousClipboard = false;
            return Task.FromResult(RestoreResultToReturn);
        }
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public List<ClipboardItem> SearchResults { get; } = new();
        public ClipboardSearchQuery? LastSearchQuery { get; private set; }
        public int SearchCallCount { get; private set; }
        public List<Guid> DeletedIds { get; } = new();
        public List<ClipboardItem> UpdatedItems { get; } = new();
        public Dictionary<Guid, ClipboardItem> ItemsById { get; } = new();
        public List<ClipboardItem> DeletedByCategoriesResults { get; } = new();
        public bool DeleteByCategoriesCalled { get; private set; }

        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
        {
            UpdatedItems.Add(item);
            ItemsById[item.Id] = item;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            SearchResults.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            ItemsById.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }

        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default)
        {
            DeleteByCategoriesCalled = true;
            return Task.FromResult<IReadOnlyList<ClipboardItem>>(DeletedByCategoriesResults.ToList());
        }

        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
        {
            LastSearchQuery = query;
            SearchCallCount++;
            return Task.FromResult<IReadOnlyList<ClipboardItem>>(SearchResults.ToList());
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(SearchResults.Count);
        public Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StorageStats
            {
                TotalItems = SearchResults.Count,
                PinnedItems = SearchResults.Count(x => x.IsPinned),
                ImageItems = SearchResults.Count(x => x.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot),
                ApproxUsageBytes = SearchResults.Sum(x => Math.Max(0, x.SizeBytes))
            });
    }

    private sealed class FakeClipboardGateway : IClipboardGateway
    {
        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardCaptureData?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult<ClipboardCaptureData?>(null);
        public Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteImageBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
