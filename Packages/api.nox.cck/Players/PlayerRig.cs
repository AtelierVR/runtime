using System;
using UnityEngine;

namespace Nox.CCK.Players {
	public enum PlayerRig : ushort {
		// position on floor
		Base = 0,

		// postions of body
		Hips  = 1,
		Spine = 2,
		Chest = 3,
		Neck  = 4,
		Head  = 5,

		// positions of arms
		LeftShoulder  = 6,
		LeftArm       = 7,
		LeftForearm   = 8,
		LeftHand      = 9,
		RightShoulder = 10,
		RightArm      = 11,
		RightForearm  = 12,
		RightHand     = 13,

		// positions of legs
		LeftUpperLeg  = 14,
		LeftLowerLeg  = 15,
		LeftFoot      = 16,
		LeftToes      = 17,
		RightUpperLeg = 18,
		RightLowerLeg = 19,
		RightFoot     = 20,
		RightToes     = 21,

		// positions of fingers
		LeftThumb       = 22,
		LeftIndex       = 23,
		LeftMiddle      = 24,
		LeftRing        = 25,
		LeftPinky       = 26,
		LeftThumbTip    = 27,
		LeftIndexTip    = 28,
		LeftMiddleTip   = 29,
		LeftRingTip     = 30,
		LeftPinkyTip    = 31,
		LeftThumbNail   = 32,
		LeftIndexNail   = 33,
		LeftMiddleNail  = 34,
		LeftRingNail    = 35,
		LeftPinkyNail   = 36,
		RightThumb      = 37,
		RightIndex      = 38,
		RightMiddle     = 39,
		RightRing       = 40,
		RightPinky      = 41,
		RightThumbTip   = 42,
		RightIndexTip   = 43,
		RightMiddleTip  = 44,
		RightRingTip    = 45,
		RightPinkyTip   = 46,
		RightThumbNail  = 47,
		RightIndexNail  = 48,
		RightMiddleNail = 49,
		RightRingNail   = 50,
		RightPinkyNail  = 51,

		// positions of head parts
		RightEye = 52,
		LeftEye  = 53,

		None = ushort.MaxValue
	}

