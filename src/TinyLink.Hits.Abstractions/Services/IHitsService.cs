using TinyLink.Core.Commands.CommandMessages;
using TinyLink.Hits.Abstractions.DataTransferObjects;

namespace TinyLink.Hits.Abstractions.Services;

public interface IHitsService
{
    Task<bool> RawHitsProcessor(ProcessHitCommand command, CancellationToken cancellationToken = default);
    Task<bool> CalculateHitsProcessor(ProcessHitCommand command, CancellationToken cancellationToken = default);

    Task<HitsTotalDto> GetHitsTotalAsync(
        string shortCode, 
        string ownerId,
        CancellationToken cancellationToken = default);
}