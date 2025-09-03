using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.videoplayer {
	public class M3U8StreamServer : IDisposable {
		private readonly string                  _videoUrl;
		private readonly string                  _audioUrl;
		private readonly int                     _port;
		private readonly string                  _outputDirectory;
		private          HttpListener            _httpListener;
		private          CancellationTokenSource _cancellationTokenSource;
		private          bool                    _isRunning;
		private          bool                    _disposed;

		public bool IsRunning
			=> _isRunning && !_disposed;

		public string PlaylistUrl
			=> $"http://127.0.0.1:{_port}/playlist.m3u8";

		public int Port
			=> _port;

		public int GetRandomPort() {
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}

		public M3U8StreamServer(string videoUrl, string audioUrl, int port = 0) {
			_videoUrl        = videoUrl ?? throw new ArgumentNullException(nameof(videoUrl));
			_audioUrl        = audioUrl ?? throw new ArgumentNullException(nameof(audioUrl));
			_port            = port > 0 ? port : GetRandomPort();
			_outputDirectory = Path.Combine(Path.GetTempPath(), $"m3u8_stream_{Guid.NewGuid():N}");
		}

		public async UniTask StartAsync(CancellationToken cancellationToken = default) {
			if (_disposed)
				throw new ObjectDisposedException(nameof(M3U8StreamServer));

			if (_isRunning)
				throw new InvalidOperationException("Server is already running");

			if (!FFmpeg.IsAvailable) {
				throw new InvalidOperationException("FFmpeg is not available. Please ensure FFmpeg is downloaded and ready.");
			}

			_cancellationTokenSource = new CancellationTokenSource();
			var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;

			try {
				// Create output directory
				if (!Directory.Exists(_outputDirectory))
					Directory.CreateDirectory(_outputDirectory);

				// Start FFmpeg process to generate HLS stream
				var ffmpegTask = StartFFmpegProcessAsync(combinedToken);

				// Start HTTP server
				var serverTask = StartHttpServerAsync(combinedToken);

				// Wait a bit for FFmpeg to start generating segments
				await UniTask.Delay(2000, cancellationToken: combinedToken);

				_isRunning = true;

				Logger.Log($"M3U8 Stream Server started on port {_port}");
				Logger.Log($"Playlist URL: {PlaylistUrl}");

				// Run both tasks in background
				ffmpegTask.Forget();
				serverTask.Forget();
			} catch (Exception ex) {
				Stop();
				throw new InvalidOperationException($"Failed to start M3U8 stream server: {ex.Message}", ex);
			}
		}

		public void Stop() {
			if (!_isRunning || _disposed)
				return;

			Logger.Log("Stopping M3U8 Stream Server...");

			_isRunning = false;

			// Cancel all operations
			_cancellationTokenSource?.Cancel();

			// Stop HTTP listener
			try {
				_httpListener?.Stop();
				_httpListener?.Close();
			} catch (Exception ex) {
				Logger.LogWarning($"Error stopping HTTP listener: {ex.Message}");
			}

			// Clean up output directory
			try {
				if (Directory.Exists(_outputDirectory))
					Directory.Delete(_outputDirectory, true);
			} catch (Exception ex) {
				Logger.LogWarning($"Error cleaning up output directory: {ex.Message}");
			}

			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;

			Logger.Log("M3U8 Stream Server stopped");
		}

		private async UniTask StartFFmpegProcessAsync(CancellationToken cancellationToken) {
			var playlistPath   = Path.Combine(_outputDirectory, "playlist.m3u8");
			var segmentPattern = Path.Combine(_outputDirectory, "segment_%03d.ts");

			// FFmpeg command to combine video and audio and create HLS stream
			var arguments = new StringBuilder();
			arguments.Append($"-i \"{_videoUrl}\" ");
			arguments.Append($"-i \"{_audioUrl}\" ");
			arguments.Append("-c:v libx264 ");
			arguments.Append("-c:a aac ");
			arguments.Append("-preset ultrafast ");
			arguments.Append("-tune zerolatency ");
			arguments.Append("-f hls ");
			arguments.Append("-hls_time 6 ");
			arguments.Append("-hls_list_size 10 ");
			arguments.Append("-hls_flags delete_segments ");
			arguments.Append($"-hls_segment_filename \"{segmentPattern}\" ");
			arguments.Append($"\"{playlistPath}\"");

			try {
				Logger.LogDebug($"Starting FFmpeg with arguments: {arguments}");
				await FFmpeg.RunCommand(arguments.ToString(), cancellationToken);
			} catch (OperationCanceledException) {
				Logger.LogDebug("FFmpeg process was cancelled");
			} catch (Exception ex) {
				Logger.LogError($"FFmpeg process failed: {ex.Message}");
				throw;
			}
		}

		private async UniTask StartHttpServerAsync(CancellationToken cancellationToken) {
			_httpListener = new HttpListener();
			_httpListener.Prefixes.Add($"http://localhost:{_port}/");

			try {
				_httpListener.Start();
				Logger.LogDebug($"HTTP server listening on port {_port}");

				while (!cancellationToken.IsCancellationRequested && _httpListener.IsListening) {
					try {
						var context = await _httpListener.GetContextAsync()
							.AsUniTask()
							.AttachExternalCancellation(cancellationToken);
						HandleHttpRequestAsync(context, cancellationToken).Forget();
					} catch (OperationCanceledException) {
						break;
					} catch (Exception ex) {
						Logger.LogError($"Error handling HTTP request: {ex.Message}");
					}
				}
			} catch (Exception ex) {
				Logger.LogError($"HTTP server error: {ex.Message}");
				throw;
			} finally {
				_httpListener?.Stop();
			}
		}

		private async UniTask HandleHttpRequestAsync(HttpListenerContext context, CancellationToken cancellationToken) {
			var request  = context.Request;
			var response = context.Response;

			try {
				var requestPath = request.Url.AbsolutePath.TrimStart('/');
				var filePath    = Path.Combine(_outputDirectory, requestPath);

				Logger.LogDebug($"HTTP request: {requestPath}");

				if (File.Exists(filePath)) {
					var fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);

					// Set appropriate content type
					if (requestPath.EndsWith(".m3u8")) {
						response.ContentType = "application/vnd.apple.mpegurl";
					} else if (requestPath.EndsWith(".ts")) {
						response.ContentType = "video/mp2t";
					} else {
						response.ContentType = "application/octet-stream";
					}

					response.ContentLength64 = fileContent.Length;
					response.StatusCode      = 200;

					await response.OutputStream.WriteAsync(fileContent, 0, fileContent.Length, cancellationToken);
				} else {
					response.StatusCode = 404;
					var errorMessage = Encoding.UTF8.GetBytes("File not found");
					response.ContentLength64 = errorMessage.Length;
					await response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length, cancellationToken);
				}
			} catch (Exception ex) {
				Logger.LogError($"Error handling request: {ex.Message}");
				response.StatusCode = 500;
				var errorMessage = Encoding.UTF8.GetBytes("Internal server error");
				response.ContentLength64 = errorMessage.Length;
				await response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length, cancellationToken);
			} finally {
				response.Close();
			}
		}

		public void Dispose() {
			if (_disposed)
				return;
			Stop();
			_disposed = true;
		}
	}
}