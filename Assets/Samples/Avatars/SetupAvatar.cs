using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Nox.CCK.Avatars;

[RequireComponent(typeof(AvatarDescriptor))]
public class SetupAvatar : MonoBehaviour
{
    [Header("Rig Settings")]
    public bool generateHeadRig = true;
    public bool generateTwoBoneIK = true;
    public bool generateHipIK = true;
    public bool useHeadTwoBoneIK = true; // New option for TwoBoneIK head control

    [Header("Target Objects")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform headTarget;
    public Transform hipTarget;

    void Start()
    {
        var avatar = GetComponent<AvatarDescriptor>();
        if (avatar == null)
            throw new System.Exception("AvatarDescriptor component is missing on the GameObject.");

        SetupLayer(avatar, LayerMask.NameToLayer("LocalAvatar"));
        SetupRigBuilder(avatar);
        GenerateAutoRig(avatar);
    }

    private void SetupLayer(AvatarDescriptor avatar, int layer)
    {
        avatar.transform.gameObject.layer = layer;
        SetupLayer(avatar.transform, layer);
    }

    private void SetupLayer(Transform transform, int layer)
    {
        var renderers = GetRenderers(transform);
        foreach (var renderer in renderers)
            renderer.gameObject.layer = layer;
    }

    private Renderer[] GetRenderers(Transform transform)
    {
        var list = new List<Renderer>();
        var renderers = transform.GetComponents<Renderer>();
        list.AddRange(renderers);
        foreach (Transform child in transform)
            list.AddRange(GetRenderers(child));
        return list.ToArray();
    }

    private void SetupRigBuilder(AvatarDescriptor avatar)
    {
        var rigBuilder = avatar.gameObject.GetComponent<RigBuilder>() 
                         ?? avatar.gameObject.AddComponent<RigBuilder>();

        // Disable the RigBuilder while setting up the rigs
        rigBuilder.enabled = false;
        // Clear existing layers
        rigBuilder.layers.Clear();
    }

    private void GenerateAutoRig(AvatarDescriptor avatar)
    {
        var animator = avatar.Animator;
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("Avatar must have a humanoid Animator to generate rig automatically.");
            return;
        }

        var rigBuilder = avatar.GetComponent<RigBuilder>();
        var rigContainer = CreateOrGetRigContainer(avatar.transform);

        // Generate head rig
        if (generateHeadRig)
        {
            var headRig = CreateRig(rigContainer, "HeadRig");
            GenerateHeadRig(animator, headRig, ref headTarget);
            rigBuilder.layers.Add(new RigLayer(headRig.GetComponent<Rig>(), true));
        }

        // Generate two bone IK rigs for arms and legs
        if (generateTwoBoneIK)
        {
            var leftArmRig = CreateRig(rigContainer, "LeftArmRig");
            var rightArmRig = CreateRig(rigContainer, "RightArmRig");
            var leftLegRig = CreateRig(rigContainer, "LeftLegRig");
            var rightLegRig = CreateRig(rigContainer, "RightLegRig");

            GenerateArmIK(animator, leftArmRig, true);
            GenerateArmIK(animator, rightArmRig, false);
            GenerateLegIK(animator, leftLegRig, true);
            GenerateLegIK(animator, rightLegRig, false);

            rigBuilder.layers.Add(new RigLayer(leftArmRig.GetComponent<Rig>(), true));
            rigBuilder.layers.Add(new RigLayer(rightArmRig.GetComponent<Rig>(), true));
            rigBuilder.layers.Add(new RigLayer(leftLegRig.GetComponent<Rig>(), true));
            rigBuilder.layers.Add(new RigLayer(rightLegRig.GetComponent<Rig>(), true));
        }

        // Generate hip IK rig
        if (generateHipIK)
        {
            var hipRig = CreateRig(rigContainer, "HipRig");
            GenerateHipIK(animator, hipRig, ref hipTarget);
            rigBuilder.layers.Add(new RigLayer(hipRig.GetComponent<Rig>(), true));
        }

        // Build the rig to initialize constraints
        rigBuilder.Build();
        // Re-enable the RigBuilder
        rigBuilder.enabled = true;

        Debug.Log($"Auto-generated {rigBuilder.layers.Count} rigs for avatar: {avatar.name}");
    }

