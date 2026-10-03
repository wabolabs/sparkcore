using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Resgrid.Model;
using Resgrid.Model.Services;
using Resgrid.Services;

namespace Resgrid.Tests.Services
{
	[TestFixture]
	public class CallRespondingServiceTests
	{
		private Mock<IActionLogsService> _actionLogsService;
		private Mock<IUnitsService> _unitsService;
		private Mock<ICustomStateService> _customStateService;
		private Mock<IUserStateService> _userStateService;
		private Mock<IUsersService> _usersService;
		private Mock<IDepartmentGroupsService> _departmentGroupsService;
		private CallRespondingService _service;

		private const int DepartmentId = 7;
		private const int CallId = 12;

		[SetUp]
		public void SetUp()
		{
			_actionLogsService = new Mock<IActionLogsService>();
			_unitsService = new Mock<IUnitsService>();
			_customStateService = new Mock<ICustomStateService>();
			_userStateService = new Mock<IUserStateService>();
			_usersService = new Mock<IUsersService>();
			_departmentGroupsService = new Mock<IDepartmentGroupsService>();

			_service = new CallRespondingService(
				_actionLogsService.Object,
				_unitsService.Object,
				_customStateService.Object,
				_userStateService.Object,
				_usersService.Object,
				_departmentGroupsService.Object);

			// Sensible empty defaults; individual tests override.
			_actionLogsService
				.Setup(x => x.GetActionLogsForCallAsync(It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync(new List<ActionLog>());
			_actionLogsService
				.Setup(x => x.GetLastActionLogsForDepartmentAsync(It.IsAny<int>(), It.IsAny<bool>(),
					It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<ActionLog>());
			_unitsService
				.Setup(x => x.GetUnitStatesForCallAsync(It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync(new List<UnitState>());
			_unitsService
				.Setup(x => x.GetAllLatestStatusForUnitsByDepartmentIdAsync(It.IsAny<int>()))
				.ReturnsAsync(new List<UnitState>());
			_unitsService
				.Setup(x => x.GetUnitsForDepartmentAsync(It.IsAny<int>()))
				.ReturnsAsync(new List<Unit>());
			_userStateService
				.Setup(x => x.GetLatestStatesForDepartmentAsync(It.IsAny<int>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<UserState>());
			_usersService
				.Setup(x => x.GetUserGroupAndRolesByDepartmentIdAsync(It.IsAny<int>(), It.IsAny<bool>(),
					It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<UserGroupRole>());
			_departmentGroupsService
				.Setup(x => x.GetAllGroupsForDepartmentAsync(It.IsAny<int>()))
				.ReturnsAsync(new List<DepartmentGroup>());
			_customStateService
				.Setup(x => x.GetCustomDetailForDepartmentAsync(It.IsAny<int>(), It.IsAny<int>()))
				.ReturnsAsync((CustomStateDetail)null);
		}

		[Test]
		public async Task GetRespondingForCallAsync_keeps_only_personnel_whose_current_status_is_tied_to_the_call()
		{
			var stillTied = new ActionLog
			{
				ActionLogId = 1, UserId = "user-a", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.Responding, Timestamp = new DateTime(2026, 1, 12, 15, 1, 0, DateTimeKind.Utc),
				DestinationId = CallId, DestinationType = (int)DestinationEntityTypes.Call
			};
			var movedOn = new ActionLog
			{
				ActionLogId = 2, UserId = "user-b", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.Responding, Timestamp = new DateTime(2026, 1, 12, 15, 2, 0, DateTimeKind.Utc),
				DestinationId = CallId, DestinationType = (int)DestinationEntityTypes.Call
			};
			var otherCall = new ActionLog
			{
				ActionLogId = 3, UserId = "user-c", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.OnScene, Timestamp = new DateTime(2026, 1, 12, 15, 3, 0, DateTimeKind.Utc),
				DestinationId = CallId, DestinationType = (int)DestinationEntityTypes.Call
			};

			_actionLogsService
				.Setup(x => x.GetActionLogsForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<ActionLog> { stillTied, movedOn, otherCall });

			// The department-wide "current status per person": user-a is still on
			// this call, user-b has changed to something not tied to it, user-c is
			// now tied to a different call.
			_actionLogsService
				.Setup(x => x.GetLastActionLogsForDepartmentAsync(DepartmentId, It.IsAny<bool>(),
					It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<ActionLog>
				{
					new ActionLog { ActionLogId = 1, UserId = "user-a", DestinationId = CallId },
					new ActionLog { ActionLogId = 9, UserId = "user-b", DestinationId = null },
					new ActionLog { ActionLogId = 3, UserId = "user-c", DestinationId = 99 }
				});

			_usersService
				.Setup(x => x.GetUserGroupAndRolesByDepartmentIdAsync(DepartmentId, true, true, true))
				.ReturnsAsync(new List<UserGroupRole>
				{
					new UserGroupRole
					{
						UserId = "user-a", FirstName = "Tammy", LastName = "Adams",
						DepartmentGroupId = 4, DepartmentGroupName = "Station 1"
					}
				});

			_customStateService
				.Setup(x => x.GetCustomPersonnelStatusAsync(DepartmentId, It.IsAny<ActionLog>()))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "Responding", ButtonColor = "#00aaff" });

			_userStateService
				.Setup(x => x.GetLatestStatesForDepartmentAsync(DepartmentId, It.IsAny<bool>()))
				.ReturnsAsync(new List<UserState>
				{
					new UserState { UserId = "user-a", DepartmentId = DepartmentId, State = (int)UserStateTypes.Available }
				});
			_customStateService
				.Setup(x => x.GetCustomPersonnelStaffingAsync(DepartmentId, It.IsAny<UserState>()))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "Available", ButtonColor = "label-success" });

			var snapshot = await _service.GetRespondingForCallAsync(DepartmentId, CallId);

			Assert.That(snapshot.Personnel, Has.Count.EqualTo(1));
			var person = snapshot.Personnel[0];
			Assert.That(person.UserId, Is.EqualTo("user-a"));
			Assert.That(person.Name, Is.EqualTo("Tammy Adams"));
			Assert.That(person.Group, Is.EqualTo("Station 1"));
			Assert.That(person.Bucket, Is.EqualTo(CallRespondingSnapshot.BucketEnRoute));
			Assert.That(person.StatusText, Is.EqualTo("Responding"));
			Assert.That(person.StaffingText, Is.EqualTo("Available"));
		}

		[Test]
		public async Task GetRespondingForCallAsync_classifies_custom_state_base_types()
		{
			var person = new ActionLog
			{
				ActionLogId = 5, UserId = "user-d", DepartmentId = DepartmentId,
				ActionTypeId = 30, Timestamp = new DateTime(2026, 1, 12, 15, 4, 0, DateTimeKind.Utc),
				DestinationId = CallId, DestinationType = (int)DestinationEntityTypes.Call
			};
			_actionLogsService
				.Setup(x => x.GetActionLogsForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<ActionLog> { person });
			_actionLogsService
				.Setup(x => x.GetLastActionLogsForDepartmentAsync(DepartmentId, It.IsAny<bool>(),
					It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<ActionLog>
				{
					new ActionLog { ActionLogId = 5, UserId = "user-d", DestinationId = CallId }
				});
			_customStateService
				.Setup(x => x.GetCustomDetailForDepartmentAsync(DepartmentId, 30))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "At Patient", BaseType = (int)ActionBaseTypes.AtPatient });
			_customStateService
				.Setup(x => x.GetCustomPersonnelStatusAsync(DepartmentId, It.IsAny<ActionLog>()))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "At Patient", ButtonColor = "#ff0000" });

			var unitState = new UnitState
			{
				UnitStateId = 6, UnitId = 2, State = 40,
				Timestamp = new DateTime(2026, 1, 12, 15, 5, 0, DateTimeKind.Utc),
				DestinationId = CallId, DestinationType = (int)DestinationEntityTypes.Call
			};
			_unitsService
				.Setup(x => x.GetUnitStatesForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<UnitState> { unitState });
			_unitsService
				.Setup(x => x.GetAllLatestStatusForUnitsByDepartmentIdAsync(DepartmentId))
				.ReturnsAsync(new List<UnitState>
				{
					new UnitState { UnitStateId = 6, UnitId = 2, DestinationId = CallId }
				});
			_unitsService
				.Setup(x => x.GetUnitsForDepartmentAsync(DepartmentId))
				.ReturnsAsync(new List<Unit>
				{
					new Unit { UnitId = 2, Name = "Engine 1", DepartmentId = DepartmentId, StationGroupId = 8 }
				});
			_departmentGroupsService
				.Setup(x => x.GetAllGroupsForDepartmentAsync(DepartmentId))
				.ReturnsAsync(new List<DepartmentGroup>
				{
					new DepartmentGroup { DepartmentGroupId = 8, DepartmentId = DepartmentId, Name = "Station 3" }
				});
			_customStateService
				.Setup(x => x.GetCustomDetailForDepartmentAsync(DepartmentId, 40))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "En Route", BaseType = (int)ActionBaseTypes.Enroute });
			_customStateService
				.Setup(x => x.GetCustomUnitStateAsync(It.IsAny<UnitState>()))
				.ReturnsAsync(new CustomStateDetail { ButtonText = "En Route", ButtonColor = "#00aaff" });

