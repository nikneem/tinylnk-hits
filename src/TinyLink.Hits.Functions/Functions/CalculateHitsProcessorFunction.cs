using System;
using System.Threading;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TinyLink.Core.Commands;
using TinyLink.Core.Commands.CommandMessages;
using TinyLink.Hits.Abstractions.Services;

namespace TinyLink.Hits.Functions.Functions
{
    public class CalculateHitsProcessorFunction
    {
        private readonly IHitsService _hitsService;
        private readonly ILogger<CalculateHitsProcessorFunction> _logger;

        public CalculateHitsProcessorFunction(
            IHitsService hitsService,
            ILogger<CalculateHitsProcessorFunction> logger)
        {
            _hitsService = hitsService;
            _logger = logger;
        }

        [Function(nameof(CalculateHitsProcessorFunction))]
        public async Task Run(
            [ServiceBusTrigger(QueueName.HitsCumulator)]
            ProcessHitCommand hit,
            CancellationToken cancellationToken)

        {
            _logger.LogTrace("Incoming hit for calculation ({shortCode})", hit.ShortCode);
            var succeeded = await _hitsService.CalculateHitsProcessor(hit, cancellationToken);
            _logger.LogInformation("Processing incoming hits for calculation succeeded: {succeeded}", succeeded);
        }
    }
}
