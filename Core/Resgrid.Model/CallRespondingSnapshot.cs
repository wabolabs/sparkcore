using System;
using System.Collections.Generic;

namespace Resgrid.Model
{
	/// <summary>
	/// Who is currently responding to a call, as one shape: personnel and units whose
	/// current status is tied to the call and means en-route or on-scene, plus the
	/// first arrival (actual or projected).
	///
	/// This is the read side of <see cref="Services.ICallDispatchStatusService"/>:
	/// that service writes dispatch statuses, this one reports the result. The
	/// classification (default status ids and custom-state base types) was extracted
	/// from the chatbot's CallRespondersActionHandler so the v4 API and the chatbot
	/// answer "who's responding?" the same way instead of drifting apart.
	/// </summary>
	public class CallRespondingSnapshot
	{
		/// <summary>Bucket for a responder who is coming to the call.</summary>
		public const string BucketEnRoute = "enroute";

		/// <summary>Bucket for a responder who is on the call's scene.</summary>
		public const string BucketOnScene = "onscene";

		public int CallId { get; set; }

		public List<CallRespondingPerson> Personnel { get; set; } = new List<CallRespondingPerson>();

		public List<CallRespondingUnit> Units { get; set; } = new List<CallRespondingUnit>();

		/// <summary>
		/// Earliest on-scene timestamp in the call's history (unit or personnel),
		/// or null while nobody has arrived.
		/// </summary>
		public DateTime? FirstArrivalAt { get; set; }

		/// <summary>
		/// Projected first arrival while nobody is on scene yet: the earliest
		/// personnel ETA (EtaPulledOn + Eta seconds) among current en-route
		/// responders. Null once someone is on scene, and null when no route ETA is
		/// known (GeoService answers -1 without a route) — never invented.
		/// </summary>
		public DateTime? FirstArrivalEta { get; set; }
	}

	/// <summary>
	/// One person currently responding to a call: their call status, the bucket it
	/// falls in, and their staffing state.
	/// </summary>
	public class CallRespondingPerson
	{
		public string UserId { get; set; }

		public string Name { get; set; }

		public int? GroupId { get; set; }

		public string Group { get; set; }

		public int StatusId { get; set; }

		public string StatusText { get; set; }

		public string StatusColor { get; set; }

		public DateTime Timestamp { get; set; }

		/// <summary><see cref="CallRespondingSnapshot.BucketEnRoute"/> or <see cref="CallRespondingSnapshot.BucketOnScene"/>.</summary>
		public string Bucket { get; set; }

		/// <summary>Projected arrival (EtaPulledOn + Eta seconds) when a route ETA is known; null otherwise.</summary>
		public DateTime? EtaAt { get; set; }

		/// <summary>The person's staffing state (Available, Unavailable, ...), if one is on record.</summary>
		public string StaffingText { get; set; }

		public string StaffingColor { get; set; }
	}

	/// <summary>
	/// One unit currently responding to a call: its call state, the bucket it falls
	/// in, and its station group.
	/// </summary>
	public class CallRespondingUnit
	{
		public int UnitId { get; set; }

		public string Name { get; set; }

		public string Type { get; set; }

		public int? GroupId { get; set; }

		public string Group { get; set; }

		public int StateId { get; set; }

		public string StateText { get; set; }

		public string StateColor { get; set; }

		public DateTime Timestamp { get; set; }

		/// <summary><see cref="CallRespondingSnapshot.BucketEnRoute"/> or <see cref="CallRespondingSnapshot.BucketOnScene"/>.</summary>
		public string Bucket { get; set; }
	}
}