    private Transform CreateOrGetRigContainer(Transform parent)
    {
        var existing = parent.Find("RigContainer");
        if (existing != null)
            return existing;

        var rigContainer = new GameObject("RigContainer");
        rigContainer.transform.SetParent(parent);
        rigContainer.transform.localPosition = Vector3.zero;
        rigContainer.transform.localRotation = Quaternion.identity;

        return rigContainer.transform;
    }

    private Transform CreateRig(Transform parent, string rigName)
    {
        var rigGO = new GameObject(rigName);
        rigGO.transform.SetParent(parent);
        rigGO.transform.localPosition = Vector3.zero;
        rigGO.transform.localRotation = Quaternion.identity;

        var rig = rigGO.AddComponent<Rig>();
        rig.weight = 1f;

        return rigGO.transform;
    }

    private void GenerateArmIK(Animator animator, Transform rigContainer, bool isLeft)
    {
        var prefix = isLeft ? "Left" : "Right";
        var shoulderBone = isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
        var forearmBone = isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
        var handBone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

        var shoulder = animator.GetBoneTransform(shoulderBone);
        var forearm = animator.GetBoneTransform(forearmBone);
        var hand = animator.GetBoneTransform(handBone);

        if (shoulder == null || forearm == null || hand == null) return;

        // Create or assign target for the hand
        var handTarget = isLeft ? leftHandTarget : rightHandTarget;
        if (handTarget == null)
        {
            var targetGO = new GameObject($"{prefix}HandTarget");
            targetGO.transform.SetParent(rigContainer);
            targetGO.transform.position = hand.position;
            targetGO.transform.rotation = hand.rotation;

            if (isLeft)
                leftHandTarget = targetGO.transform;
            else
                rightHandTarget = targetGO.transform;

            handTarget = targetGO.transform;
        }

        // Create hint target for elbow direction
        var hintTargetGO = new GameObject($"{prefix}ElbowHint");
        hintTargetGO.transform.SetParent(rigContainer);
        var elbowDirection = isLeft ? Vector3.left : Vector3.right;
        hintTargetGO.transform.position = forearm.position + elbowDirection * 0.3f;

        // Create TwoBoneIK constraint
        var constraintGO = new GameObject($"{prefix}ArmIK");
        constraintGO.transform.SetParent(rigContainer);

        var ikConstraint = constraintGO.AddComponent<TwoBoneIKConstraint>();
        ikConstraint.data.root = shoulder;
        ikConstraint.data.mid = forearm;
        ikConstraint.data.tip = hand;
        ikConstraint.data.target = handTarget;
        ikConstraint.data.hint = hintTargetGO.transform;
        ikConstraint.data.targetPositionWeight = 1f;
        ikConstraint.data.targetRotationWeight = 1f;
        ikConstraint.data.hintWeight = 0.5f;
        ikConstraint.weight = 1f;
    }

    private void GenerateLegIK(Animator animator, Transform rigContainer, bool isLeft)
    {
        var prefix = isLeft ? "Left" : "Right";
        var upperLegBone = isLeft ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
        var lowerLegBone = isLeft ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
        var footBone = isLeft ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;

        var upperLeg = animator.GetBoneTransform(upperLegBone);
        var lowerLeg = animator.GetBoneTransform(lowerLegBone);
        var foot = animator.GetBoneTransform(footBone);

        if (upperLeg == null || lowerLeg == null || foot == null) return;

        // Create or assign target for the foot
        var footTarget = isLeft ? leftFootTarget : rightFootTarget;
        if (footTarget == null)
        {
            var targetGO = new GameObject($"{prefix}FootTarget");
            targetGO.transform.SetParent(rigContainer);
            targetGO.transform.position = foot.position;
            targetGO.transform.rotation = foot.rotation;

            if (isLeft)
                leftFootTarget = targetGO.transform;
            else
                rightFootTarget = targetGO.transform;

            footTarget = targetGO.transform;
        }

        // Create hint target for knee direction
        var hintTargetGO = new GameObject($"{prefix}KneeHint");
        hintTargetGO.transform.SetParent(rigContainer);
        hintTargetGO.transform.position = lowerLeg.position + Vector3.forward * 0.3f;

        // Create TwoBoneIK constraint
        var constraintGO = new GameObject($"{prefix}LegIK");
        constraintGO.transform.SetParent(rigContainer);

        var ikConstraint = constraintGO.AddComponent<TwoBoneIKConstraint>();
        ikConstraint.data.root = upperLeg;
        ikConstraint.data.mid = lowerLeg;
        ikConstraint.data.tip = foot;
        ikConstraint.data.target = footTarget;
        ikConstraint.data.hint = hintTargetGO.transform;
        ikConstraint.data.targetPositionWeight = 1f;
        ikConstraint.data.targetRotationWeight = 1f;
        ikConstraint.data.hintWeight = 0.5f;
        ikConstraint.weight = 1f;
    }

