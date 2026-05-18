using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Resgrid.Model;
using Resgrid.Model.Billing.Api;
using Resgrid.Model.Providers;
using Resgrid.Model.Repositories;
using Resgrid.Model.Services;

namespace Resgrid.Services
{
	// SparkOps Core: payment processing removed. All plans default to free/unlimited.
	// Stripe and Paddle method stubs return safe no-op defaults so existing callers compile.
	public class SubscriptionsService : ISubscriptionsService
	{
		private static string CacheKey = "CurrentPayment_{0}";
		private const int FreePlanId = 1;

		private readonly IPlansRepository _plansRepository;
		private readonly IPaymentRepository _paymentsRepository;
		private readonly ICacheProvider _cacheProvider;
		private readonly IPlanAddonsRepository _planAddonsRepository;
		private readonly IPaymentAddonsRepository _paymentAddonsRepository;
		private readonly IPaymentProviderEventsRepository _paymentProviderEventsRepository;

		public SubscriptionsService(IPlansRepository plansRepository, IPaymentRepository paymentsRepository,
			ICacheProvider cacheProvider, IDepartmentsRepository departmentsRepository,
			IPlanAddonsRepository planAddonsRepository, IPaymentAddonsRepository paymentAddonsRepository,
			IDepartmentSettingsRepository departmentSettingsRepository,
			IPaymentProviderEventsRepository paymentProviderEventsRepository)
		{
			_plansRepository = plansRepository;
			_paymentsRepository = paymentsRepository;
			_cacheProvider = cacheProvider;
			_planAddonsRepository = planAddonsRepository;
			_paymentAddonsRepository = paymentAddonsRepository;
			_paymentProviderEventsRepository = paymentProviderEventsRepository;
		}

		public async Task<Model.Plan> GetCurrentPlanForDepartmentAsync(int departmentId, bool byPassCache = true)
		{
			return await _plansRepository.GetByIdAsync(FreePlanId);
		}

		public Task<DepartmentPlanCount> GetPlanCountsForDepartmentAsync(int departmentId)
		{
			return Task.FromResult(new DepartmentPlanCount());
		}

		public Task<Payment> GetCurrentPaymentForDepartmentAsync(int departmentId, bool byPassCache = true)
		{
			return Task.FromResult<Payment>(null);
		}

		public Task<Payment> GetPreviousNonFreePaymentForDepartmentAsync(int departmentId, int paymentId)
		{
			return Task.FromResult<Payment>(null);
		}

		public Task<Payment> GetUpcomingPaymentForDepartmentAsync(int departmentId)
		{
			return Task.FromResult<Payment>(null);
		}

		public Task<Payment> GetPaymentByTransactionIdAsync(string transactionId)
		{
			return Task.FromResult<Payment>(null);
		}

		public async Task<Plan> GetPlanByIdAsync(int planId, bool byPassCache = false)
		{
			return await _plansRepository.GetByIdAsync(planId);
		}

		public Task<Plan> GetPlanByExternalIdAsync(string externalId, bool byPassCache = false)
		{
			return Task.FromResult<Plan>(null);
		}

		public Task<Payment> GetPaymentByIdAsync(int paymentId)
		{
			return Task.FromResult<Payment>(null);
		}

		public bool ValidateUserSelectableBuyNowPlan(int planId)
		{
			return false;
		}

		public async Task<Payment> SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
		{
			var saved = await _paymentsRepository.SaveOrUpdateAsync(payment, cancellationToken);
			_cacheProvider.Remove(string.Format(CacheKey, payment.DepartmentId));
			return saved;
		}

		public async Task<Payment> UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
		{
			var saved = await _paymentsRepository.SaveOrUpdateAsync(payment, cancellationToken);
			_cacheProvider.Remove(string.Format(CacheKey, payment.DepartmentId));
			return saved;
		}

		public async Task<Payment> InsertPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
		{
			var saved = await _paymentsRepository.SaveOrUpdateAsync(payment, cancellationToken);
			_cacheProvider.Remove(string.Format(CacheKey, payment.DepartmentId));
			return saved;
		}

		public void ClearCacheForCurrentPayment(int departmentId)
		{
			_cacheProvider.Remove(string.Format(CacheKey, departmentId));
		}

