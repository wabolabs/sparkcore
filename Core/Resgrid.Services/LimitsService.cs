using System.Threading.Tasks;
using Resgrid.Model;
using Resgrid.Model.Services;

namespace Resgrid.Services
{
	// SparkOps Core: all departments treated as unlimited. No plan-based limit checks.
	public class LimitsService : ILimitsService
	{
		public Task<bool> ValidateDepartmentIsWithinLimitsAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<bool> CanDepartmentAddNewUserAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<bool> CanDepartmentAddNewGroup(int departmentId)
			=> Task.FromResult(true);

		public bool CanDepartmentAddNewRole(int departmentId)
			=> true;

		public Task<bool> CanDepartmentAddNewUnit(int departmentId)
			=> Task.FromResult(true);

		public Task<int> GetPersonnelLimitForDepartmentAsync(int departmentId, Plan plan = null)
			=> Task.FromResult(int.MaxValue);

		public Task<int> GetGroupsLimitForDepartmentAsync(int departmentId, Plan plan = null)
			=> Task.FromResult(int.MaxValue);

		public int GetRolesLimitForDepartment(int departmentId)
			=> int.MaxValue;

		public Task<int> GetUnitsLimitForDepartmentAsync(int departmentId, Plan plan = null)
			=> Task.FromResult(int.MaxValue);

		public Task<bool> CanDepartmentProvisionNumberAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<bool> CanDepartmentUseVoiceAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<bool> CanDepartmentUseLinksAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<bool> CanDepartmentCreateOrdersAsync(int departmentId)
			=> Task.FromResult(true);

		public Task<DepartmentLimits> GetLimitsForEntityPlanWithFallbackAsync(int departmentId, bool bypassCache = false)
		{
			var limits = new DepartmentLimits
			{
				PersonnelLimit = int.MaxValue,
				UnitsLimit = int.MaxValue,
				EntityTotal = 0,
				IsEntityPlan = false,
			};
			return Task.FromResult(limits);
		}

		public Task<bool> InvalidateDepartmentsEntityLimitsCache(int departmentId)
			=> Task.FromResult(true);
	}
}