    private void GenerateHeadRig(Animator animator, Transform rigContainer, ref Transform target)
    {
        var headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
        var neckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
        var chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
        
        if (headTransform == null) return;

        // Create target if not assigned
        if (target == null)
        {
            var targetGO = new GameObject("HeadTarget");
            targetGO.transform.SetParent(rigContainer);
            targetGO.transform.position = headTransform.position + headTransform.forward * 2f;
            target = targetGO.transform;
        }

        // Create constraint GameObject
        var constraintGO = new GameObject("HeadConstraint");
        constraintGO.transform.SetParent(rigContainer);

        if (useHeadTwoBoneIK && neckTransform != null && chestTransform != null)
        {
            // Create hint target for neck direction
            var hintTargetGO = new GameObject("NeckHint");
            hintTargetGO.transform.SetParent(rigContainer);
            hintTargetGO.transform.position = neckTransform.position + neckTransform.up * 0.2f;

            // Add TwoBoneIK constraint for Chest/Neck/Head chain
            var ikConstraint = constraintGO.AddComponent<TwoBoneIKConstraint>();
            ikConstraint.data.root = chestTransform;
            ikConstraint.data.mid = neckTransform;
            ikConstraint.data.tip = headTransform;
            ikConstraint.data.target = target;
            ikConstraint.data.hint = hintTargetGO.transform;
            ikConstraint.data.targetPositionWeight = 1f;
            ikConstraint.data.targetRotationWeight = 1f;
            ikConstraint.data.hintWeight = 0.3f; // Lower weight for more natural movement
            ikConstraint.weight = 1f;
        }
        else
        {
            // Fallback to MultiAim constraint for head look
            var aimConstraint = constraintGO.AddComponent<MultiAimConstraint>();
            aimConstraint.data.constrainedObject = headTransform;
            aimConstraint.data.sourceObjects.Add(new WeightedTransform(target, 1f));
            aimConstraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
            aimConstraint.data.upAxis = MultiAimConstraintData.Axis.Y;
            aimConstraint.data.worldUpType = MultiAimConstraintData.WorldUpType.ObjectUp;
            aimConstraint.data.worldUpObject = headTransform.parent;
            aimConstraint.weight = 1f;
        }
    }

    private void GenerateHipIK(Animator animator, Transform rigContainer, ref Transform target)
    {
        var hipTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hipTransform == null) return;

        // Create target if not assigned
        if (target == null)
        {
            var targetGO = new GameObject("HipTarget");
            targetGO.transform.SetParent(rigContainer);
            targetGO.transform.position = hipTransform.position;
            targetGO.transform.rotation = hipTransform.rotation;
            target = targetGO.transform;
        }

        // Create constraint GameObject
        var constraintGO = new GameObject("HipConstraint");
        constraintGO.transform.SetParent(rigContainer);

        // Add MultiPosition constraint for hip movement
        var positionConstraint = constraintGO.AddComponent<MultiPositionConstraint>();
        positionConstraint.data.constrainedObject = hipTransform;
        positionConstraint.data.sourceObjects.Add(new WeightedTransform(target, 1f));
        positionConstraint.data.constrainedXAxis = true;
        positionConstraint.data.constrainedYAxis = true;
        positionConstraint.data.constrainedZAxis = true;
        positionConstraint.weight = 1f;

