using UnityEngine;

namespace Nox.Avatars.Parameters {
	public enum ParameterType : byte {
		[InspectorName("Boolean")]
		Bool = 0,
		Byte  = 1,
		Short = 2,

		[InspectorName("Unsigned Short")]
		UShort = 3,
		Int = 4,

		[InspectorName("Unsigned Int")]
		UInt = 5,
		Long = 6,

		[InspectorName("Unsigned Long")]
		ULong = 7,
		Float  = 8,
		Double = 9,

		[InspectorName("Byte Array")]
		ByteArray = 10,
		String = 11,
		Vector3 = 12,
		Quaternion = 13,
	}
}