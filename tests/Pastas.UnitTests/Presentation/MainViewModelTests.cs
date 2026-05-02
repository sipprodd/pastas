using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Presentation.ViewModels;

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
        var viewModel = new MainViewModel(repository);

        await viewModel.RefreshAsync();

        Assert.Single(viewModel.Items);
    }

    [Fact]
    public async Task SearchCommand_UsesSearchQuery()
    {
        var repository = new FakeClipboardItemRepository();
        var viewModel = new MainViewModel(repository)
        {
            SearchQuery = "term"
        };

        await viewModel.RefreshAsync();

        Assert.Equal("term", repository.LastSearchQuery?.Query);
    }

    [Fact]
    public async Task RefreshAsync_UsesSelectedFilter()
    {
        var repository = new FakeClipboardItemRepository();
        var viewModel = new MainViewModel(repository)
        {
            SelectedFilter = ClipboardFilter.Protected
        };

        await viewModel.RefreshAsync();

        Assert.Equal(ClipboardFilter.Protected, repository.LastSearchQuery?.Filter);
    }

    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryAndRefreshes()
    {
        var item = new ClipboardItem { Id = Guid.NewGuid(), PreviewText = "x", ContentText = "x", Hash = "x" };
        var repository = new FakeClipboardItemRepository { SearchResults = { item } };
        var viewModel = new MainViewModel(repository);
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
        var viewModel = new MainViewModel(repository);
        await viewModel.RefreshAsync();

        viewModel.TogglePinCommand.Execute(viewModel.Items[0]);
        await Task.Delay(20);

        Assert.Single(repository.UpdatedItems);
        Assert.True(repository.UpdatedItems[0].IsPinned);
        Assert.True(repository.SearchCallCount >= 2);
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public List<ClipboardItem> SearchResults { get; } = new();
        public ClipboardSearchQuery? LastSearchQuery { get; private set; }
        public int SearchCallCount { get; private set; }
        public List<Guid> DeletedIds { get; } = new();
        public List<ClipboardItem> UpdatedItems { get; } = new();
        public Dictionary<Guid, ClipboardItem> ItemsById { get; } = new();

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

        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
        {
            LastSearchQuery = query;
            SearchCallCount++;
            return Task.FromResult<IReadOnlyList<ClipboardItem>>(SearchResults.ToList());
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(SearchResults.Count);
    }
}
