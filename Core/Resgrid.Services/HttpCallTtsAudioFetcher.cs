using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Resgrid.Model.Services;

namespace Resgrid.Services
{
	/// <summary>
	/// GETs a TTS playback URL. Returns null on any failure — callers treat a
	/// TTS outage as "no announcement", never as an error on the call.
	/// </summary>
	public class HttpCallTtsAudioFetcher : ICallTtsAudioFetcher
	{
		private static readonly HttpClient Http = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15),
		};

		public async Task<byte[]> FetchAsync(Uri url, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (url == null)
				return null;

			try
			{
				return await Http.GetByteArrayAsync(url, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
