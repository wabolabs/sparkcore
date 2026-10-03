using System.Collections.Generic;
using System.Threading.Tasks;

namespace Resgrid.Model.Services
{
	/// <summary>
	/// The read side of call dispatch statuses: who is currently responding to a
	/// call (personnel and units whose current status is tied to the call and means
	/// en-route or on-scene), with first arrival actual or projected.
	/// </summary>
	public interface ICallRespondingService
	{
		/// <summary>
		/// Builds the responding snapshot for one call. Callers are responsible for
		/// department and view-call authorization before calling.
		/// </summary>
		Task<CallRespondingSnapshot> GetRespondingForCallAsync(int departmentId, int callId);

		/// <summary>
		/// Builds snapshots for several calls in one pass, fetching the
		/// department-wide current-status lists once. The dashboard asks for every
		/// active call; doing this per call would repeat the expensive lookups.
		/// </summary>
		Task<List<CallRespondingSnapshot>> GetRespondingForCallsAsync(int departmentId, IEnumerable<int> callIds);
	}
}
