using System.Runtime.InteropServices;
using UnityEngine;
using URuntimePlatform = UnityEngine.RuntimePlatform;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Utils {
	public enum Platform : byte {
		None     = 0,
		Windows  = 1,
		Linux    = 2,
		MacOS    = 3,
		Android  = 4,
		IOS      = 5,
		VisionOS = 6,
	}

	public static class PlatformExtensions {
		public static string GetPlatformName(this Platform platform)
			=> platform switch {
				Platform.Windows  => "windows",
				Platform.Linux    => "linux",
				Platform.MacOS    => "macos",
				Platform.Android  => "android",
				Platform.IOS      => "ios",
				Platform.VisionOS => "visionos",
				_                 => null,
			};

		public static Platform GetPlatformFromName(string name)
			=> name switch {
				"windows"  => Platform.Windows,
				"linux"    => Platform.Linux,
				"macos"    => Platform.MacOS,
				"android"  => Platform.Android,
				"ios"      => Platform.IOS,
				"visionos" => Platform.VisionOS,
				_          => Platform.None,
			};

		public static Platform CurrentPlatform {
			get
			#if UNITY_EDITOR
				=> CurrentTarget.GetPlatform();
			#else
				=> RuntimePlatform;
			#endif
			#if UNITY_EDITOR
			set {
				var target = value.GetBuildTarget();
				if (IsSupported(target)) {
					EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
					Logger.Log($"Switched Unity build target to {target}");
				} else {
					Logger.LogError($"Build target {target} is not supported");
				}
			}
			#endif
		}

		public static Platform GetPlatform(this URuntimePlatform target)
			=> target switch {
				URuntimePlatform.OSXEditor               => Platform.MacOS,
				URuntimePlatform.OSXPlayer               => Platform.MacOS,
				URuntimePlatform.WindowsEditor           => Platform.Windows,
				URuntimePlatform.WindowsPlayer           => Platform.Windows,
				URuntimePlatform.LinuxEditor             => Platform.Linux,
				URuntimePlatform.LinuxPlayer             => Platform.Linux,
				URuntimePlatform.LinuxServer             => Platform.Linux,
				URuntimePlatform.LinuxHeadlessSimulation => Platform.Linux,
				URuntimePlatform.EmbeddedLinuxArm32      => Platform.Linux,
				URuntimePlatform.EmbeddedLinuxArm64      => Platform.Linux,
				URuntimePlatform.EmbeddedLinuxX64        => Platform.Linux,
				URuntimePlatform.IPhonePlayer            => Platform.IOS,
				URuntimePlatform.Android                 => Platform.Android,
				URuntimePlatform.VisionOS                => Platform.VisionOS,
				_                                        => Platform.None
			};

		public static Platform RuntimePlatform
			=> Application.platform.GetPlatform();

		#if UNITY_EDITOR
		public static Platform GetPlatform(this BuildTarget target)
			=> target switch {
				BuildTarget.StandaloneWindows64 => Platform.Windows,
				BuildTarget.StandaloneLinux64   => Platform.Linux,
				BuildTarget.StandaloneOSX       => Platform.MacOS,
				BuildTarget.Android             => Platform.Android,
				BuildTarget.iOS                 => Platform.IOS,
				BuildTarget.VisionOS            => Platform.VisionOS,
				_                               => Platform.None,
			};

		public static BuildTarget GetBuildTarget(this Platform platform)
			=> platform switch {
				Platform.Windows  => BuildTarget.StandaloneWindows64,
				Platform.Linux    => BuildTarget.StandaloneLinux64,
				Platform.MacOS    => BuildTarget.StandaloneOSX,
				Platform.Android  => BuildTarget.Android,
				Platform.IOS      => BuildTarget.iOS,
				Platform.VisionOS => BuildTarget.VisionOS,
				_                 => BuildTarget.NoTarget,
			};

		private static bool IsSupported(this BuildTarget target)
			=> BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target);

		private static BuildTarget CurrentTarget
			=> EditorUserBuildSettings.activeBuildTarget;

		public static bool IsSupported(this Platform platform)
			=> IsSupported(GetBuildTarget(platform));
		#endif
	}
}