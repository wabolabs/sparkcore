using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Resgrid.Framework;
using Resgrid.Model;
using Resgrid.Model.Providers;
using Resgrid.Model.Services;
using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;

namespace Resgrid.Providers.NumberProvider
{
	public class SipTrunkOutboundVoiceProvider : IOutboundVoiceProvider
	{
		// Cached raw 16-bit PCM bytes for static prompts (8 kHz mono); populated on first use.
		private static byte[] _promptResponding;
		private static byte[] _promptMarkedResponding;
		private static byte[] _promptVerificationIntro;
		private static byte[] _promptVerificationOutro;
		private static readonly SemaphoreSlim _promptLock = new(1, 1);

		private readonly ITtsAudioService _ttsAudioService;
		private readonly IUserProfileService _userProfileService;
		private readonly IDepartmentsService _departmentsService;
		private readonly IEncryptionService _encryptionService;

		public SipTrunkOutboundVoiceProvider(
			ITtsAudioService ttsAudioService,
			IUserProfileService userProfileService,
			IDepartmentsService departmentsService,
			IEncryptionService encryptionService)
		{
			_ttsAudioService = ttsAudioService;
			_userProfileService = userProfileService;
			_departmentsService = departmentsService;
			_encryptionService = encryptionService;
		}

		// ── IOutboundVoiceProvider ────────────────────────────────────────────────

		public async Task<bool> CommunicateCallAsync(string phoneNumber, UserProfile profile, Call call)
		{
			if (profile == null || !profile.VoiceForCall)
				return false;

			string targetNumber = null;
			if (profile.VoiceCallMobile && !string.IsNullOrWhiteSpace(profile.GetPhoneNumber()))
				targetNumber = profile.GetPhoneNumber();
			else if (profile.VoiceCallHome && !string.IsNullOrWhiteSpace(profile.GetHomePhoneNumber()))
				targetNumber = profile.GetHomePhoneNumber();

			if (string.IsNullOrWhiteSpace(targetNumber))
				return false;

			try
			{
				await EnsureStaticPromptsAsync();

				var dispatchText = BuildDispatchText(call);
				var dispatchPcm  = await FetchTtsAsPcm16Async(dispatchText);
				if (dispatchPcm == null)
					return false;

				// Combine dispatch audio + menu prompt
				var callPcm = CombinePcm16(dispatchPcm, _promptResponding ?? Array.Empty<byte>());

				using var sipTransport = CreateTransport();
				var session = new VoIPMediaSession((string)null, (Func<AudioFormat, bool>)null);
				var ua      = new SIPUserAgent(sipTransport, null);

				// DTMF tap — event is on SIPUserAgent in SIPSorcery 8.x
				var dtmfTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
				ua.OnDtmfTone += (tone, duration) => dtmfTcs.TrySetResult(tone.ToString());

				bool answered = await PlaceCallAsync(ua, targetNumber, session);
				if (!answered)
				{
					sipTransport.Shutdown();
					return false;
				}

				await StreamPcm16Async(session, callPcm);

				// Wait up to 30 s for DTMF "1" (respond to scene)
				var dtmfWinner = await Task.WhenAny(dtmfTcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
				if (dtmfWinner == dtmfTcs.Task
				    && dtmfTcs.Task.Result == "1"
				    && _promptMarkedResponding != null)
				{
					await StreamPcm16Async(session, _promptMarkedResponding);
				}

				ua.Hangup();
				sipTransport.Shutdown();
				return true;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}

		public async Task<bool> SendVoiceVerificationCallAsync(string phoneNumber, string userId, int contactType)
		{
			if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(userId))
				return false;

			try
			{
				var profile = await _userProfileService.GetProfileByUserIdAsync(userId, bypassCache: true);
				if (profile == null)
					return false;

				string encryptedCode;
				DateTime? expiry;
				bool alreadyConsumed;

				if (contactType == (int)ContactVerificationType.MobileNumber)
				{
					encryptedCode   = profile.MobileVerificationCode;
					expiry          = profile.MobileVerificationCodeExpiry;
					alreadyConsumed = profile.MobileVerificationVoiceCodeConsumed;
				}
				else if (contactType == (int)ContactVerificationType.HomeNumber)
				{
					encryptedCode   = profile.HomeVerificationCode;
					expiry          = profile.HomeVerificationCodeExpiry;
					alreadyConsumed = profile.HomeVerificationVoiceCodeConsumed;
				}
				else
				{
					return false;
				}

				if (string.IsNullOrWhiteSpace(encryptedCode) || alreadyConsumed)
					return false;
				if (expiry.HasValue && expiry.Value < DateTime.UtcNow)
					return false;

				string code = _encryptionService.Decrypt(encryptedCode);
				if (string.IsNullOrWhiteSpace(code))
					return false;

				// Mark consumed before placing call to prevent replay
				if (contactType == (int)ContactVerificationType.MobileNumber)
					profile.MobileVerificationVoiceCodeConsumed = true;
				else
					profile.HomeVerificationVoiceCodeConsumed = true;

				var dept = await _departmentsService.GetDepartmentByUserIdAsync(userId);
				if (dept != null)
					await _userProfileService.SaveProfileAsync(dept.DepartmentId, profile);

				await EnsureStaticPromptsAsync();

				// "1, 2, 3, 4, 5, 6." spoken 3× — same repetition as the Twilio endpoint
				var spokenDigits = string.Join(", ", code.Select(c => c.ToString())) + ".";
				var codeText     = $"Your verification code is: {spokenDigits}";
				var fullCodeText = $"{codeText} {codeText} {codeText}";

				var introPcm  = _promptVerificationIntro ?? Array.Empty<byte>();
				var codePcm   = await FetchTtsAsPcm16Async(fullCodeText) ?? Array.Empty<byte>();
				var outroPcm  = _promptVerificationOutro ?? Array.Empty<byte>();

				var verifyPcm = CombinePcm16(introPcm, codePcm, outroPcm);

				using var sipTransport = CreateTransport();
				var session = new VoIPMediaSession((string)null, (Func<AudioFormat, bool>)null);
				var ua      = new SIPUserAgent(sipTransport, null);

				bool answered = await PlaceCallAsync(ua, phoneNumber, session);
				if (!answered)
				{
					sipTransport.Shutdown();
					return false;
				}

				await StreamPcm16Async(session, verifyPcm);

				ua.Hangup();
				sipTransport.Shutdown();
				return true;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}

		// ── SIP helpers ──────────────────────────────────────────────────────────

		private static SIPTransport CreateTransport()
		{
			var transport = new SIPTransport();
			transport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 0)));
			return transport;
		}

