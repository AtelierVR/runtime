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

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreateVideoPlayer();

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyVideoPlayer(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LoadVideo(IntPtr player, [MarshalAs(UnmanagedType.LPStr)] string url);

		// ========================================
		// Fonctions de contrôle de lecture
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Play(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Pause(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Resume(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Stop(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Seek(IntPtr player, double time);

		// ========================================
		// Fonctions d'accès aux données
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetVideoFrameAtTime(IntPtr player, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetAudioFrameAtTime(IntPtr player, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FreeVideoFrame(IntPtr frame);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FreeAudioFrame(IntPtr frame);

		// ========================================
		// Fonctions d'information sur le média
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetDuration(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetCurrentTime(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetVideoWidth(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetVideoHeight(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetFrameRate(IntPtr player);

		// ========================================
		// Fonctions de callbacks et mise à jour
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetVideoFrameCallback(IntPtr player, VideoFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetAudioFrameCallback(IntPtr player, AudioFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void UpdatePlayer(IntPtr player);

		// ========================================
		// Fonctions d'état et gestion d'erreurs
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetPlayerState(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetPlayerError(IntPtr player);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.LPStr)]
		public static extern string GetPlayerErrorMessage(IntPtr player);
	}
}