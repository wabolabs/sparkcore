using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Resgrid.Model;
using Resgrid.Model.Providers;
using Resgrid.Model.Services;
using Resgrid.Services;

namespace Resgrid.Tests.Services
{
	/// <summary>
	/// An install with no billing API configured is self-hosted and unlimited.
	///
	/// SystemBehaviorConfig.BillingApiBaseUrl is documented as "Do not set for Open-Source install",
	/// and SubscriptionsService already falls back to the local free plan when it is blank. The free
	/// plan is a *cap* though — 10 personnel, 10 units (M0027) — so before these tests an
	/// open-source install silently behaved as if it had bought the smallest tier. UsersService and
	/// DepartmentsService read the limit directly and truncate with Take(limit), which is the worst
	/// version of the failure: a 130-member department renders 10 people and reports no error at all.
	/// </summary>
	[TestFixture, NonParallelizable]
	public class OpenSourceUnlimitedTests
	{
		private const int Dept = 61;
		private string _billingUrl;
		private string _billingKey;
		private bool _cacheEnabled;
		private Mock<ISubscriptionsService> _subscriptions;
		private Mock<ICacheProvider> _cache;
		private LimitsService _service;

		[SetUp]
		public void SetUp()
		{
			_billingUrl = Resgrid.Config.SystemBehaviorConfig.BillingApiBaseUrl;
			_billingKey = Resgrid.Config.ApiConfig.BackendInternalApikey;
			_cacheEnabled = Resgrid.Config.SystemBehaviorConfig.CacheEnabled;

			// The self-hosted state: no billing endpoint, no internal API key.
			Resgrid.Config.SystemBehaviorConfig.BillingApiBaseUrl = "";
			Resgrid.Config.ApiConfig.BackendInternalApikey = "";
			Resgrid.Config.SystemBehaviorConfig.CacheEnabled = false;

			_subscriptions = new Mock<ISubscriptionsService>();
			_cache = new Mock<ICacheProvider>();
			_service = new LimitsService(_subscriptions.Object, _cache.Object);
		}

		[TearDown]
		public void TearDown()
		{
			Resgrid.Config.SystemBehaviorConfig.BillingApiBaseUrl = _billingUrl;
			Resgrid.Config.ApiConfig.BackendInternalApikey = _billingKey;
			Resgrid.Config.SystemBehaviorConfig.CacheEnabled = _cacheEnabled;
		}

		[Test]
		public async Task A_large_department_is_within_limits()
		{
			(await _service.ValidateDepartmentIsWithinLimitsAsync(Dept))
				.Should().BeTrue("Highway 58 has 130 members and no billing relationship");
		}

		[Test]
		public async Task The_limits_report_unlimited_rather_than_the_free_plan_cap()
		{
			var limits = await _service.GetLimitsForEntityPlanWithFallbackAsync(Dept);

			limits.PersonnelLimit.Should().Be(int.MaxValue);
			limits.UnitsLimit.Should().Be(int.MaxValue);
		}

		[Test]
		public async Task Adding_personnel_and_units_is_allowed()
		{
			(await _service.CanDepartmentAddNewUserAsync(Dept, bypassCache: true)).Should().BeTrue();
			(await _service.CanDepartmentAddNewUnit(Dept)).Should().BeTrue();
		}

		[Test]
		public async Task Paid_features_are_available_without_a_plan()
		{
			// These four read the plan and refuse on a free tier. With no billing relationship
			// there is no tier to be restricted to, so they must not refuse.
			(await _service.CanDepartmentProvisionNumberAsync(Dept)).Should().BeTrue();
			(await _service.CanDepartmentUseVoiceAsync(Dept)).Should().BeTrue();
			(await _service.CanDepartmentUseLinksAsync(Dept)).Should().BeTrue();
			(await _service.CanDepartmentCreateOrdersAsync(Dept)).Should().BeTrue();
		}

		[Test]
		public async Task The_roster_is_never_truncated_by_a_plan_cap()
		{
			// The failure this guards: UsersService does users.Take(limit.PersonnelLimit), so a cap
			// here silently hides real members instead of erroring.
			var limits = await _service.GetLimitsForEntityPlanWithFallbackAsync(Dept);
			var roster = Enumerable.Range(1, 130).ToList();

			roster.Count.Should().BeLessThanOrEqualTo(limits.PersonnelLimit);
		}

		[Test]
		public async Task No_billing_call_is_made()
		{
			await _service.ValidateDepartmentIsWithinLimitsAsync(Dept);
			await _service.CanDepartmentAddNewUserAsync(Dept, bypassCache: true);

			// An unconfigured install must not reach for a plan or for usage counts at all.
			_subscriptions.Verify(s => s.GetCurrentPlanForDepartmentAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
			_subscriptions.Verify(s => s.GetPlanCountsForDepartmentAsync(It.IsAny<int>()), Times.Never);
		}
	}
}