		public Task<List<Payment>> GetAllPaymentsForDepartmentAsync(int departmentId)
		{
			return Task.FromResult(new List<Payment>());
		}

		public Task<List<Payment>> GetAllNonFreePaymentsForDepartmentAsync(int departmentId)
		{
			return Task.FromResult(new List<Payment>());
		}

		public List<int> GetPossibleUpgradesForPlan(int planId)
		{
			return new List<int>();
		}

		public List<int> GetPossibleDowngradesForPlan(int planId)
		{
			return new List<int>();
		}

		public bool IsPlanRestrictedOrFree(int planId)
		{
			return false;
		}

		public Task<double> GetAdjustedUpgradePriceAsync(int paymentId, int planId)
		{
			return Task.FromResult(0.0);
		}

		public Tuple<int, double> CalculateCyclesTillFirstBill(double balance, double cost)
		{
			return new Tuple<int, double>(0, 0);
		}

		public async Task<Payment> CreateFreePlanPaymentAsync(int departmentId, string userId, CancellationToken cancellationToken = default)
		{
			var payment = new Payment
			{
				DepartmentId = departmentId,
				PurchasingUserId = userId,
				PlanId = FreePlanId,
				Method = (int)PaymentMethods.System,
				IsTrial = false,
				IsUpgrade = false,
				PurchaseOn = DateTime.UtcNow,
				TransactionId = "SYSTEM",
				Successful = true,
				Data = string.Empty,
				Description = "SparkOps Core — Self-Hosted Unlimited",
				EffectiveOn = DateTime.UtcNow.AddDays(-1),
				Amount = 0.00,
				EndingOn = DateTime.MaxValue,
			};

			var saved = await _paymentsRepository.SaveOrUpdateAsync(payment, cancellationToken);
			ClearCacheForCurrentPayment(departmentId);
			return saved;
		}

		public Task<List<PaymentAddon>> GetCurrentPaymentAddonsForDepartmentAsync(int departmentId, List<string> planAddonIds)
		{
			return Task.FromResult(new List<PaymentAddon>());
		}

		public Task<List<PlanAddon>> GetAllAddonPlansByTypeAsync(PlanAddonTypes planAddonType)
		{
			return Task.FromResult(new List<PlanAddon>());
		}

		public Task<List<PlanAddon>> GetCurrentPlanAddonsForDepartmentFromStripeAsync(int departmentId)
		{
			return Task.FromResult(new List<PlanAddon>());
		}

		public Task<PlanAddon> GetPTTAddonPlanForDepartmentFromStripeAsync(int departmentId)
		{
			return Task.FromResult<PlanAddon>(null);
		}

		public Task<PlanAddon> GetPlanAddonByExternalIdAsync(string externalId)
		{
			return Task.FromResult<PlanAddon>(null);
		}

		public async Task<PaymentAddon> InsertPaymentAddonAsync(PaymentAddon paymentAddon, CancellationToken cancellationToken = default)
		{
			return await _paymentAddonsRepository.SaveOrUpdateAsync(paymentAddon, cancellationToken);
		}

		public Task<bool> HasActiveSubForDepartmentFromStripeAsync(int departmentId)
		{
			return Task.FromResult(true);
		}

		public Task<PlanAddon> GetPTTAddonForCurrentSubAsync(int departmentId)
		{
			return Task.FromResult<PlanAddon>(null);
		}

		public Task<PlanAddon> GetPlanAddonByIdAsync(string planAddonId)
		{
			return Task.FromResult<PlanAddon>(null);
		}

		public Task<PlanAddon> AddAddonAddedToExistingSub(int departmentId, Plan plan, PlanAddon addon)
		{
			return Task.FromResult<PlanAddon>(null);
		}

		public Task<bool> CancelPlanAddonByTypeFromStripeAsync(int departmentId, int addonType)
		{
			return Task.FromResult(true);
		}

		public Task<GetCanceledPlanFromStripeData> GetCanceledPlanFromStripeAsync(int departmentId)
		{
			return Task.FromResult<GetCanceledPlanFromStripeData>(null);
		}

		public Task<List<PlanAddon>> GetAllAddonPlansAsync()
		{
			return Task.FromResult(new List<PlanAddon>());
		}

