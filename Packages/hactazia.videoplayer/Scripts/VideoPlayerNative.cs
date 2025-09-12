using System;
using System.Runtime.InteropServices;

namespace Hactazia.VideoPlayer {
	// Delegates pour les callbacks
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void VideoFrameCallback(IntPtr frame);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void AudioFrameCallback(IntPtr frame);

	/// <summary>
	/// Interface native pour la DLL hactazia_videoplayer
	/// Cette classe fait le pont entre Unity (C#) et la DLL native (C++)
	/// </summary>
	public class VideoPlayerNative {
		#if UNITY_STANDALONE_WIN
		private const string DLLName = "hactazia_videoplayer";
		#elif UNITY_STANDALONE_OSX
            private const string DLLName = "hactazia_videoplayer";
		#else
		private const string DLLName = "hactazia_videoplayer";
		#endif

		// ========================================
		// Fonctions de gestion du cycle de vie
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateVideoPlayer")]
		private static extern IntPtr Impl_CreateVideoPlayer();

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DestroyVideoPlayer")]
		private static extern void Impl_DestroyVideoPlayer(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "LoadVideo")]
		private static extern bool Impl_LoadVideo(IntPtr player, [MarshalAs(UnmanagedType.LPStr)] string url);

		// ========================================
		// Fonctions de contrôle de lecture
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Play")]
		private static extern void Impl_Play(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Pause")]
		private static extern void Impl_Pause(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Resume")]
		private static extern void Impl_Resume(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Stop")]
		private static extern void Impl_Stop(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Seek")]
		private static extern void Impl_Seek(IntPtr player, double time);

		// ========================================
		// Fonctions d'accès aux données
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetVideoFrame")]
		private static extern IntPtr Impl_GetVideoFrame(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetVideoFrameAtTime")]
		private static extern IntPtr Impl_GetVideoFrameAtTime(IntPtr player, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetAudioFrameAtTime")]
		private static extern IntPtr Impl_GetAudioFrameAtTime(IntPtr player, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeVideoFrame")]
		private static extern void Impl_FreeVideoFrame(IntPtr frame);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeAudioFrame")]
		private static extern void Impl_FreeAudioFrame(IntPtr frame);

		// ========================================
		// Fonctions d'information sur le média
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetDuration")]
		private static extern double Impl_GetDuration(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetCurrentTime")]
		private static extern double Impl_GetCurrentTime(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetVideoWidth")]
		private static extern int Impl_GetVideoWidth(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetVideoHeight")]
		private static extern int Impl_GetVideoHeight(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFrameRate")]
		private static extern double Impl_GetFrameRate(IntPtr player);

		// ========================================
		// Fonctions de callbacks et mise à jour
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetVideoFrameCallback")]
		private static extern void Impl_SetVideoFrameCallback(IntPtr player, VideoFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetAudioFrameCallback")]
		private static extern void Impl_SetAudioFrameCallback(IntPtr player, AudioFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UpdatePlayer")]
		private static extern void Impl_UpdatePlayer(IntPtr player);

		// ========================================
		// Fonctions d'état et gestion d'erreurs
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetPlayerState")]
		private static extern int Impl_GetPlayerState(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetPlayerError")]
		private static extern int Impl_GetPlayerError(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "GetPlayerErrorMessage")]
		[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern string Impl_GetPlayerErrorMessage(IntPtr player);

		// ========================================
		// Cache information functions
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetVideoCacheSize")]
		private static extern int Impl_GetVideoCacheSize(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetAudioCacheSize")]
		private static extern int Impl_GetAudioCacheSize(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetLastVideoCacheTime")]
		private static extern double Impl_GetLastVideoCacheTime(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetLastAudioCacheTime")]
		private static extern double Impl_GetLastAudioCacheTime(IntPtr player);

		// ========================================
		// FFmpeg debug information
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "GetFFMPEGDetails")]
		[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern string Impl_GetFFMPEGDetails(IntPtr player);


		public static IntPtr CreateVideoPlayer()
			=> Impl_CreateVideoPlayer();

		public static bool IsValid(IntPtr player)
			=> player != InvalidPlayer;

		public static IntPtr InvalidPlayer
			=> IntPtr.Zero;

		public static void DestroyVideoPlayer(IntPtr player) {
			if (!IsValid(player)) return;
			Impl_DestroyVideoPlayer(player);
		}

		public static bool LoadVideo(IntPtr player, string url) {
			if (!IsValid(player) || string.IsNullOrEmpty(url)) return false;
			return Impl_LoadVideo(player, url);
		}

		public static void Play(IntPtr player) {
			if (!IsValid(player)) return;
			Impl_Play(player);
		}

		public static void Pause(IntPtr player) {
			if (!IsValid(player)) return;
			Impl_Pause(player);
		}

		public static void Resume(IntPtr player) {
			if (!IsValid(player)) return;
			Impl_Resume(player);
		}

		public static void Stop(IntPtr player) {
			if (!IsValid(player)) return;
			Impl_Stop(player);
		}

		public static void Seek(IntPtr player, double time) {
			if (!IsValid(player) || time < 0) return;
			Impl_Seek(player, time);
		}

		public static VideoFrame? GetVideoFrame(IntPtr player) {
			if (!IsValid(player)) return null;
			var ptr = Impl_GetVideoFrame(player);
			if (ptr == IntPtr.Zero) return null;
			return Marshal.PtrToStructure<VideoFrame>(ptr);
		}

		public static VideoFrame? GetVideoFrameAtTime(IntPtr player, double time) {
			if (!IsValid(player) || time < 0) return null;
			var ptr = Impl_GetVideoFrameAtTime(player, time);
			if (ptr == IntPtr.Zero) return null;
			return Marshal.PtrToStructure<VideoFrame>(ptr);
		}

		public static AudioFrame? GetAudioFrameAtTime(IntPtr player, double time) {
			if (!IsValid(player) || time < 0) return null;
			var ptr = Impl_GetAudioFrameAtTime(player, time);
			if (ptr == IntPtr.Zero) return null;
			return Marshal.PtrToStructure<AudioFrame>(ptr);
		}

		public static double GetDuration(IntPtr player) {
			if (!IsValid(player)) return 0.0;
			return Impl_GetDuration(player);
		}

		public static double GetCurrentTime(IntPtr player) {
			if (!IsValid(player)) return 0.0;
			return Impl_GetCurrentTime(player);
		}

		public static int GetVideoWidth(IntPtr player) {
			if (!IsValid(player)) return 0;
			return Impl_GetVideoWidth(player);
		}

		public static int GetVideoHeight(IntPtr player) {
			if (!IsValid(player)) return 0;
			return Impl_GetVideoHeight(player);
		}

		public static double GetFrameRate(IntPtr player) {
			if (!IsValid(player)) return 0.0;
			return Impl_GetFrameRate(player);
		}
		
		public static PlayerState GetPlayerState(IntPtr player) {
			if (!IsValid(player)) return PlayerState.Uninitialized;
			return (PlayerState)Impl_GetPlayerState(player);
		}

		public static PlayerError GetPlayerError(IntPtr player) {
			if (!IsValid(player)) return PlayerError.None;
			return (PlayerError)Impl_GetPlayerError(player);
		}

		public static string GetPlayerErrorMessage(IntPtr player) {
			if (!IsValid(player)) return "";
			return Impl_GetPlayerErrorMessage(player);
		}

		// ========================================
		// Cache information methods
		// ========================================

		public static int GetVideoCacheSize(IntPtr player) {
			if (!IsValid(player)) return 0;
			return Impl_GetVideoCacheSize(player);
		}

		public static int GetAudioCacheSize(IntPtr player) {
			if (!IsValid(player)) return 0;
			return Impl_GetAudioCacheSize(player);
		}

		public static double GetLastVideoCacheTime(IntPtr player) {
			if (!IsValid(player)) return -1.0;
			return Impl_GetLastVideoCacheTime(player);
		}

		public static double GetLastAudioCacheTime(IntPtr player) {
			if (!IsValid(player)) return -1.0;
			return Impl_GetLastAudioCacheTime(player);
		}

		// ========================================
		// FFmpeg debug information
		// ========================================

		public static string GetFFMPEGDetails(IntPtr player) {
			if (!IsValid(player)) return "Player not found";
			return Impl_GetFFMPEGDetails(player);
		}
	}
}