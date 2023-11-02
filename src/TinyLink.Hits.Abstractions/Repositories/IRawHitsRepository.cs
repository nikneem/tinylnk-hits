using TinyLink.Hits.Abstractions.DomainModels;

namespace TinyLink.Hits.Abstractions.Repositories;

public interface IRawHitsRepository
{
    Task<bool> Create(IRawHit domainModel, CancellationToken cancellationToken = default);
}