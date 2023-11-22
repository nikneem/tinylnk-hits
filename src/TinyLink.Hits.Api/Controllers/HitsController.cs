using Microsoft.AspNetCore.Mvc;
using TinyLink.Hits.Abstractions.Services;
using TinyLink.Hits.Api.Controllers.Base;

namespace TinyLink.Hits.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HitsController : AuthenticatedControllerBase
    {
        private readonly IHitsService _hitsService;

        [HttpGet("{id:guid}/cumulated")]
        public async Task<IActionResult> GetCumulated(Guid id, [FromQuery]DateTimeOffset? fromDate, CancellationToken cancellationToken)
        {
            var ownerId = GetSubjectId();
            var sanitizedFromDate = fromDate.HasValue ?  fromDate.Value : DateTimeOffset.UtcNow.AddDays(-1);
            var response = await _hitsService.GetCumulatedHits(id, ownerId, sanitizedFromDate, cancellationToken);
            return Ok(response);
        }

        public HitsController(IHitsService hitsService)
        {
            _hitsService = hitsService;
        }
    }
}
