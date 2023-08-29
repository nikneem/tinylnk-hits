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

        [HttpGet("{shortCode}/total")]
        public async Task<IActionResult> GetTotal(string shortCode, CancellationToken cancellationToken)
        {
            var ownerId = GetSubjectId();
            var responseDto = await _hitsService.GetHitsTotalAsync(shortCode, ownerId, cancellationToken);
            return Ok(responseDto);
        }

        public HitsController(IHitsService hitsService)
        {
            _hitsService = hitsService;
        }
    }
}
