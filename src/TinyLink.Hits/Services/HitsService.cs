using TinyLink.Core.Commands.CommandMessages;
using TinyLink.Hits.Abstractions.DataTransferObjects;
using TinyLink.Hits.Abstractions.Repositories;
using TinyLink.Hits.Abstractions.Services;
using TinyLink.Hits.DomainModels;

namespace TinyLink.Hits.Services;

public class HitsService : IHitsService
{
    private readonly IRawHitsRepository _rawHitsRepository;
    private readonly ICalculateHitsRepository _calculateHitsRepository;
    private readonly IHitsTotalRepository _hitsTotalRepository;

    public Task<bool> RawHitsProcessor(ProcessHitCommand command, CancellationToken cancellationToken = default)
    {
        var rawHit = RawHit.Create(command.OwnerId, command.Id, command.ShortCode);
        return _rawHitsRepository.Create(rawHit, cancellationToken);
    }

    public Task<bool> CalculateHitsProcessor(ProcessHitCommand command, CancellationToken cancellationToken = default)
    {
        var rawHit = RawHit.Create(command.OwnerId, command.Id, command.ShortCode);
        return _calculateHitsRepository.Create(rawHit, cancellationToken);
    }

    public Task<HitsTotalDto> GetHitsTotalAsync(string shortCode, string ownerId, CancellationToken cancellationToken = default)
    {
        return _hitsTotalRepository.GetAsync(ownerId, shortCode, cancellationToken);
    }

    public HitsService(
        IRawHitsRepository rawHitsRepository,
        ICalculateHitsRepository calculateHitsRepository,
        IHitsTotalRepository hitsTotalRepository)
    {
        _rawHitsRepository = rawHitsRepository;
        _calculateHitsRepository = calculateHitsRepository;
        _hitsTotalRepository = hitsTotalRepository;
    }
}