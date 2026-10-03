using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Resgrid.Model;
using Resgrid.Model.Identity;

namespace Resgrid.Web.Areas.User.Models
{
	public class CallsDashboardModel: BaseUserModel
	{
		public string Latitude { get; set; }
		public string Longitude { get; set; }
		public Department Department { get; set; }
		public IdentityUser User { get; set; }
		public List<Call> Calls { get; set; }
		public Call NewCall { get; set; }
		public Call ViewCall { get; set; }
		public string ModalCssClass { get; set; }
		public string ViewModalCssClass { get; set; }
		public string ViewModalStyle { get; set; }
		public string EditModalCssClass { get; set; }
		public string EditModalStyle { get; set; }
		public string Message { get; set; }
		public string Year { get; set; }
		public List<SelectListItem> Years { get; set; }

		/// <summary>
		/// Who is currently responding, per active call (A2). Built from
		/// ICallRespondingService — the same logic the v4 responding endpoint
		/// exposes to external clients.
		/// </summary>
		public List<NowRespondingCall> NowResponding { get; set; } = new List<NowRespondingCall>();
	}

	/// <summary>One active call with its currently responding personnel and units.</summary>
	public class NowRespondingCall
	{
		public Call Call { get; set; }

		public CallRespondingSnapshot Responding { get; set; }

		/// <summary>First arrival, department-local, pre-formatted for the panel.</summary>
		public string FirstArrivalText { get; set; }

		/// <summary>Projected first arrival, department-local, pre-formatted.</summary>
		public string EtaText { get; set; }
	}
}
