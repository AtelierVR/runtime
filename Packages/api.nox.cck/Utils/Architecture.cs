using System.Runtime.InteropServices;
using UArchitecture = System.Runtime.InteropServices.Architecture;

namespace Nox.CCK.Utils {
	public enum Architecture : byte {
		None  = 0,
		X86   = 1,
		X64   = 2,
		Arm   = 3,
		Arm64 = 4,
	}

	public static class ArchitectureExtensions {
		public static string GetArchitectureName(this Architecture architecture)
			=> architecture switch {
				Architecture.X86   => "x86",
				Architecture.X64   => "x64",
				Architecture.Arm   => "arm",
				Architecture.Arm64 => "arm64",
				_                  => null,
			};
		
		public static Architecture GetArchitectureFromName(string name)
			=> name switch {
				"x86"   => Architecture.X86,
				"x64"   => Architecture.X64,
				"arm"   => Architecture.Arm,
				"arm64" => Architecture.Arm64,
				_       => Architecture.None,
			};
		
		public static Architecture GetArchitecture(this UArchitecture target)
			=> target switch {
				UArchitecture.X86   => Architecture.X86,
				UArchitecture.X64   => Architecture.X64,
				UArchitecture.Arm   => Architecture.Arm,
				UArchitecture.Arm64 => Architecture.Arm64,
				_                   => Architecture.None,
			};
		
		public static Architecture CurrentArchitecture
			=> RuntimeInformation.ProcessArchitecture.GetArchitecture();
	}
}