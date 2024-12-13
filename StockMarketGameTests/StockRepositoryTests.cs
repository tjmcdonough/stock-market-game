using Moq;
using StockMarketGame.Database;
using StockMarketGame.Entities;
using StockMarketGame.Exceptions;
using StockMarketGame.Repository;

namespace StockMarketGameTests;

public class StockRepositoryTests
{
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly StockRepository _repository;
    private const string CollectionName = "stocks";

    public StockRepositoryTests()
    {
        _mockDatabase = new Mock<IDatabase>();
        _repository = new StockRepository(_mockDatabase.Object);
    }

    [Fact]
    public async Task Add_ValidStock_CallsDatabase()
    {
        // Arrange
        Stock stock = new("test-stock", 1000);
        _mockDatabase.Setup(db => db.Insert(CollectionName, stock.Name, stock))
            .ReturnsAsync(true);

        // Act
        bool result = await _repository.Add(stock);

        // Assert
        Assert.True(result);
        _mockDatabase.Verify(db => db.Insert(CollectionName, stock.Name, stock), Times.Once);
    }

    [Fact]
    public async Task Add_WhenDatabaseThrowsDuplicateException_PropagatesException()
    {
        // Arrange
        Stock stock = new("test-stock", 1000);
        _mockDatabase.Setup(db => db.Insert(CollectionName, stock.Name, stock))
            .ThrowsAsync(new DuplicateKeyException(CollectionName, stock.Name));

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateKeyException>(() => 
            _repository.Add(stock));
    }

    [Fact]
    public async Task Get_ExistingStock_ReturnsStock()
    {
        // Arrange
        Stock stock = new("test-stock", 1000);
        _mockDatabase.Setup(db => db.GetById<Stock>(CollectionName, stock.Name))
            .ReturnsAsync(stock);

        // Act
        Stock? result = await _repository.Get(stock.Name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stock.Name, result.Name);
        Assert.Equal(stock.Price, result.Price);
    }

    [Fact]
    public async Task Get_NonExistentStock_ReturnsNull()
    {
        // Arrange
        _mockDatabase.Setup(db => db.GetById<Stock>(CollectionName, "non-existent"))
            .ReturnsAsync((Stock?)null);

        // Act
        Stock? result = await _repository.Get("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task List_WithStocks_ReturnsAllStocks()
    {
        // Arrange
        List<Stock> stocks =
        [
            new("stock-1", 1000),
            new("stock-2", 2000)
        ];

        _mockDatabase.Setup(db => db.FindAll<Stock>(CollectionName))
            .ReturnsAsync(stocks);

        // Act
        IEnumerable<Stock> result = await _repository.List();

        // Assert
        Assert.Equal(stocks.Count, result.Count());
        Assert.All(stocks, stock => 
            Assert.Contains(result, s => s.Name == stock.Name && s.Price == stock.Price));
    }

    [Fact]
    public async Task List_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _mockDatabase.Setup(db => db.FindAll<Stock>(CollectionName))
            .ReturnsAsync(Array.Empty<Stock>());

        // Act
        IEnumerable<Stock> result = await _repository.List();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Update_ExistingStock_UpdatesSuccessfully()
    {
        // Arrange
        Stock stock = new("test-stock", 1000);
        _mockDatabase.Setup(db => db.Update(CollectionName, stock.Name, stock))
            .ReturnsAsync(true);

        // Act
        bool result = await _repository.Update(stock);

        // Assert
        Assert.True(result);
        _mockDatabase.Verify(db => db.Update(CollectionName, stock.Name, stock), Times.Once);
    }

    [Fact]
    public async Task Delete_ExistingStock_DeletesSuccessfully()
    {
        // Arrange
        string stockName = "test-stock";
        _mockDatabase.Setup(db => db.Delete(CollectionName, stockName))
            .ReturnsAsync(true);

        // Act
        bool result = await _repository.Delete(stockName);

        // Assert
        Assert.True(result);
        _mockDatabase.Verify(db => db.Delete(CollectionName, stockName), Times.Once);
    }

    [Fact]
    public async Task GetPopularStocks_ReturnsOrderedByPopularity()
    {
        // Arrange
        List<Stock> stocks =
        [
            new("stock-1", 1000),
            new("stock-2", 1000),
            new("stock-3", 1000)
        ];
        
        stocks[0].SetPopularity(5);
        stocks[1].SetPopularity(10);
        stocks[2].SetPopularity(3);

        _mockDatabase.Setup(db => db.GetTopByField<Stock>(CollectionName, "Popularity", 3, true))
            .ReturnsAsync(stocks);

        // Act
        List<Stock> result = (await _repository.GetPopularStocks(3)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("stock-2", result[0].Name);
        Assert.Equal("stock-1", result[1].Name);
        Assert.Equal("stock-3", result[2].Name);
    }

    [Fact]
    public async Task GetPopularStocks_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _mockDatabase.Setup(db => db.GetTopByField<Stock>(CollectionName, "Popularity", 3, true))
            .ReturnsAsync(Array.Empty<Stock>());

        // Act
        IEnumerable<Stock> result = await _repository.GetPopularStocks(3);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPopularStocks_DefaultLimit_UsesTen()
    {
        // Arrange
        List<Stock> stocks = Enumerable.Range(1, 15)
            .Select(i => 
            {
                Stock stock = new($"stock-{i}", 1000);
                stock.SetPopularity(i);
                return stock;
            })
            .ToList();

        _mockDatabase.Setup(db => db.GetTopByField<Stock>(CollectionName, "Popularity", 10, true))
            .ReturnsAsync(stocks);

        // Act
        IEnumerable<Stock> result = await _repository.GetPopularStocks();

        // Assert
        Assert.Equal(10, result.Count());
        _mockDatabase.Verify(db => db.GetTopByField<Stock>(
            CollectionName, "Popularity", 10, true), Times.Once);
    }
}