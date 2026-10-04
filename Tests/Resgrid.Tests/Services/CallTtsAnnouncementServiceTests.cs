using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using Resgrid.Config;
using Resgrid.Model;
using Resgrid.Model.Providers;
using Resgrid.Model.Services;
using Resgrid.Services;

namespace Resgrid.Tests.Services
{
	[TestFixture]
	public class CallTtsAnnouncementServiceTests
	{
		private const int DepartmentId = 7;
		private string _originalBaseUrl;

		[SetUp]
		public void SetUp()
		{
			_originalBaseUrl = TtsConfig.ServiceBaseUrl;
			TtsConfig.ServiceBaseUrl = "http://tts.test";
		}

		[TearDown]
		public void TearDown()
		{
			TtsConfig.ServiceBaseUrl = _originalBaseUrl;
		}

		#region Text

		[Test]
		public void BuildAnnouncementText_reuses_the_voice_prompt_and_adds_units()
		{
			var call = new Call
			{
				Name = "CHESTPN-Chest Pain",
				NatureOfCall = "Chest Pain",
				Address = "123 Main St",
				DispatchCount = 1,
			};

			var text = CallTtsAnnouncementService.BuildAnnouncementText(
				call, "123 Main St", new[] { "Engine 42", "Rescue 2" });

			Assert.That(text, Does.Contain("New call, CHESTPN-Chest Pain."));
			Assert.That(text, Does.Contain("Nature, Chest Pain."));
			Assert.That(text, Does.Contain("Address, 123 Main St."));
			Assert.That(text, Does.Contain("Priority,"));
			Assert.That(text, Does.Contain("Units, Engine 42, Rescue 2."));
		}

		[Test]
		public void BuildAnnouncementText_omits_units_when_none_are_dispatched()
		{
			var call = new Call { Name = "AFA", NatureOfCall = "AFA", Address = "1 Rd" };

			var text = CallTtsAnnouncementService.BuildAnnouncementText(call, "1 Rd", Array.Empty<string>());

			Assert.That(text, Does.Not.Contain("Units,"));
			Assert.That(text, Does.Contain("Address, 1 Rd."));
		}

		[Test]
		public void BuildAnnouncementText_omits_the_address_when_there_is_none()
		{
			var call = new Call { Name = "AFA", NatureOfCall = "AFA" };

			var text = CallTtsAnnouncementService.BuildAnnouncementText(call, null, new[] { "Engine 42" });

			Assert.That(text, Does.Not.Contain("Address,"));
			Assert.That(text, Does.Not.Contain("Location,"));
			Assert.That(text, Does.Contain("Units, Engine 42."));
		}

		[Test]
		public void BuildAnnouncementText_of_no_call_is_empty()
		{
			Assert.That(CallTtsAnnouncementService.BuildAnnouncementText(null, null, null),
				Is.EqualTo(string.Empty));
		}

		#endregion Text

		#region Orchestration

		private static Call SampleCall()
		{
			return new Call
			{
				CallId = 12,
				DepartmentId = DepartmentId,
				Name = "CHESTPN-Chest Pain",
				NatureOfCall = "Chest Pain",
				Address = "123 Main St",
				ReportingUserId = "user-a",
				DispatchCount = 1,
				UnitDispatches = new List<CallDispatchUnit> { new CallDispatchUnit { UnitId = 5 } }
			};
		}

		private static CallTtsAnnouncementService BuildService()
		{
			return new CallTtsAnnouncementService(
				new Mock<IEventAggregator>().Object,
				new Mock<ILifetimeScope>().Object);
		}

		private static Mock<IFeatureToggleService> Toggles(bool enabled)
		{
			var toggles = new Mock<IFeatureToggleService>();
			toggles.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<int>(),
					It.IsAny<bool>(), It.IsAny<IDictionary<string, string>>()))
				.ReturnsAsync(enabled);
			return toggles;
		}

		private static Mock<IUnitsService> Units()
		{
			var units = new Mock<IUnitsService>();
			units.Setup(x => x.GetUnitsForDepartmentAsync(DepartmentId))
				.ReturnsAsync(new List<Unit> { new Unit { UnitId = 5, Name = "Engine 42" } });
			return units;
		}

		private static Mock<ITtsAudioService> Tts()
		{
			var tts = new Mock<ITtsAudioService>();
			tts.Setup(x => x.GenerateSpeechUrlAsync(It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<int?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new Uri("http://tts.test/tts/audio/abc.wav"));
			return tts;
		}

		private static Mock<ICallTtsAudioFetcher> Fetcher(byte[] audio)
		{
			var fetcher = new Mock<ICallTtsAudioFetcher>();
			fetcher.Setup(x => x.FetchAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(audio);
			return fetcher;
		}

		[Test]
		public async Task AnnounceCallAsync_attaches_the_audio_when_the_department_opted_in()
		{
			var service = BuildService();
			var calls = new Mock<ICallsService>();
			calls.Setup(x => x.SaveCallAttachmentAsync(It.IsAny<CallAttachment>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((CallAttachment attachment, CancellationToken _) => attachment);

			await service.AnnounceCallAsync(SampleCall(), Toggles(true).Object, Units().Object,
				new Mock<IGeoLocationProvider>().Object, Tts().Object, Fetcher(new byte[] { 1, 2, 3 }).Object,
				calls.Object);

			calls.Verify(x => x.SaveCallAttachmentAsync(It.Is<CallAttachment>(attachment =>
				attachment.CallId == 12 &&
				attachment.CallAttachmentType == 1 &&
				attachment.FileName == "call-announcement.wav" &&
				attachment.Data.Length == 3), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Test]
		public async Task AnnounceCallAsync_does_nothing_when_the_toggle_is_off()
		{
			var service = BuildService();
			var calls = new Mock<ICallsService>();
			var tts = Tts();

			await service.AnnounceCallAsync(SampleCall(), Toggles(false).Object, Units().Object,
				new Mock<IGeoLocationProvider>().Object, tts.Object, Fetcher(new byte[] { 1 }).Object,
				calls.Object);

			tts.Verify(x => x.GenerateSpeechUrlAsync(It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
			calls.Verify(x => x.SaveCallAttachmentAsync(It.IsAny<CallAttachment>(), It.IsAny<CancellationToken>()),
				Times.Never);
		}

		[Test]
		public async Task AnnounceCallAsync_does_nothing_when_the_tts_service_is_not_configured()
		{
			TtsConfig.ServiceBaseUrl = "";
			var service = BuildService();
			var calls = new Mock<ICallsService>();
			var tts = Tts();

			await service.AnnounceCallAsync(SampleCall(), Toggles(true).Object, Units().Object,
				new Mock<IGeoLocationProvider>().Object, tts.Object, Fetcher(new byte[] { 1 }).Object,
				calls.Object);

			tts.Verify(x => x.GenerateSpeechUrlAsync(It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
			calls.Verify(x => x.SaveCallAttachmentAsync(It.IsAny<CallAttachment>(), It.IsAny<CancellationToken>()),
				Times.Never);
		}

		[Test]
		public async Task AnnounceCallAsync_does_not_attach_when_the_audio_cannot_be_fetched()
		{
			var service = BuildService();
			var calls = new Mock<ICallsService>();

			await service.AnnounceCallAsync(SampleCall(), Toggles(true).Object, Units().Object,
				new Mock<IGeoLocationProvider>().Object, Tts().Object, Fetcher(null).Object,
				calls.Object);

			calls.Verify(x => x.SaveCallAttachmentAsync(It.IsAny<CallAttachment>(), It.IsAny<CancellationToken>()),
				Times.Never);
		}

		#endregion Orchestration
	}
}