        // Add MultiRotation constraint for hip rotation
        var rotationConstraint = constraintGO.AddComponent<MultiRotationConstraint>();
        rotationConstraint.data.constrainedObject = hipTransform;
        rotationConstraint.data.sourceObjects.Add(new WeightedTransform(target, 1f));
        rotationConstraint.data.constrainedXAxis = true;
        rotationConstraint.data.constrainedYAxis = true;
        rotationConstraint.data.constrainedZAxis = true;
        rotationConstraint.weight = 1f;
    }

    // Public methods to move hands and feet
    public void MoveLeftHand(Vector3 position)
    {
        if (leftHandTarget != null)
            leftHandTarget.position = position;
    }

    public void MoveRightHand(Vector3 position)
    {
        if (rightHandTarget != null)
            rightHandTarget.position = position;
    }

    public void RotateLeftHand(Quaternion rotation)
    {
        if (leftHandTarget != null)
            leftHandTarget.rotation = rotation;
    }

    public void RotateRightHand(Quaternion rotation)
    {
        if (rightHandTarget != null)
            rightHandTarget.rotation = rotation;
    }

    // Public methods for hip control
    public void MoveHip(Vector3 position)
    {
        if (hipTarget != null)
            hipTarget.position = position;
    }

    public void RotateHip(Quaternion rotation)
    {
        if (hipTarget != null)
            hipTarget.rotation = rotation;
    }

    public void MoveHipLocal(Vector3 localOffset)
    {
        if (hipTarget != null)
        {
            var animator = GetComponent<AvatarDescriptor>().Animator;
            var hip = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hip != null)
                hipTarget.position = hip.position + localOffset;
        }
    }

    public void ResetHipPosition()
    {
        var animator = GetComponent<AvatarDescriptor>().Animator;
        
        if (hipTarget != null)
        {
            var hip = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hip != null)
            {
                hipTarget.position = hip.position;
                hipTarget.rotation = hip.rotation;
            }
        }
    }

    public void MoveLeftHandLocal(Vector3 localOffset)
    {
        if (leftHandTarget != null)
        {
            var animator = GetComponent<AvatarDescriptor>().Animator;
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
                leftHandTarget.position = leftHand.position + localOffset;
        }
    }

    public void MoveRightHandLocal(Vector3 localOffset)
    {
        if (rightHandTarget != null)
        {
            var animator = GetComponent<AvatarDescriptor>().Animator;
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
                rightHandTarget.position = rightHand.position + localOffset;
        }
    }

    public void PointAt(Vector3 worldPosition, bool leftHand = true)
    {
        var targetTransform = leftHand ? leftHandTarget : rightHandTarget;
        if (targetTransform != null)
        {
            var direction = (worldPosition - targetTransform.position).normalized;
            targetTransform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ReachFor(Vector3 worldPosition, bool leftHand = true)
    {
        var targetTransform = leftHand ? leftHandTarget : rightHandTarget;
        if (targetTransform != null)
        {
            targetTransform.position = worldPosition;
            var direction = (worldPosition - transform.position).normalized;
            targetTransform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ResetHandPositions()
    {
        var animator = GetComponent<AvatarDescriptor>().Animator;

        if (leftHandTarget != null)
        {
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                leftHandTarget.position = leftHand.position;
                leftHandTarget.rotation = leftHand.rotation;
            }
        }

        if (rightHandTarget != null)
        {
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
                rightHandTarget.position = rightHand.position;
                rightHandTarget.rotation = rightHand.rotation;
            }
        }
    }

    private void ToggleRigControl()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder != null && rigBuilder.layers.Count > 0)
        {
            // Toggle all rigs at once
            var newWeight = rigBuilder.layers[0].rig.weight > 0.5f ? 0f : 1f;

            foreach (var layer in rigBuilder.layers)
            {
                if (layer.rig != null)
                {
                    layer.rig.weight = newWeight;
                }
            }

            Debug.Log($"All rig weights set to: {newWeight}");
        }
    }

    // Public methods to set rig and IK weights
    public void SetRigWeight(float weight)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder != null)
        {
            foreach (var layer in rigBuilder.layers)
            {
                if (layer.rig != null)
                    layer.rig.weight = Mathf.Clamp01(weight);
            }
        }
    }

    public void SetIKWeights(float positionWeight, float rotationWeight, float hintWeight, bool leftArm = true)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        var prefix = leftArm ? "Left" : "Right";
        var rigName = $"{prefix}Arm";

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains(rigName))
            {
                var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
                foreach (var constraint in ikConstraints)
                {
                    var data = constraint.data;
                    data.targetPositionWeight = Mathf.Clamp01(positionWeight);
                    data.targetRotationWeight = Mathf.Clamp01(rotationWeight);
                    data.hintWeight = Mathf.Clamp01(hintWeight);
                    constraint.data = data;
                }
            }
        }
    }

    public void SetAllIKWeights(float positionWeight, float rotationWeight, float hintWeight)
    {
        // Arms
        SetIKWeights(positionWeight, rotationWeight, hintWeight, true);  // Left arm
        SetIKWeights(positionWeight, rotationWeight, hintWeight, false); // Right arm
        // Legs
        SetLegIKWeights(positionWeight, rotationWeight, hintWeight, true);
        SetLegIKWeights(positionWeight, rotationWeight, hintWeight, false);
        // Head (if using TwoBoneIK)
        SetHeadIKWeights(positionWeight, rotationWeight, hintWeight);
    }

    public void SetLegIKWeights(float positionWeight, float rotationWeight, float hintWeight, bool leftLeg = true)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        var prefix = leftLeg ? "Left" : "Right";
        var rigName = $"{prefix}Leg";

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains(rigName))
            {
                var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
                foreach (var constraint in ikConstraints)
                {
                    var data = constraint.data;
                    data.targetPositionWeight = Mathf.Clamp01(positionWeight);
                    data.targetRotationWeight = Mathf.Clamp01(rotationWeight);
                    data.hintWeight = Mathf.Clamp01(hintWeight);
                    constraint.data = data;
                }
            }
        }
    }

    public void SetHeadIKWeights(float positionWeight, float rotationWeight, float hintWeight)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains("Head"))
            {
                var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
                foreach (var constraint in ikConstraints)
                {
                    var data = constraint.data;
                    data.targetPositionWeight = Mathf.Clamp01(positionWeight);
                    data.targetRotationWeight = Mathf.Clamp01(rotationWeight);
                    data.hintWeight = Mathf.Clamp01(hintWeight);
                    constraint.data = data;
                }
            }
        }
    }

    // Methods for hip constraint control
    public void SetHipConstraintWeights(float positionWeight, float rotationWeight)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains("Hip"))
            {
                var positionConstraints = layer.rig.GetComponentsInChildren<MultiPositionConstraint>();
                var rotationConstraints = layer.rig.GetComponentsInChildren<MultiRotationConstraint>();

                foreach (var constraint in positionConstraints)
                {
                    constraint.weight = Mathf.Clamp01(positionWeight);
                }

                foreach (var constraint in rotationConstraints)
                {
                    constraint.weight = Mathf.Clamp01(rotationWeight);
                }
            }
        }
    }

    public MultiPositionConstraint GetHipPositionConstraint()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return null;

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains("Hip"))
            {
                return layer.rig.GetComponentInChildren<MultiPositionConstraint>();
            }
        }

        return null;
    }

    public MultiRotationConstraint GetHipRotationConstraint()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return null;

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null && layer.rig.name.Contains("Hip"))
            {
                return layer.rig.GetComponentInChildren<MultiRotationConstraint>();
            }
        }

        return null;
    }

    // Public methods for head control
    public void MoveHead(Vector3 position)
    {
        if (headTarget != null)
            headTarget.position = position;
    }

    public void RotateHead(Quaternion rotation)
    {
        if (headTarget != null)
            headTarget.rotation = rotation;
    }

    public void LookAt(Vector3 worldPosition)
    {
        if (headTarget != null)
        {
            var direction = (worldPosition - headTarget.position).normalized;
            headTarget.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ResetHeadPosition()
    {
        var animator = GetComponent<AvatarDescriptor>().Animator;
        
        if (headTarget != null)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                headTarget.position = head.position + head.forward * 2f;
                headTarget.rotation = head.rotation;
            }
        }
    }

    public void EnableDisableIK(bool enable, bool includeHands = true, bool includeFeet = true, bool includeHips = true, bool includeHead = true)
    {
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        foreach (var layer in rigBuilder.layers)
        {
            if (layer.rig != null)
            {
                bool shouldToggle = false;

                if (includeHands && layer.rig.name.Contains("Arm"))
                    shouldToggle = true;
                if (includeFeet && layer.rig.name.Contains("Leg"))
                    shouldToggle = true;
                if (includeHips && layer.rig.name.Contains("Hip"))
                    shouldToggle = true;
                if (includeHead && layer.rig.name.Contains("Head"))
                    shouldToggle = true;

                if (shouldToggle)
                    layer.rig.weight = enable ? 1f : 0f;
            }
        }
    }
}
