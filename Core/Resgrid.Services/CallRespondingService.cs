using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resgrid.Model;
using Resgrid.Model.Services;

namespace Resgrid.Services
{
	/// <summary>
	/// Builds the "who is responding?" snapshot for a call: personnel and units whose
	/// CURRENT status (the latest action log / unit state) is tied to the call and
	/// falls in the en-route or on-scene bucket, plus first arrival.
	///
	/// The classification mirrors the chatbot's CallRespondersActionHandler — default
	/// status ids first, then custom-state base types — so both surfaces agree. The
	/// distinction that matters: the call-scoped queries return HISTORY (every state
	/// change during the call); a responder is "current" only when their latest
	/// department-wide status is still tied to this call (the same intersection the
	/// chatbot and the old Core dashboard perform).
	/// </summary>
	public class CallRespondingService : ICallRespondingService
	{
		private readonly IActionLogsService _actionLogsService;
		private readonly IUnitsService _unitsService;
		private readonly ICustomStateService _customStateService;
		private readonly IUserStateService _userStateService;
		private readonly IUsersService _usersService;
		private readonly IDepartmentGroupsService _departmentGroupsService;

		public CallRespondingService(
			IActionLogsService actionLogsService,
			IUnitsService unitsService,
			ICustomStateService customStateService,
			IUserStateService userStateService,
			IUsersService usersService,
			IDepartmentGroupsService departmentGroupsService)
		{
			_actionLogsService = actionLogsService;
			_unitsService = unitsService;
			_customStateService = customStateService;
			_userStateService = userStateService;
			_usersService = usersService;
			_departmentGroupsService = departmentGroupsService;
		}

		public async Task<CallRespondingSnapshot> GetRespondingForCallAsync(int departmentId, int callId)
		{
			var snapshots = await GetRespondingForCallsAsync(departmentId, new[] { callId });
			return snapshots.Count > 0 ? snapshots[0] : new CallRespondingSnapshot { CallId = callId };
		}

		public async Task<List<CallRespondingSnapshot>> GetRespondingForCallsAsync(int departmentId,
			IEnumerable<int> callIds)
		{
			var ids = (callIds ?? Enumerable.Empty<int>()).Distinct().ToList();
			var results = new List<CallRespondingSnapshot>();
			if (ids.Count == 0)
				return results;

			// The department-wide "current" lists are fetched once for the whole
			// batch: the dashboard asks for every active call, and re-fetching
			// these per call is the expensive part (GetLastActionLogsForDepartment
			// bypasses its cache by design).
			var currentLogs = await _actionLogsService.GetLastActionLogsForDepartmentAsync(departmentId)
				?? new List<ActionLog>();
			var currentUnitStates = await _unitsService.GetAllLatestStatusForUnitsByDepartmentIdAsync(departmentId)
				?? new List<UnitState>();
			var users = await _usersService.GetUserGroupAndRolesByDepartmentIdAsync(departmentId, true, true, true)
				?? new List<UserGroupRole>();
			var staffingStates = await _userStateService.GetLatestStatesForDepartmentAsync(departmentId)
				?? new List<UserState>();
			var units = await _unitsService.GetUnitsForDepartmentAsync(departmentId) ?? new List<Unit>();
			var groups = await _departmentGroupsService.GetAllGroupsForDepartmentAsync(departmentId)
				?? new List<DepartmentGroup>();

			foreach (var callId in ids)
			{
				var snapshot = new CallRespondingSnapshot { CallId = callId };

				// One fetch per call-scoped source; the history feeds both the
				// current-status lists and the first-arrival calculation.
				var callLogs = await _actionLogsService.GetActionLogsForCallAsync(departmentId, callId)
					?? new List<ActionLog>();
				var callStates = await _unitsService.GetUnitStatesForCallAsync(departmentId, callId)
					?? new List<UnitState>();

				await AddPersonnelAsync(snapshot, departmentId, callId, callLogs, currentLogs, users,
					staffingStates);
				await AddUnitsAsync(snapshot, departmentId, callId, callStates, currentUnitStates, units, groups);
				await SetFirstArrivalAsync(snapshot, departmentId, callLogs, callStates);

				results.Add(snapshot);
			}

			return results;
		}

