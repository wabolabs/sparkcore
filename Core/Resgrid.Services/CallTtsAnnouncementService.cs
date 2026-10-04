using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Resgrid.Config;
using Resgrid.Framework;
using Resgrid.Model;
using Resgrid.Model.Events;
using Resgrid.Model.Providers;
using Resgrid.Model.Services;

namespace Resgrid.Services
{
	/// <summary>
	/// The spoken call alert (A7). On call creation, when the department has the
	/// <see cref="FeatureKey"/> toggle on and the TTS service is configured, the
	/// dispatch announcement — the same prompt the outbound voice calls speak,
	/// plus the responding units — is synthesized and attached to the call as an
	/// audio file. That is the same delivery the last-mile snapshot uses, so
	/// every client that renders call files (station display, Unit, Responder,
	/// the web UI) can play it with no new protocol.
	///
	/// Fire-and-forget by design: call creation must never wait on, or fail
	/// with, speech synthesis.
	/// </summary>
	public class CallTtsAnnouncementService : ICallTtsAnnouncementService
	{
		/// <summary>The per-department opt-in key (feature flags).</summary>
		public const string FeatureKey = FeatureFlagKeys.CallTtsAnnouncement;

		private readonly ILifetimeScope _lifetimeScope;

		public CallTtsAnnouncementService(IEventAggregator eventAggregator, ILifetimeScope lifetimeScope)
		{
			_lifetimeScope = lifetimeScope;

			// AddListener, not AddAsyncListener: call creation publishes through
			// SendMessage (the synchronous hub), which never invokes the async
			// listener set — the same reason CoreEventService uses this pattern.
			// The work is fire-and-forget; each event runs in its own child scope
			// so it does not share a unit of work with the publisher.
			eventAggregator.AddListener<CallAddedEvent>(message => _ = AnnounceFromEventAsync(message));
		}

		private async Task AnnounceFromEventAsync(CallAddedEvent message)
		{
			try
			{
				if (message?.Call == null || string.IsNullOrWhiteSpace(TtsConfig.ServiceBaseUrl))
					return;

				using var scope = _lifetimeScope.BeginLifetimeScope();
				await AnnounceCallAsync(message.Call,
					scope.Resolve<IFeatureToggleService>(),
					scope.Resolve<IUnitsService>(),
					scope.Resolve<IGeoLocationProvider>(),
					scope.Resolve<ITtsAudioService>(),
					scope.Resolve<ICallTtsAudioFetcher>(),
					scope.Resolve<ICallsService>());
			}
			catch (Exception ex)
			{
				// Nothing awaits this, so an escaping exception would go unobserved.
				Logging.LogException(ex, $"Call TTS announcement failed for call {message?.Call?.CallId}.");
			}
		}

		public async Task AnnounceCallAsync(Call call, IFeatureToggleService featureToggles,
			IUnitsService unitsService, IGeoLocationProvider geoLocationProvider,
			ITtsAudioService ttsAudioService, ICallTtsAudioFetcher audioFetcher,
			ICallsService callsService, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (call == null || string.IsNullOrWhiteSpace(TtsConfig.ServiceBaseUrl))
				return;

			try
			{
				if (!await featureToggles.IsEnabledAsync(FeatureKey, call.DepartmentId))
					return;

				var unitNames = new List<string>();
				if (call.UnitDispatches != null && call.UnitDispatches.Any())
				{
					var departmentUnits = await unitsService.GetUnitsForDepartmentAsync(call.DepartmentId)
						?? new List<Unit>();
					unitNames = call.UnitDispatches
						.Select(dispatch => departmentUnits
							.FirstOrDefault(unit => unit.UnitId == dispatch.UnitId)?.Name)
						.Where(name => !string.IsNullOrWhiteSpace(name))
						.ToList();
				}

				var address = await DispatchVoicePromptBuilder.ResolveDispatchAddressAsync(
					call, geoLocationProvider, cancellationToken);
				var text = BuildAnnouncementText(call, address, unitNames);
				if (string.IsNullOrWhiteSpace(text))
					return;

				var url = await ttsAudioService.GenerateSpeechUrlAsync(text,
					cancellationToken: cancellationToken);
				if (url == null)
					return;

				var audio = await audioFetcher.FetchAsync(url, cancellationToken);
				if (audio == null || audio.Length == 0)
					return;

				await callsService.SaveCallAttachmentAsync(new CallAttachment
				{
					CallId = call.CallId,
					CallAttachmentType = 1,   // audio
					FileName = "call-announcement.wav",
					Name = "Call announcement (TTS)",
					Data = audio,
					Size = audio.Length,
					UserId = call.ReportingUserId ?? string.Empty,
					Timestamp = DateTime.UtcNow,
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				// Never let speech take down call creation.
				Logging.LogException(ex, $"Call TTS announcement failed for call {call.CallId}.");
			}
		}

		/// <summary>
		/// The dispatch prompt the voice calls already speak (alarm intro, name,
		/// nature, address, priority), plus the responding units — the one piece
		/// the voice prompt leaves to the dispatcher. Only the fields that exist;
		/// a missing one is absent, not read as "unknown".
		/// </summary>
		public static string BuildAnnouncementText(Call call, string address, IEnumerable<string> unitNames)
		{
			if (call == null)
				return string.Empty;

			var prompt = DispatchVoicePromptBuilder.BuildDispatchPrompt(call, address);

			var units = (unitNames ?? Enumerable.Empty<string>())
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.Select(name => name.Trim())
				.ToList();
			if (units.Count > 0)
				prompt = $"{prompt} Units, {string.Join(", ", units)}.";

			return prompt;
		}
	}
}