	public static class PlayerRigExtension {
		public static HumanBodyBones ToHumanBodyBones(this PlayerRig rig)
			=> rig switch {
				PlayerRig.Hips            => HumanBodyBones.Hips,
				PlayerRig.Spine           => HumanBodyBones.Spine,
				PlayerRig.Chest           => HumanBodyBones.Chest,
				PlayerRig.Neck            => HumanBodyBones.Neck,
				PlayerRig.Head            => HumanBodyBones.Head,
				PlayerRig.LeftShoulder    => HumanBodyBones.LeftShoulder,
				PlayerRig.LeftArm         => HumanBodyBones.LeftUpperArm,
				PlayerRig.LeftForearm     => HumanBodyBones.LeftLowerArm,
				PlayerRig.LeftHand        => HumanBodyBones.LeftHand,
				PlayerRig.RightShoulder   => HumanBodyBones.RightShoulder,
				PlayerRig.RightArm        => HumanBodyBones.RightUpperArm,
				PlayerRig.RightForearm    => HumanBodyBones.RightLowerArm,
				PlayerRig.RightHand       => HumanBodyBones.RightHand,
				PlayerRig.LeftUpperLeg    => HumanBodyBones.LeftUpperLeg,
				PlayerRig.LeftLowerLeg    => HumanBodyBones.LeftLowerLeg,
				PlayerRig.LeftFoot        => HumanBodyBones.LeftFoot,
				PlayerRig.LeftToes        => HumanBodyBones.LeftToes,
				PlayerRig.RightUpperLeg   => HumanBodyBones.RightUpperLeg,
				PlayerRig.RightLowerLeg   => HumanBodyBones.RightLowerLeg,
				PlayerRig.RightFoot       => HumanBodyBones.RightFoot,
				PlayerRig.RightToes       => HumanBodyBones.RightToes,
				PlayerRig.LeftThumb       => HumanBodyBones.LeftThumbProximal,
				PlayerRig.LeftIndex       => HumanBodyBones.LeftIndexProximal,
				PlayerRig.LeftMiddle      => HumanBodyBones.LeftMiddleProximal,
				PlayerRig.LeftRing        => HumanBodyBones.LeftRingProximal,
				PlayerRig.LeftPinky       => HumanBodyBones.LeftLittleProximal,
				PlayerRig.LeftThumbTip    => HumanBodyBones.LeftThumbDistal,
				PlayerRig.LeftIndexTip    => HumanBodyBones.LeftIndexDistal,
				PlayerRig.LeftMiddleTip   => HumanBodyBones.LeftMiddleDistal,
				PlayerRig.LeftRingTip     => HumanBodyBones.LeftRingDistal,
				PlayerRig.LeftPinkyTip    => HumanBodyBones.LeftLittleDistal,
				PlayerRig.LeftThumbNail   => HumanBodyBones.LeftThumbIntermediate,
				PlayerRig.LeftIndexNail   => HumanBodyBones.LeftIndexIntermediate,
				PlayerRig.LeftMiddleNail  => HumanBodyBones.LeftMiddleIntermediate,
				PlayerRig.LeftRingNail    => HumanBodyBones.LeftRingIntermediate,
				PlayerRig.LeftPinkyNail   => HumanBodyBones.LeftLittleIntermediate,
				PlayerRig.RightThumb      => HumanBodyBones.RightThumbProximal,
				PlayerRig.RightIndex      => HumanBodyBones.RightIndexProximal,
				PlayerRig.RightMiddle     => HumanBodyBones.RightMiddleProximal,
				PlayerRig.RightRing       => HumanBodyBones.RightRingProximal,
				PlayerRig.RightPinky      => HumanBodyBones.RightLittleProximal,
				PlayerRig.RightThumbTip   => HumanBodyBones.RightThumbDistal,
				PlayerRig.RightIndexTip   => HumanBodyBones.RightIndexDistal,
				PlayerRig.RightMiddleTip  => HumanBodyBones.RightMiddleDistal,
				PlayerRig.RightRingTip    => HumanBodyBones.RightRingDistal,
				PlayerRig.RightPinkyTip   => HumanBodyBones.RightLittleDistal,
				PlayerRig.RightThumbNail  => HumanBodyBones.RightThumbIntermediate,
				PlayerRig.RightIndexNail  => HumanBodyBones.RightIndexIntermediate,
				PlayerRig.RightMiddleNail => HumanBodyBones.RightMiddleIntermediate,
				PlayerRig.RightRingNail   => HumanBodyBones.RightRingIntermediate,
				PlayerRig.RightPinkyNail  => HumanBodyBones.RightLittleIntermediate,
				PlayerRig.RightEye        => HumanBodyBones.RightEye,
				PlayerRig.LeftEye         => HumanBodyBones.LeftEye,
				_                         => HumanBodyBones.LastBone
			};