		private static async Task<bool> PlaceCallAsync(SIPUserAgent ua, string targetNumber, VoIPMediaSession session)
		{
			string normalized = targetNumber.Trim();
			if (!normalized.StartsWith("+"))
				normalized = normalized.Length == 10 ? $"+1{normalized}" : $"+{normalized}";

			string dest = $"sip:{normalized}@{Config.SipTrunkConfig.SipDomain};user=phone";

			bool answered = await ua.Call(
				dest,
				Config.SipTrunkConfig.SipUsername,
				Config.SipTrunkConfig.SipPassword,
				session);

			if (answered)
				await Task.Delay(500);

			return answered;
		}

		// ── Audio helpers ─────────────────────────────────────────────────────────

		private async Task<byte[]> FetchTtsAsPcm16Async(string text)
		{
			try
			{
				Uri audioUri = await _ttsAudioService.GenerateSpeechUrlAsync(text);
				if (audioUri == null)
					return null;

				using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
				byte[] wavBytes = await http.GetByteArrayAsync(audioUri);
				return ConvertWavToPcm16(wavBytes);
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return null;
			}
		}

		// WAV → 16-bit PCM mono at 8 kHz (the format SendAudioFromStream expects)
		private static byte[] ConvertWavToPcm16(byte[] wavBytes)
		{
			using var ms     = new MemoryStream(wavBytes);
			using var reader = new WaveFileReader(ms);

			ISampleProvider samples = reader.ToSampleProvider();
			if (reader.WaveFormat.Channels > 1)
				samples = new StereoToMonoSampleProvider(samples);

			// WDL resampler is cross-platform (no Windows MediaFoundation required)
			var resampled = new WdlResamplingSampleProvider(samples, 8000);

			var floats = new List<float>();
			var buf    = new float[4096];
			int read;
			while ((read = resampled.Read(buf, 0, buf.Length)) > 0)
				floats.AddRange(buf.Take(read));

			// float → 16-bit PCM little-endian bytes
			var pcm16Bytes = new byte[floats.Count * 2];
			for (int i = 0; i < floats.Count; i++)
			{
				short s = (short)Math.Clamp((int)(floats[i] * 32767f), short.MinValue, short.MaxValue);
				pcm16Bytes[i * 2]     = (byte)(s & 0xFF);
				pcm16Bytes[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
			}
			return pcm16Bytes;
		}

		// Stream raw 16-bit PCM bytes through AudioExtrasSource; awaits completion.
		private static async Task StreamPcm16Async(VoIPMediaSession session, byte[] pcm16Bytes)
		{
			if (pcm16Bytes == null || pcm16Bytes.Length == 0)
				return;

			var completionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			session.AudioExtrasSource.OnSendFromAudioStreamComplete +=
				() => completionTcs.TrySetResult();

			using var stream = new MemoryStream(pcm16Bytes);
			session.AudioExtrasSource.SendAudioFromStream(stream, AudioSamplingRatesEnum.Rate8KHz);

			// Guard: never wait longer than actual audio duration + 3 s buffer
			int durationMs = (pcm16Bytes.Length / 2 / 8000) * 1000 + 3000;
			await Task.WhenAny(completionTcs.Task, Task.Delay(durationMs));
		}

		private static byte[] CombinePcm16(params byte[][] parts) =>
			parts.SelectMany(p => p).ToArray();

		// ── Static prompt cache ───────────────────────────────────────────────────

		private async Task EnsureStaticPromptsAsync()
		{
			if (_promptResponding != null) return;

			await _promptLock.WaitAsync();
			try
			{
				if (_promptResponding != null) return;

				_promptResponding       = await FetchTtsAsPcm16Async(
					"To mark yourself responding to the scene, press 1. Otherwise this call will end in 30 seconds.");
				_promptMarkedResponding = await FetchTtsAsPcm16Async(
					"You have been marked responding to the scene. Goodbye.");
				_promptVerificationIntro = await FetchTtsAsPcm16Async(
					"Hello, this is SparkOps calling with your verification code.");
				_promptVerificationOutro = await FetchTtsAsPcm16Async(
					"That was your verification code. Goodbye.");
			}
			finally
			{
				_promptLock.Release();
			}
		}

		// ── Dispatch text builder ─────────────────────────────────────────────────

		private static string BuildDispatchText(Call call)
		{
			var parts = new List<string>();
			if (!string.IsNullOrWhiteSpace(call.Name))
				parts.Add(call.Name);

			var priority = call.GetPriorityText();
			if (!string.IsNullOrWhiteSpace(priority))
				parts.Add($"Priority {priority}");

			if (!string.IsNullOrWhiteSpace(call.Address))
				parts.Add($"Address {call.Address}");

			if (!string.IsNullOrWhiteSpace(call.NatureOfCall))
				parts.Add($"Nature {call.NatureOfCall}");

			return string.Join(". ", parts) + ".";
		}
	}
}
