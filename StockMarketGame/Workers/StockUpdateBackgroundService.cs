using StockMarketGame.Entities;
using StockMarketGame.Interfaces;

namespace StockMarketGame.Workers;

public class StockUpdateBackgroundService(
    IStockRepository stockRepository,
    IStockMarket stockMarket,
    ILogger<StockUpdateBackgroundService> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            await _updateLock.WaitAsync(stoppingToken);
            try
            {
                long batchCreatedDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                IEnumerable<Stock> stocks = await stockRepository.List();
                
                List<Task> updates = [];
                updates.AddRange(stocks.Select(stock => Task.Run(async () =>
                {
                    try
                    {
                        long newPrice = stockMarket.GetNextStockPrice(stock.Name, stock.Price);
                        stock.SetPrice(newPrice);
                        stock.SetUpdatedAt(batchCreatedDate);
                        await stockRepository.Update(stock);
                        logger.LogInformation("Stock {StockName}; Price {Price}; Date: {Date}", stock.Name, newPrice, batchCreatedDate);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing stock {StockName}", stock.Name);
                        throw;
                    }
                }, stoppingToken)));

                await Task.WhenAll(updates);
            }
            finally
            {
                _updateLock.Release();
            }
        }
    }
}