		public static PlayerRig ToPlayerRig(this HumanBodyBones bones)
			=> bones switch {
				HumanBodyBones.Hips                    => PlayerRig.Hips,
				HumanBodyBones.Spine                   => PlayerRig.Spine,
				HumanBodyBones.Chest                   => PlayerRig.Chest,
				HumanBodyBones.Neck                    => PlayerRig.Neck,
				HumanBodyBones.Head                    => PlayerRig.Head,
				HumanBodyBones.LeftShoulder            => PlayerRig.LeftShoulder,
				HumanBodyBones.LeftUpperArm            => PlayerRig.LeftArm,
				HumanBodyBones.LeftLowerArm            => PlayerRig.LeftForearm,
				HumanBodyBones.LeftHand                => PlayerRig.LeftHand,
				HumanBodyBones.RightShoulder           => PlayerRig.RightShoulder,
				HumanBodyBones.RightUpperArm           => PlayerRig.RightArm,
				HumanBodyBones.RightLowerArm           => PlayerRig.RightForearm,
				HumanBodyBones.RightHand               => PlayerRig.RightHand,
				HumanBodyBones.LeftUpperLeg            => PlayerRig.LeftUpperLeg,
				HumanBodyBones.LeftLowerLeg            => PlayerRig.LeftLowerLeg,
				HumanBodyBones.LeftFoot                => PlayerRig.LeftFoot,
				HumanBodyBones.LeftToes                => PlayerRig.LeftToes,
				HumanBodyBones.RightUpperLeg           => PlayerRig.RightUpperLeg,
				HumanBodyBones.RightLowerLeg           => PlayerRig.RightLowerLeg,
				HumanBodyBones.RightFoot               => PlayerRig.RightFoot,
				HumanBodyBones.RightToes               => PlayerRig.RightToes,
				HumanBodyBones.LeftThumbProximal       => PlayerRig.LeftThumb,
				HumanBodyBones.LeftIndexProximal       => PlayerRig.LeftIndex,
				HumanBodyBones.LeftMiddleProximal      => PlayerRig.LeftMiddle,
				HumanBodyBones.LeftRingProximal        => PlayerRig.LeftRing,
				HumanBodyBones.LeftLittleProximal      => PlayerRig.LeftPinky,
				HumanBodyBones.LeftThumbDistal         => PlayerRig.LeftThumbTip,
				HumanBodyBones.LeftIndexDistal         => PlayerRig.LeftIndexTip,
				HumanBodyBones.LeftMiddleDistal        => PlayerRig.LeftMiddleTip,
				HumanBodyBones.LeftRingDistal          => PlayerRig.LeftRingTip,
				HumanBodyBones.LeftLittleDistal        => PlayerRig.LeftPinkyTip,
				HumanBodyBones.LeftThumbIntermediate   => PlayerRig.LeftThumbNail,
				HumanBodyBones.LeftIndexIntermediate   => PlayerRig.LeftIndexNail,
				HumanBodyBones.LeftMiddleIntermediate  => PlayerRig.LeftMiddleNail,
				HumanBodyBones.LeftRingIntermediate    => PlayerRig.LeftRingNail,
				HumanBodyBones.LeftLittleIntermediate  => PlayerRig.LeftPinkyNail,
				HumanBodyBones.RightThumbProximal      => PlayerRig.RightThumb,
				HumanBodyBones.RightIndexProximal      => PlayerRig.RightIndex,
				HumanBodyBones.RightMiddleProximal     => PlayerRig.RightMiddle,
				HumanBodyBones.RightRingProximal       => PlayerRig.RightRing,
				HumanBodyBones.RightLittleProximal     => PlayerRig.RightPinky,
				HumanBodyBones.RightThumbDistal        => PlayerRig.RightThumbTip,
				HumanBodyBones.RightIndexDistal        => PlayerRig.RightIndexTip,
				HumanBodyBones.RightMiddleDistal       => PlayerRig.RightMiddleTip,
				HumanBodyBones.RightRingDistal         => PlayerRig.RightRingTip,
				HumanBodyBones.RightLittleDistal       => PlayerRig.RightPinkyTip,
				HumanBodyBones.RightThumbIntermediate  => PlayerRig.RightThumbNail,
				HumanBodyBones.RightIndexIntermediate  => PlayerRig.RightIndexNail,
				HumanBodyBones.RightMiddleIntermediate => PlayerRig.RightMiddleNail,
				HumanBodyBones.RightRingIntermediate   => PlayerRig.RightRingNail,
				HumanBodyBones.RightLittleIntermediate => PlayerRig.RightPinkyNail,
				HumanBodyBones.RightEye                => PlayerRig.RightEye,
				HumanBodyBones.LeftEye                 => PlayerRig.LeftEye,
				_                                      => PlayerRig.None
			};

		public static ushort ToIndex(this PlayerRig rig)
			=> (ushort)rig;
		
		public static ushort ToIndex(this HumanBodyBones bones)
			=> bones.ToPlayerRig().ToIndex();

		public static PlayerRig ToPlayerRig(this ushort index)
			=> Enum.IsDefined(typeof(PlayerRig), index)
				? (PlayerRig)index
				: PlayerRig.None;

		public static HumanBodyBones ToHumanBodyBones(this ushort index)
			=> Enum.IsDefined(typeof(HumanBodyBones), index)
				? (HumanBodyBones)index
				: HumanBodyBones.LastBone;
	}
}