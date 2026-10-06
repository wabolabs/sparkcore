using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Resgrid.Config;
using Resgrid.Web.Tts.Configuration;
using Resgrid.Web.Tts.Health;

namespace Resgrid.Tests.Web.Tts
{
	/// <summary>
	/// SparkOps fork: a filesystem-storage deployment has no S3 credentials, and the
	/// dependency check reported it unhealthy for that alone.
	/// </summary>
	[TestFixture]
	[NonParallelizable]
	public class TtsDependencyHealthCheckTests
	{
		private string _originalStorageMode;
		private string _originalRedis;

		[SetUp]
		public void SetUp()
		{
			_originalStorageMode = TtsConfig.StorageMode;
			_originalRedis = CacheConfig.RedisConnectionString;
			CacheConfig.RedisConnectionString = "127.0.0.1:6379";
		}

		[TearDown]
		public void TearDown()
		{
			TtsConfig.StorageMode = _originalStorageMode;
			CacheConfig.RedisConnectionString = _originalRedis;
		}

		private static TtsDependencyHealthCheck CreateCheck() =>
			new TtsDependencyHealthCheck(
				Options.Create(new S3StorageOptions()),
				Options.Create(new TtsOptions { TempDirectory = Path.Combine(Path.GetTempPath(), "tts-health-test") }),
				NullLogger<TtsDependencyHealthCheck>.Instance);

		[Test]
		public async Task filesystem_storage_should_not_require_s3_settings()
		{
			TtsConfig.StorageMode = "filesystem";

			var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

			result.Status.Should().Be(HealthStatus.Healthy);
		}

		[Test]
		public async Task s3_storage_should_still_require_s3_settings()
		{
			TtsConfig.StorageMode = "s3";

			var result = await CreateCheck().CheckHealthAsync(new HealthCheckContext());

			result.Status.Should().Be(HealthStatus.Unhealthy);
			result.Description.Should().Contain("S3 access key is not configured.");
		}
	}
}
