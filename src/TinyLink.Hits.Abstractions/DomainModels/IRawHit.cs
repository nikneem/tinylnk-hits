using HexMaster.DomainDrivenDesign.Abstractions;

namespace TinyLink.Hits.Abstractions.DomainModels;

public interface IRawHit : IDomainModel<Guid>
{
    public Guid ShortCodeId { get; }
    public string OwnerId { get; }
    public string ShortCode { get; }
    public DateTimeOffset CreatedOn { get; }
}