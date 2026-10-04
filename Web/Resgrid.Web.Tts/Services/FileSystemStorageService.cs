using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Resgrid.Config;

namespace Resgrid.Web.Tts.Services
{
	/// <summary>
	/// Filesystem storage for self-hosted deployments without S3: the audio
	/// cache lives under <see cref="TtsConfig.FileStoragePath"/> (default: a
	/// temp directory). Selected with <c>TtsConfig.StorageMode = "filesystem"</c>.
	///
	/// The playback URL is the service's own /tts/audio/{hash}.wav — there are
	/// no presigned URLs without S3, and the controller rewrites response.Url
	/// from the request host anyway.
	/// </summary>
	public sealed class FileSystemStorageService : IStorageService
	{
		private readonly string _root;

		public FileSystemStorageService()
		{
			_root = string.IsNullOrWhiteSpace(TtsConfig.FileStoragePath)
				? Path.Combine(Path.GetTempPath(), "resgrid-tts-storage")
				: TtsConfig.FileStoragePath;
			Directory.CreateDirectory(_root);
		}

		private string PathFor(string objectKey)
		{
			var key = (objectKey ?? string.Empty).Replace('\\', '/').TrimStart('/');
			if (key.Contains("..", StringComparison.Ordinal))
				throw new ArgumentException("Object key cannot contain '..'.", nameof(objectKey));

			return Path.Combine(_root, key);
		}

		public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
			=> Task.FromResult(File.Exists(PathFor(objectKey)));

		public async Task UploadAsync(string objectKey, Stream content, string contentType,
			CancellationToken cancellationToken)
		{
			var path = PathFor(objectKey);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _root);

			await using (var file = File.Create(path))
			{
				await content.CopyToAsync(file, cancellationToken);
			}

			// Beside the audio so a restart can still serve the right type.
			await File.WriteAllTextAsync(path + ".contenttype", contentType ?? string.Empty, cancellationToken);
		}

		public async Task<TtsAudioContent?> GetObjectAsync(string objectKey, CancellationToken cancellationToken)
		{
			var path = PathFor(objectKey);
			if (!File.Exists(path))
				return null;

			var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
			var contentType = File.Exists(path + ".contenttype")
				? (await File.ReadAllTextAsync(path + ".contenttype", cancellationToken)).Trim()
				: "audio/wav";

			return new TtsAudioContent(bytes,
				string.IsNullOrWhiteSpace(contentType) ? "audio/wav" : contentType,
				string.Empty,
				File.GetLastWriteTimeUtc(path));
		}

		public Task<Uri> GetObjectUrlAsync(string objectKey, CancellationToken cancellationToken)
		{
			var fileName = Path.GetFileName(PathFor(objectKey));
			var baseUrl = (TtsConfig.PlaybackBaseUrl ?? string.Empty).TrimEnd('/');
			var url = string.IsNullOrWhiteSpace(baseUrl)
				? $"/tts/audio/{fileName}"
				: $"{baseUrl}/tts/audio/{fileName}";

			return Task.FromResult(new Uri(url, UriKind.RelativeOrAbsolute));
		}
	}
}