			var snapshot = await _service.GetRespondingForCallAsync(DepartmentId, CallId);

			Assert.That(snapshot.Personnel, Has.Count.EqualTo(1));
			Assert.That(snapshot.Personnel[0].Bucket, Is.EqualTo(CallRespondingSnapshot.BucketOnScene));
			Assert.That(snapshot.Units, Has.Count.EqualTo(1));
			Assert.That(snapshot.Units[0].Bucket, Is.EqualTo(CallRespondingSnapshot.BucketEnRoute));
			Assert.That(snapshot.Units[0].Name, Is.EqualTo("Engine 1"));
			Assert.That(snapshot.Units[0].Group, Is.EqualTo("Station 3"));
			Assert.That(snapshot.Units[0].StateText, Is.EqualTo("En Route"));
		}

		[Test]
		public async Task GetRespondingForCallAsync_first_arrival_is_the_earliest_on_scene_in_history()
		{
			var unitArrives = new UnitState
			{
				UnitStateId = 1, UnitId = 2, State = (int)UnitStateTypes.OnScene,
				Timestamp = new DateTime(2026, 1, 12, 15, 10, 0, DateTimeKind.Utc), DestinationId = CallId
			};
			var personArrives = new ActionLog
			{
				ActionLogId = 2, UserId = "user-a", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.OnScene,
				Timestamp = new DateTime(2026, 1, 12, 15, 6, 0, DateTimeKind.Utc), DestinationId = CallId
			};

			_actionLogsService
				.Setup(x => x.GetActionLogsForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<ActionLog> { personArrives });
			_unitsService
				.Setup(x => x.GetUnitStatesForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<UnitState> { unitArrives });

			var snapshot = await _service.GetRespondingForCallAsync(DepartmentId, CallId);

			Assert.That(snapshot.FirstArrivalAt, Is.EqualTo(personArrives.Timestamp));
			Assert.That(snapshot.FirstArrivalEta, Is.Null);
		}

