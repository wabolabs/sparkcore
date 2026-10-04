using System;
using System.Threading;
using System.Threading.Tasks;
using Resgrid.Model.Providers;

namespace Resgrid.Model.Services
{
	/// <summary>
	/// Fetches the bytes of a TTS playback URL. Split out so the announcement
	/// flow is testable without a running TTS deployment.
	/// </summary>
	public interface ICallTtsAudioFetcher
	{
		/// <summary>Returns the audio bytes, or null on any failure.</summary>
		Task<byte[]> FetchAsync(Uri url, CancellationToken cancellationToken = default(CancellationToken));
	}

	/// <summary>
	/// The spoken call alert (A7): on call creation, announces the dispatch
	/// (type, address, units) to the station display and any subscribed client.
	///
	/// Reuses the existing dispatch-voice stack: the same prompt the outbound
	/// voice calls speak (<see cref="DispatchVoicePromptBuilder"/>), the same
	/// TTS service (<see cref="ITtsAudioService"/>), and the same delivery the
	/// last-mile snapshot uses — an audio attachment on the call, so every
	/// client that renders call files can play it with no new protocol.
	///
	/// Opt-in per department through the <c>calls.tts_announcement</c> feature
	/// toggle, and a no-op when <c>TtsConfig.ServiceBaseUrl</c> is unset.
	/// </summary>
	public interface ICallTtsAnnouncementService
	{
		/// <summary>
		/// Composes and attaches the announcement to the call. Never throws:
		/// a TTS outage must not affect call creation.
		/// </summary>
		Task AnnounceCallAsync(Resgrid.Model.Call call, IFeatureToggleService featureToggles,
			IUnitsService unitsService, IGeoLocationProvider geoLocationProvider,
			ITtsAudioService ttsAudioService, ICallTtsAudioFetcher audioFetcher,
			ICallsService callsService,
			CancellationToken cancellationToken = default(CancellationToken));
	}
}