		private async Task AddPersonnelAsync(CallRespondingSnapshot snapshot, int departmentId, int callId,
			List<ActionLog> callLogs, List<ActionLog> currentLogs, List<UserGroupRole> users,
			List<UserState> staffingStates)
		{
			if (callLogs.Count == 0)
				return;

			var currentIds = new HashSet<int>(currentLogs
				.Where(x => x.DestinationId == callId)
				.Select(x => x.ActionLogId));

			var responding = callLogs.Where(x => currentIds.Contains(x.ActionLogId)).ToList();
			if (responding.Count == 0)
				return;

			foreach (var log in responding)
			{
				var bucket = await ClassifyPersonnelAsync(departmentId, log);
				if (bucket == null)
					continue;

				var status = await _customStateService.GetCustomPersonnelStatusAsync(departmentId, log);
				var user = users.FirstOrDefault(x => x.UserId == log.UserId);
				var staffingState = staffingStates.FirstOrDefault(x => x.UserId == log.UserId);
				var staffing = staffingState != null
					? await _customStateService.GetCustomPersonnelStaffingAsync(departmentId, staffingState)
					: null;

				// Eta/EtaPulledOn are filled by GetLastActionLogsForDepartmentAsync for
				// call-tied statuses; -1 means no route is known.
				DateTime? etaAt = null;
				if (log.Eta > 0 && log.EtaPulledOn.HasValue)
					etaAt = log.EtaPulledOn.Value.AddSeconds(log.Eta);

				snapshot.Personnel.Add(new CallRespondingPerson
				{
					UserId = log.UserId,
					Name = user?.Name ?? "Unknown User",
					GroupId = user?.DepartmentGroupId,
					Group = user?.DepartmentGroupName,
					StatusId = log.ActionTypeId,
					StatusText = status?.ButtonText ?? "Unknown",
					StatusColor = status?.ButtonColor ?? "#ffa500",
					Timestamp = log.Timestamp,
					Bucket = bucket,
					EtaAt = etaAt,
					StaffingText = staffing?.ButtonText,
					StaffingColor = staffing?.ButtonColor
				});
			}

			snapshot.Personnel = snapshot.Personnel.OrderBy(x => x.Timestamp).ToList();
		}

		private async Task AddUnitsAsync(CallRespondingSnapshot snapshot, int departmentId, int callId,
			List<UnitState> callStates, List<UnitState> currentUnitStates, List<Unit> units,
			List<DepartmentGroup> groups)
		{
			if (callStates.Count == 0)
				return;

			var currentIds = new HashSet<int>(currentUnitStates
				.Where(x => x.DestinationId == callId)
				.Select(x => x.UnitStateId));

			var responding = callStates.Where(x => currentIds.Contains(x.UnitStateId)).ToList();
			if (responding.Count == 0)
				return;

			foreach (var state in responding)
			{
				var bucket = await ClassifyUnitAsync(departmentId, state.State);
				if (bucket == null)
					continue;

				// The call-scoped query does not populate Unit; custom-state resolution
				// needs it, and so does the name/group below.
				var unit = state.Unit ?? units.FirstOrDefault(x => x.UnitId == state.UnitId);
				if (state.Unit == null && unit != null)
					state.Unit = unit;

				var status = await _customStateService.GetCustomUnitStateAsync(state);
				var group = unit?.StationGroupId.HasValue == true
					? groups.FirstOrDefault(g => g.DepartmentGroupId == unit.StationGroupId.Value)
					: null;

				snapshot.Units.Add(new CallRespondingUnit
				{
					UnitId = state.UnitId,
					Name = unit?.Name ?? "Unknown Unit",
					Type = unit?.Type,
					GroupId = unit?.StationGroupId,
					Group = group?.Name,
					StateId = state.State,
					StateText = status?.ButtonText ?? "Unknown",
					StateColor = status?.ButtonColor ?? "#ffa500",
					Timestamp = state.Timestamp,
					Bucket = bucket
				});
			}

			snapshot.Units = snapshot.Units.OrderBy(x => x.Timestamp).ToList();
		}

