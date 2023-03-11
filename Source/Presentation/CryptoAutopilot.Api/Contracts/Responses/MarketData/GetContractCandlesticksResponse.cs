using Bybit.Net.Enums;

using CryptoAutopilot.Api.Contracts.Responses.Common;

namespace CryptoAutopilot.Api.Contracts.Responses.MarketData;

public class GetContractCandlesticksResponse
{
    public required string CurrencyPair { get; init; }
    public required KlineInterval Timeframe { get; init; }
    public required IEnumerable<CandlestickResponse> Candlesticks { get; init; }
}
