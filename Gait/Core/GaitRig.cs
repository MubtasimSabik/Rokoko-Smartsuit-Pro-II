// Purpose: Resolve & hold rig references; compute leg length; expose participant path

using UnityEngine;

namespace Gait.Core
{
    public enum RigResolveMode { Auto, HumanoidOnly, PathOnly }

    [System.Serializable]
    public class GaitRig
    {
        [Header("Participant")]
        public string participantName;

        [Header("Resolved bones")]
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform leftThigh;
        public Transform rightThigh;
        public Transform leftShin;
        public Transform rightShin;
        public Transform leftShoulder;
        public Transform rightShoulder;
        public Transform head;
        public Transform hips;

        [HideInInspector] public float legLength;

    
        public bool ResolveFrom(Transform root, out string message, RigResolveMode mode = RigResolveMode.Auto)
        {
            message = string.Empty;
            if (root == null)
            {
                message = "ResolveFrom: root is null.";
                return false;
            }

            var participant = string.IsNullOrEmpty(participantName) ? root.name : participantName;

            bool ok = mode switch
            {
                RigResolveMode.HumanoidOnly => TryResolveHumanoid(root, out message),
                RigResolveMode.PathOnly => TryResolveByPath(root, participant, out message),
                _ => TryResolveHumanoid(root, out message) || TryResolveByPath(root, participant, out message)
            };

            if (!ok)
            {
                // report clearly once.
                Debug.LogError($"GaitRig: Failed to resolve rig for '{participant}'. {message}", root);
                return false;
            }

            // Compute leg length as average of the two legs that are present.
            legLength = ComputeAverageLegLength();
            return true;
        }

        public bool IsValid()
        {
            return leftFoot && rightFoot &&
                   leftThigh && rightThigh &&
                   leftShin && rightShin &&
                   leftShoulder && rightShoulder &&
                   head && hips;
        }


        private bool TryResolveHumanoid(Transform root, out string message)
        {
            message = string.Empty;
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
            {
                message = animator == null ? "No Animator found." : "Animator is not Humanoid.";
                return false;
            }

            // Map humanoid bones → our fields
            leftFoot       = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot      = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            leftShin       = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightShin      = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            leftThigh      = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightThigh     = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            leftShoulder   = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightShoulder  = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            head           = animator.GetBoneTransform(HumanBodyBones.Head);
            hips           = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (!IsValid())
            {
                message = "Humanoid resolution incomplete (one or more required bones are null).";
                return false;
            }
            return true;
        }

        private bool TryResolveByPath(Transform root, string participant, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrEmpty(participant))
            {
                message = "Participant name unresolved. set participantName as root.name";
                return false;
            }

            
            string basePath = participant + "/Root/Hips";
            Transform baseT = root.Find(basePath);
            if (baseT == null)
            {
                message = $"Base path not found: '{basePath}'.";
                return false;
            }

            // Cache common spine root
            var spine4 = baseT.Find("Spine1/Spine2/Spine3/Spine4");

            // Legs
            rightThigh = baseT.Find("RightThigh");
            leftThigh  = baseT.Find("LeftThigh");
            rightShin  = baseT.Find("RightThigh/RightShin");
            leftShin   = baseT.Find("LeftThigh/LeftShin");
            rightFoot  = baseT.Find("RightThigh/RightShin/RightFoot");
            leftFoot   = baseT.Find("LeftThigh/LeftShin/LeftFoot");

            // Upper body
            rightShoulder = spine4 ? spine4.Find("RightShoulder") : null;
            leftShoulder  = spine4 ? spine4.Find("LeftShoulder")  : null;
            head          = spine4 ? spine4.Find("Neck/Head")     : null;
            hips          = baseT;

            if (!IsValid())
            {
                message = BuildMissingList();
                return false;
            }

            return true;
        }

        private float ComputeAverageLegLength()
        {
            int n = 0;
            float sum = 0f;

            if (rightThigh && rightFoot)
            {
                sum += Vector3.Distance(rightThigh.position, rightFoot.position);
                n++;
            }
            if (leftThigh && leftFoot)
            {
                sum += Vector3.Distance(leftThigh.position, leftFoot.position);
                n++;
            }

            // Fallback: if one leg is missing, return the other; else zero.
            return n > 0 ? (sum / n) : 0f;
        }

        private string BuildMissingList()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Missing bones: ");
            void M(string name, Transform t) { if (!t) sb.Append(name).Append(", "); }

            M(nameof(leftFoot), leftFoot);
            M(nameof(rightFoot), rightFoot);
            M(nameof(leftThigh), leftThigh);
            M(nameof(rightThigh), rightThigh);
            M(nameof(leftShin), leftShin);
            M(nameof(rightShin), rightShin);
            M(nameof(leftShoulder), leftShoulder);
            M(nameof(rightShoulder), rightShoulder);
            M(nameof(head), head);
            M(nameof(hips), hips);

            if (sb[sb.Length - 2] == ',') sb.Length -= 2; 
            return sb.ToString();
        }

#if UNITY_EDITOR

        public void DrawGizmos(Color? color = null)
        {
            var c = color ?? new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.color = c.Value;

            void Dot(Transform t) { if (t) Gizmos.DrawSphere(t.position, 0.01f); }
            void Link(Transform a, Transform b) { if (a && b) Gizmos.DrawLine(a.position, b.position); }

            Dot(hips); Dot(head);
            Dot(leftShoulder); Dot(rightShoulder);
            Dot(leftThigh); Dot(rightThigh);
            Dot(leftShin); Dot(rightShin);
            Dot(leftFoot); Dot(rightFoot);

            Link(leftThigh, leftShin); Link(leftShin, leftFoot);
            Link(rightThigh, rightShin); Link(rightShin, rightFoot);
            Link(hips, leftThigh); Link(hips, rightThigh);
        }
#endif
    }
}
