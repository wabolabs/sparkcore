using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Resgrid.Web.Services.Controllers
{
	// SparkOps Core: Stripe webhook processing removed. Endpoint preserved so that any
	// misconfigured Stripe webhook delivery returns 200 OK rather than 404/500.
	[ApiController]
	[Route("api/[controller]")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class StripeHandlerController : ControllerBase
	{
		[HttpPost]
		public Task<IActionResult> Index()
		{
			return Task.FromResult<IActionResult>(Ok());
		}
	}
}
