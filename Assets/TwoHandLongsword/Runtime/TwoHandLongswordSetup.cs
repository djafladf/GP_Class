using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace TwoHandLongsword
{
    [DisallowMultipleComponent]
    public sealed class TwoHandLongswordSetup : MonoBehaviour
    {
        [Header("Reference — source clips are read only")]
        public Animator animator;
        public AnimationClip referenceIdle;
        public AnimationClip bodyIdle;
        [Min(0)] public float referenceTime;
        [Min(0)] public float bodyTime;
        public Transform sword;
        [Tooltip("Only enable after confirming the visible arm pose is the intended reference.")]
        public bool useCurrentBodyPose;
        [Tooltip("Sword-local direction from pommel to blade tip. Confirm against the real mesh; zero blocks capture.")]
        public Vector3 bladeDirectionLocal;
        [Header("Edit preview — optional Walk / Run body clip")]
        public AnimationClip previewBody;
        [Min(0)] public float previewTime;

        [Header("Generated controls")]
        public Transform weaponControl;
        public Transform rightGrip, leftGrip, rightHint, leftHint;
        public Rig locomotionRig;
        public TwoBoneIKConstraint rightIK, leftIK;

        [Header("Offsets: weapon in character space; grips in sword space")]
        public Vector3 weaponPositionOffset, weaponRotationOffset;
        public Vector3 rightPositionOffset, rightRotationOffset;
        public Vector3 leftPositionOffset, leftRotationOffset;
        public Vector3 rightHintOffset, leftHintOffset;
        [Range(0.05f, 1f)] public float hintDistanceInArmLengths = 0.35f;

        [Header("Runtime — explicitly enable only for Idle / Walk / Run")]
        public bool locomotionActive;
        [Range(0, 1)] public float locomotionWeight = 1;
        [Min(0)] public float blendSpeed = 8;

        [HideInInspector] public bool captured;
        [HideInInspector] public Vector3 midpointLocal, swordPositionFromMidpoint;
        [HideInInspector] public Quaternion swordRotationLocal = Quaternion.identity;
        [HideInInspector] public Vector3 rightPosition, leftPosition;
        [HideInInspector] public Quaternion rightRotation = Quaternion.identity, leftRotation = Quaternion.identity;
        [HideInInspector] public Vector3 rightHintLocal, leftHintLocal;

        // No ExecuteAlways or OnValidate writes: editing settings never moves the source.
        public void ApplyOffsets()
        {
            if (!captured || !weaponControl || !sword || !rightGrip || !leftGrip || !rightHint || !leftHint)
                throw new InvalidOperationException("Capture a valid reference before applying offsets.");
            weaponControl.localPosition = midpointLocal + weaponPositionOffset;
            weaponControl.localRotation = Quaternion.Euler(weaponRotationOffset);
            sword.localPosition = swordPositionFromMidpoint;
            sword.localRotation = swordRotationLocal;
            rightGrip.SetLocalPositionAndRotation(rightPosition + rightPositionOffset,
                rightRotation * Quaternion.Euler(rightRotationOffset));
            leftGrip.SetLocalPositionAndRotation(leftPosition + leftPositionOffset,
                leftRotation * Quaternion.Euler(leftRotationOffset));
            rightHint.localPosition = rightHintLocal + rightHintOffset;
            leftHint.localPosition = leftHintLocal + leftHintOffset;
        }

        public void SetLocomotionActive(bool value)
        {
            locomotionActive = value;
            // Attacks relinquish both arms immediately; entering locomotion blends in.
            if (!value && locomotionRig) locomotionRig.weight = 0;
        }

        void OnEnable()
        {
            if (Application.isPlaying && locomotionRig) locomotionRig.weight = 0;
        }

        void Update()
        {
            if (!Application.isPlaying || !locomotionRig) return;
            float goal = locomotionActive ? locomotionWeight : 0;
            locomotionRig.weight = Mathf.MoveTowards(locomotionRig.weight, goal, blendSpeed * Time.deltaTime);
        }

        void OnDisable()
        {
            if (Application.isPlaying && locomotionRig) locomotionRig.weight = 0;
        }

        void OnDrawGizmosSelected()
        {
            if (!rightGrip || !leftGrip) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightGrip.position, leftGrip.position);
            Gizmos.DrawWireSphere((rightGrip.position + leftGrip.position) * .5f, .025f);
            DrawArm(rightIK, Color.red);
            DrawArm(leftIK, Color.green);
        }

        static void DrawArm(TwoBoneIKConstraint ik, Color color)
        {
            if (!ik) return;
            var d = ik.data;
            if (!d.root || !d.mid || !d.tip || !d.target || !d.hint) return;
            Gizmos.color = color;
            Gizmos.DrawLine(d.root.position, d.mid.position);
            Gizmos.DrawLine(d.mid.position, d.tip.position);
            Gizmos.DrawLine(d.mid.position, d.hint.position);
            Gizmos.DrawWireSphere(d.target.position, .02f);
            Gizmos.DrawWireSphere(d.hint.position, .03f);
        }
    }
}