		private async Task SetFirstArrivalAsync(CallRespondingSnapshot snapshot, int departmentId,
			List<ActionLog> callLogs, List<UnitState> callStates)
		{
			// First arrival is a fact about the call's history, not about the current
			// status list: a unit that arrived and then returned still arrived.
			var arrivals = new List<DateTime>();

			foreach (var log in callLogs)
			{
				if (await ClassifyPersonnelAsync(departmentId, log) == CallRespondingSnapshot.BucketOnScene)
					arrivals.Add(log.Timestamp);
			}

			foreach (var state in callStates)
			{
				if (await ClassifyUnitAsync(departmentId, state.State) == CallRespondingSnapshot.BucketOnScene)
					arrivals.Add(state.Timestamp);
			}

			if (arrivals.Count > 0)
			{
				snapshot.FirstArrivalAt = arrivals.Min();
				return;
			}

			var etas = snapshot.Personnel
				.Where(x => x.EtaAt.HasValue)
				.Select(x => x.EtaAt.Value)
				.ToList();
			if (etas.Count > 0)
				snapshot.FirstArrivalEta = etas.Min();
		}

		private async Task<string> ClassifyPersonnelAsync(int departmentId, ActionLog log)
		{
			if (log == null)
				return null;

			if (log.ActionTypeId <= 25)
				return ClassifyPersonnelDefault(log.ActionTypeId);

			var detail = await _customStateService.GetCustomDetailForDepartmentAsync(departmentId, log.ActionTypeId);
			return BucketForBaseType(detail?.BaseType);
		}

		private async Task<string> ClassifyUnitAsync(int departmentId, int state)
		{
			if (state <= 25)
				return ClassifyUnitDefault(state);

			var detail = await _customStateService.GetCustomDetailForDepartmentAsync(departmentId, state);
			return BucketForBaseType(detail?.BaseType);
		}

		private static string ClassifyPersonnelDefault(int actionTypeId)
		{
			switch ((ActionTypes)actionTypeId)
			{
				case ActionTypes.Responding:
				case ActionTypes.RespondingToScene:
				case ActionTypes.RespondingToStation:
					return CallRespondingSnapshot.BucketEnRoute;
				case ActionTypes.OnScene:
					return CallRespondingSnapshot.BucketOnScene;
				default:
					return null;
			}
		}

		private static string ClassifyUnitDefault(int state)
		{
			switch ((UnitStateTypes)state)
			{
				case UnitStateTypes.Responding:
				case UnitStateTypes.Enroute:
					return CallRespondingSnapshot.BucketEnRoute;
				case UnitStateTypes.OnScene:
				case UnitStateTypes.Staging:
					return CallRespondingSnapshot.BucketOnScene;
				default:
					return null;
			}
		}

		private static string BucketForBaseType(int? baseType)
		{
			if (!baseType.HasValue)
				return null;

			switch ((ActionBaseTypes)baseType.Value)
			{
				case ActionBaseTypes.Responding:
				case ActionBaseTypes.Enroute:
				case ActionBaseTypes.Transporting:
					return CallRespondingSnapshot.BucketEnRoute;
				case ActionBaseTypes.OnScene:
				case ActionBaseTypes.MadeContact:
				case ActionBaseTypes.AtPatient:
				case ActionBaseTypes.Staging:
				case ActionBaseTypes.Searching:
					return CallRespondingSnapshot.BucketOnScene;
				default:
					return null;
			}
		}
	}
}
