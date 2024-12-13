namespace StockMarketGame.Exceptions;

public class NoCompleteStockSetException(
    long timestamp,
    int stockCount)
    : StockMarketGameException($"No complete set of stock values found. " +
                               $"Latest timestamp: {timestamp}, latest stock count: {stockCount}");