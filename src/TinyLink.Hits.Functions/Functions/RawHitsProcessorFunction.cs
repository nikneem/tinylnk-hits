using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TinyLink.Core.Commands.CommandMessages;
using TinyLink.Core.Commands;
using TinyLink.Hits.Abstractions.Services;

namespace TinyLink.Hits.Functions.Functions;

public class RawHitsProcessorFunction
{
    private readonly IHitsService _hitsService;
    private readonly ILogger<RawHitsProcessorFunction> _logger;

    public RawHitsProcessorFunction(
        IHitsService hitsService, 
        ILogger<RawHitsProcessorFunction> logger)
    {
        _hitsService = hitsService;
        _logger = logger;
    }

    [Function(nameof(RawHitsProcessorFunction))]
    public async Task Run(
        [ServiceBusTrigger(QueueName.HitsProcessor)]
        ProcessHitCommand hit,
        CancellationToken cancellationToken
    )
    {
        _logger.LogTrace("Incoming hit for {shortCode}", hit.ShortCode);
        var succeeded = await _hitsService.RawHitsProcessor(hit, cancellationToken);
        _logger.LogInformation("Processing incoming hits succeeded: {succeeded}", succeeded);
    }

}
