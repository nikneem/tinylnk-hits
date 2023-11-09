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
    private readonly IHitsByTenMinutesRepository _hitsByTenMinutesRepository;

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

    public Task<List<CumulatedHitDto>> GetCumulatedHits(Guid id, string ownerId, DateTimeOffset startDate, CancellationToken cancellationToken = default)
    {
        var timespan = DateTimeOffset.UtcNow - startDate;
        if (timespan.TotalDays > 365)
        {
            throw new Exception("Cannot go that far back");
        }

        return _hitsByTenMinutesRepository.Get(id, ownerId, startDate, cancellationToken);
    }

    public Task<HitsTotalDto> GetHitsTotalAsync(string shortCode, string ownerId, CancellationToken cancellationToken = default)
    {
        return _hitsTotalRepository.GetAsync(ownerId, shortCode, cancellationToken);
    }

    public HitsService(
        IRawHitsRepository rawHitsRepository,
        ICalculateHitsRepository calculateHitsRepository,
        IHitsTotalRepository hitsTotalRepository,
        IHitsByTenMinutesRepository hitsByTenMinutesRepository)
    {
        _rawHitsRepository = rawHitsRepository;
        _calculateHitsRepository = calculateHitsRepository;
        _hitsTotalRepository = hitsTotalRepository;
        _hitsByTenMinutesRepository = hitsByTenMinutesRepository;
    }
}