		[Test]
		public async Task GetRespondingForCallAsync_projects_the_earliest_eta_while_nobody_is_on_scene()
		{
			var pulledOn = new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc);
			var slower = new ActionLog
			{
				ActionLogId = 1, UserId = "user-a", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.Responding, Timestamp = pulledOn,
				DestinationId = CallId, Eta = 900, EtaPulledOn = pulledOn
			};
			var faster = new ActionLog
			{
				ActionLogId = 2, UserId = "user-b", DepartmentId = DepartmentId,
				ActionTypeId = (int)ActionTypes.Responding, Timestamp = pulledOn,
				DestinationId = CallId, Eta = 300, EtaPulledOn = pulledOn
			};

			_actionLogsService
				.Setup(x => x.GetActionLogsForCallAsync(DepartmentId, CallId))
				.ReturnsAsync(new List<ActionLog> { slower, faster });
			_actionLogsService
				.Setup(x => x.GetLastActionLogsForDepartmentAsync(DepartmentId, It.IsAny<bool>(),
					It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(new List<ActionLog>
				{
					new ActionLog { ActionLogId = 1, UserId = "user-a", DestinationId = CallId, Eta = 900, EtaPulledOn = pulledOn },
					new ActionLog { ActionLogId = 2, UserId = "user-b", DestinationId = CallId, Eta = 300, EtaPulledOn = pulledOn }
				});

			var snapshot = await _service.GetRespondingForCallAsync(DepartmentId, CallId);

			Assert.That(snapshot.FirstArrivalAt, Is.Null);
			Assert.That(snapshot.FirstArrivalEta, Is.EqualTo(pulledOn.AddSeconds(300)));
			Assert.That(snapshot.Personnel, Has.Count.EqualTo(2));
			Assert.That(snapshot.Personnel[0].EtaAt, Is.EqualTo(pulledOn.AddSeconds(900)));
		}

		[Test]
		public async Task GetRespondingForCallAsync_answers_an_empty_call_with_an_empty_snapshot()
		{
			var snapshot = await _service.GetRespondingForCallAsync(DepartmentId, CallId);

			Assert.That(snapshot.CallId, Is.EqualTo(CallId));
			Assert.That(snapshot.Personnel, Is.Empty);
			Assert.That(snapshot.Units, Is.Empty);
			Assert.That(snapshot.FirstArrivalAt, Is.Null);
			Assert.That(snapshot.FirstArrivalEta, Is.Null);
		}
	}
}
