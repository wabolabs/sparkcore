using System;
using System.Collections.Generic;
using System.Linq;
using Resgrid.Model;

namespace Resgrid.Web.Services.Models.v4.Calls
{
	/// <summary>
	/// Who is currently responding to a call (A1): personnel and units whose current
	/// status is tied to the call and means en-route or on-scene, plus first arrival
	/// actual or projected.
	/// </summary>
	public class CallRespondingResult : StandardApiResponseV4Base
	{
		/// <summary>Response Data</summary>
		public CallRespondingResultData Data { get; set; }

		/// <summary>Default constructor</summary>
		public CallRespondingResult()
		{
			Data = new CallRespondingResultData();
		}
	}

	/// <summary>Depicts who is responding to a call.</summary>
	public class CallRespondingResultData
	{
		public string CallId { get; set; }

		/// <summary>Earliest on-scene timestamp in the call's history, or null.</summary>
		public DateTime? FirstArrivalAt { get; set; }

		/// <summary>Projected first arrival while nobody is on scene and a route ETA is known; null otherwise.</summary>
		public DateTime? FirstArrivalEta { get; set; }

		public List<CallRespondingPersonResultData> Personnel { get; set; } = new List<CallRespondingPersonResultData>();

		public List<CallRespondingUnitResultData> Units { get; set; } = new List<CallRespondingUnitResultData>();

		public static CallRespondingResultData Convert(CallRespondingSnapshot snapshot)
		{
			var data = new CallRespondingResultData();

			if (snapshot == null)
				return data;

			data.CallId = snapshot.CallId.ToString();
			data.FirstArrivalAt = snapshot.FirstArrivalAt;
			data.FirstArrivalEta = snapshot.FirstArrivalEta;

			data.Personnel = (snapshot.Personnel ?? new List<CallRespondingPerson>())
				.Select(x => new CallRespondingPersonResultData
				{
					UserId = x.UserId,
					Name = x.Name,
					GroupId = x.GroupId?.ToString(),
					Group = x.Group,
					StatusId = x.StatusId,
					StatusText = x.StatusText,
					StatusColor = x.StatusColor,
					Timestamp = x.Timestamp,
					Bucket = x.Bucket,
					EtaAt = x.EtaAt,
					StaffingText = x.StaffingText,
					StaffingColor = x.StaffingColor
				})
				.ToList();

			data.Units = (snapshot.Units ?? new List<CallRespondingUnit>())
				.Select(x => new CallRespondingUnitResultData
				{
					UnitId = x.UnitId,
					Name = x.Name,
					Type = x.Type,
					GroupId = x.GroupId?.ToString(),
					Group = x.Group,
					StateId = x.StateId,
					StateText = x.StateText,
					StateColor = x.StateColor,
					Timestamp = x.Timestamp,
					Bucket = x.Bucket
				})
				.ToList();

			return data;
		}
	}

	/// <summary>One person currently responding to a call.</summary>
	public class CallRespondingPersonResultData
	{
		public string UserId { get; set; }
		public string Name { get; set; }
		public string GroupId { get; set; }
		public string Group { get; set; }
		public int StatusId { get; set; }
		public string StatusText { get; set; }
		public string StatusColor { get; set; }
		public DateTime Timestamp { get; set; }

		/// <summary>"enroute" or "onscene".</summary>
		public string Bucket { get; set; }

		/// <summary>Projected arrival when a route ETA is known; null otherwise.</summary>
		public DateTime? EtaAt { get; set; }

		public string StaffingText { get; set; }
		public string StaffingColor { get; set; }
	}

	/// <summary>One unit currently responding to a call.</summary>
	public class CallRespondingUnitResultData
	{
		public int UnitId { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public string GroupId { get; set; }
		public string Group { get; set; }
		public int StateId { get; set; }
		public string StateText { get; set; }
		public string StateColor { get; set; }
		public DateTime Timestamp { get; set; }

		/// <summary>"enroute" or "onscene".</summary>
		public string Bucket { get; set; }
	}
}
