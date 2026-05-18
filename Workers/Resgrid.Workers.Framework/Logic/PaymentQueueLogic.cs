using System;
using System.Threading.Tasks;
using Resgrid.Framework;
using Resgrid.Model;

namespace Resgrid.Workers.Framework.Logic
{
	// SparkOps Core: payment processing removed. All payment queue events are no-ops.
	public class PaymentQueueLogic
	{
		public static Task<bool> ProcessPaymentQueueItem(CqrsEvent qi)
		{
			if (qi != null)
				Logging.LogInfo($"PaymentQueueLogic: payment processing disabled in SparkOps Core (event type {qi.Type} discarded)");

			return Task.FromResult(true);
		}
	}
}
