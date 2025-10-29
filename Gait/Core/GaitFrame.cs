// Purpose: Per-frame data containers & simple helpers

using System;
using UnityEngine;

namespace Gait.Core
{
    [Serializable]
    public struct GroundState
    {
        public bool leftGrounded;
        public bool rightGrounded;

        public GroundState(bool left, bool right)
        {
            leftGrounded = left;
            rightGrounded = right;
        }
    }

    [Serializable]
    public struct JointRotations
    {
        public Quaternion rightThigh, leftThigh, rightShin, leftShin, rightShoulder, leftShoulder, head;

        public JointRotations(
            Quaternion rightThigh, Quaternion leftThigh,
            Quaternion rightShin, Quaternion leftShin,
            Quaternion rightShoulder, Quaternion leftShoulder,
            Quaternion head)
        {
            this.rightThigh = rightThigh;
            this.leftThigh = leftThigh;
            this.rightShin = rightShin;
            this.leftShin = leftShin;
            this.rightShoulder = rightShoulder;
            this.leftShoulder = leftShoulder;
            this.head = head;
        }
    }

    [Serializable]
    public struct HipsKinematics
    {
        public Vector3 hipPosition;
        public float velocity;
        public float acceleration;

        public HipsKinematics(Vector3 hipPosition, float velocity, float acceleration)
        {
            this.hipPosition = hipPosition;
            this.velocity = velocity;
            this.acceleration = acceleration;
        }
    }

    [Serializable]
    public class GaitFrame
    {
        public int frame;
        public float timeSum;
        public DateTime sysTime;
        public double sysTimeDiffMs;
        public int stepCount;
        public double lastStepDurationMs;
        public float stepLengthInst;
        public float stepLengthAccum;
        public float stepHipDistance;
        public float strideWidth;
        public float strideLengthRight, strideLengthLeft;
        public float strideTimeRight, strideTimeLeft;
        public float legLength;
        public GroundState ground;
        public JointRotations joints;
        public HipsKinematics hips;

        [Obsolete("Use hips.hipPosition.")]
        public Vector3 hipPositionXZ
        {
            get => hips.hipPosition;
            set => hips = new HipsKinematics(value, hips.velocity, hips.acceleration);
        }

        public GaitFrame() { }

        public GaitFrame(
            int frame,
            float timeSum,
            DateTime sysTime,
            double sysTimeDiffMs,
            int stepCount,
            double lastStepDurationMs,
            float stepLengthInst,
            float stepLengthAccum,
            float stepHipDistance,
            float strideWidth,
            float strideLengthRight, float strideLengthLeft,
            float strideTimeRight, float strideTimeLeft,
            float legLength,
            GroundState ground,
            JointRotations joints,
            HipsKinematics hips)
        {
            this.frame = frame;
            this.timeSum = timeSum;
            this.sysTime = sysTime;
            this.sysTimeDiffMs = sysTimeDiffMs;
            this.stepCount = stepCount;
            this.lastStepDurationMs = lastStepDurationMs;
            this.stepLengthInst = stepLengthInst;
            this.stepLengthAccum = stepLengthAccum;
            this.stepHipDistance = stepHipDistance;
            this.strideWidth = strideWidth;
            this.strideLengthRight = strideLengthRight;
            this.strideLengthLeft = strideLengthLeft;
            this.strideTimeRight = strideTimeRight;
            this.strideTimeLeft = strideTimeLeft;
            this.legLength = legLength;
            this.ground = ground;
            this.joints = joints;
            this.hips = hips;
        }

        public float StepLengthRatio(float epsilon = 1e-4f) =>
            stepLengthInst / Mathf.Max(epsilon, legLength);

        public float StrideWidthRatio(float epsilon = 1e-4f) =>
            strideWidth / Mathf.Max(epsilon, legLength);

        public float StrideLengthRightRatio(float epsilon = 1e-4f) =>
            strideLengthRight / Mathf.Max(epsilon, legLength);

        public float StrideLengthLeftRatio(float epsilon = 1e-4f) =>
            strideLengthLeft / Mathf.Max(epsilon, legLength);

        public float VelocityRatio(float epsilon = 1e-4f) =>
            hips.velocity / Mathf.Max(epsilon, legLength);

        public float AccelerationRatio(float epsilon = 1e-4f) =>
            hips.acceleration / Mathf.Max(epsilon, legLength);
    }
}