		public async Task<PaymentAddon> SavePaymentAddonAsync(PaymentAddon paymentAddon, CancellationToken cancellationToken = default)
		{
			return await _paymentAddonsRepository.SaveOrUpdateAsync(paymentAddon, cancellationToken);
		}

		public bool CanPlanSendMessageSms(int planId)
		{
			return true;
		}

		public bool CanPlanSendCallSms(int planId)
		{
			return true;
		}

		public async Task<PaymentProviderEvent> SavePaymentEventAsync(PaymentProviderEvent providerEvent, CancellationToken cancellationToken = default)
		{
			if (providerEvent == null)
				return null;
			return await _paymentProviderEventsRepository.SaveOrUpdateAsync(providerEvent, cancellationToken);
		}

		// ── Stripe stubs (payment processing removed in SparkOps Core) ──

		public Task<CreateStripeSessionForUpdateData> CreateStripeSessionForUpdate(int departmentId, string stripeCustomerId, string email, string departmentName)
		{
			return Task.FromResult<CreateStripeSessionForUpdateData>(null);
		}

		public Task<GetCanceledPlanFromStripeData> GetActiveStripeSubscriptionAsync(string stripeCustomerId)
		{
			return Task.FromResult<GetCanceledPlanFromStripeData>(null);
		}

		public Task<GetCanceledPlanFromStripeData> GetActivePTTStripeSubscriptionAsync(string stripeCustomerId)
		{
			return Task.FromResult<GetCanceledPlanFromStripeData>(null);
		}

		public Task<bool> ModifyPTTAddonSubscriptionAsync(string stripeCustomerId, long quantity, PlanAddon planAddon)
		{
			return Task.FromResult(false);
		}

		public Task<bool> CancelSubscriptionAsync(string stripeCustomerId)
		{
			return Task.FromResult(false);
		}

		public Task<CreateStripeBillingPortalSessionData> CreateStripeSessionForCustomerPortal(int departmentId, string stripeCustomerId, string customerConfigId, string email, string departmentName)
		{
			return Task.FromResult<CreateStripeBillingPortalSessionData>(null);
		}

		public Task<CreateStripeSessionForUpdateData> CreateStripeSessionForSub(int departmentId, string stripeCustomerId, string stripePlanId, int planId, string email, string departmentName, int count, string discountCode = null)
		{
			return Task.FromResult<CreateStripeSessionForUpdateData>(null);
		}

		public Task<ChangeActiveSubscriptionData> ChangeActiveSubscriptionAsync(string stripeCustomerId, string stripePlanId)
		{
			return Task.FromResult<ChangeActiveSubscriptionData>(null);
		}

		// ── Paddle stubs ──

		public Task<CreatePaddleCheckoutData> CreatePaddleCheckoutForSub(int departmentId, string paddleCustomerId, string paddleProductId, int planId, string email, string departmentName, int count, string discountCode = null)
		{
			return Task.FromResult<CreatePaddleCheckoutData>(null);
		}

		public Task<CreatePaddleCheckoutData> CreatePaddleCheckoutForUpdate(int departmentId, string paddleCustomerId, string email, string departmentName)
		{
			return Task.FromResult<CreatePaddleCheckoutData>(null);
		}

		public Task<GetActivePaddleSubscriptionData> GetActivePaddleSubscriptionAsync(string paddleCustomerId)
		{
			return Task.FromResult<GetActivePaddleSubscriptionData>(null);
		}

		public Task<GetActivePaddleSubscriptionData> GetActivePTTPaddleSubscriptionAsync(string paddleCustomerId)
		{
			return Task.FromResult<GetActivePaddleSubscriptionData>(null);
		}

		public Task<bool> ChangePaddleSubscriptionAsync(string paddleCustomerId, string paddlePriceId)
		{
			return Task.FromResult(false);
		}

		public Task<bool> ModifyPaddlePTTAddonSubscriptionAsync(string paddleCustomerId, long quantity, PlanAddon planAddon)
		{
			return Task.FromResult(false);
		}

		public Task<bool> CancelPaddleSubscriptionAsync(string paddleCustomerId)
		{
			return Task.FromResult(false);
		}
	}
}
