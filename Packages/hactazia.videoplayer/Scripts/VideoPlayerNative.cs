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
		public static extern int CreateVideoPlayer();

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyVideoPlayer(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LoadVideo(int playerId, [MarshalAs(UnmanagedType.LPStr)] string url);

		// ========================================
		// Fonctions de contrôle de lecture
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Play(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Pause(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Resume(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Stop(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Seek(int playerId, double time);

		// ========================================
		// Fonctions d'accès aux données
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetVideoFrameAtTime(int playerId, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetAudioFrameAtTime(int playerId, double time);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FreeVideoFrame(IntPtr frame);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FreeAudioFrame(IntPtr frame);

		// ========================================
		// Fonctions d'information sur le média
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetDuration(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetCurrentTime(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetVideoWidth(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetVideoHeight(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double GetFrameRate(int playerId);

		// ========================================
		// Fonctions de callbacks et mise à jour
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetVideoFrameCallback(int playerId, VideoFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetAudioFrameCallback(int playerId, AudioFrameCallback callback);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void UpdatePlayer(int playerId);

		// ========================================
		// Fonctions d'état et gestion d'erreurs
		// ========================================

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetPlayerState(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetPlayerError(int playerId);

		[DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.LPStr)]
		public static extern string GetPlayerErrorMessage(int playerId);
	}
}