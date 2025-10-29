// Purpose: Core maths previously inside Update(); now isolated & reusable

using System;
using UnityEngine;

namespace Gait.Metrics
{
    public static class Ground
    {
        public static bool IsFootGrounded(Transform foot, LayerMask groundLayer, float rayLength = 0.1f, float upOffset = 0.01f)
        {
            if (!foot) return false;

            Vector3 origin = foot.position + Vector3.up * upOffset;

            return Physics.Raycast(origin, Vector3.down, rayLength + upOffset, groundLayer, QueryTriggerInteraction.Ignore);
        }
    }

    [Serializable]
    public class StepCounter
    {
        
        public int stepCount;
        public bool stepGate = true; // open when both feet share same state
        public DateTime lastStepTime = DateTime.MinValue;

        private readonly double minStepIntervalMs;

        public StepCounter(double minStepIntervalMs = 120.0) // ~0.12s default
        {
            this.minStepIntervalMs = Mathf.Max(0f, (float)minStepIntervalMs);
        }

        public void Reset(DateTime now)
        {
            stepCount = 0;
            stepGate = true;
            lastStepTime = now;
        }

        public bool OnGroundState(bool left, bool right, DateTime now, out double lastStepDurationMs)
        {
            bool xor = left ^ right;
            bool stepped = false;
            lastStepDurationMs = -1;

            if (xor && stepGate)
            {
                if (lastStepTime == DateTime.MinValue) lastStepTime = now;

                double dt = (now - lastStepTime).TotalMilliseconds;
                if (dt >= minStepIntervalMs)
                {
                    stepCount++;
                    stepped = true;
                    lastStepDurationMs = dt;
                    lastStepTime = now;
                }
            }

            stepGate = !xor;
            return stepped;
        }
    }

    [Serializable]
    public class StrideCalculator
    {
        
        public float strideLenRight, strideLenLeft; // meters since last lift-off to touch-down
        public float strideTimeRight, strideTimeLeft; // seconds

        // internals
        bool prevRightGround, prevLeftGround;
        Vector3 rightPrevPos, leftPrevPos;
        float startTimeRight, startTimeLeft;
        bool initialised = false;

        public void Reset()
        {
            prevRightGround = prevLeftGround = false;
            rightPrevPos = leftPrevPos = Vector3.zero;
            strideLenRight = strideLenLeft = 0f;
            strideTimeRight = strideTimeLeft = 0f;
            startTimeRight = startTimeLeft = 0f;
            initialised = false;
        }

        public void Init(Vector3 rightFootPos, Vector3 leftFootPos, bool rightGround, bool leftGround, float time)
        {
            rightPrevPos = rightFootPos;
            leftPrevPos = leftFootPos;
            prevRightGround = rightGround;
            prevLeftGround = leftGround;
            startTimeRight = time;
            startTimeLeft  = time;
            strideLenRight = strideLenLeft = 0f;
            strideTimeRight = strideTimeLeft = 0f;
            initialised = true;
        }

        public void Update(Vector3 rightFootPos, Vector3 leftFootPos, bool rightGround, bool leftGround, float time)
        {
            if (!initialised)
            {
                Init(rightFootPos, leftFootPos, rightGround, leftGround, time);
                return;
            }

            // Right foot
            if (!prevRightGround && rightGround)
            {
                // touch-down: finalize stride from last lift-off
                strideLenRight  = Vector3.Distance(rightFootPos, rightPrevPos);
                strideTimeRight = Mathf.Max(0f, time - startTimeRight);
            }
            if (prevRightGround && !rightGround)
            {
                // lift-off: mark start of next stride
                rightPrevPos = rightFootPos;
                startTimeRight = time;
            }
            prevRightGround = rightGround;

            // Left foot
            if (!prevLeftGround && leftGround)
            {
                strideLenLeft  = Vector3.Distance(leftFootPos, leftPrevPos);
                strideTimeLeft = Mathf.Max(0f, time - startTimeLeft);
            }
            if (prevLeftGround && !leftGround)
            {
                leftPrevPos = leftFootPos;
                startTimeLeft = time;
            }
            prevLeftGround = leftGround;
        }
    }

    [Serializable]
    public class HipsTracker
    {
        Vector3 prevPosXZ;
        Vector3 prevVel;
        bool hasPrev;

        public Vector3 CurrentPosXZ { get; private set; }
        public float VelocityMag { get; private set; }
        public float AccelMag { get; private set; }

        public void Reset()
        {
            prevPosXZ = Vector3.zero;
            prevVel = Vector3.zero;
            CurrentPosXZ = Vector3.zero;
            VelocityMag = 0f;
            AccelMag = 0f;
            hasPrev = false;
        }

        public void Update(Transform hips)
        {
            Vector3 posXZ = hips ? new Vector3(hips.position.x, 0f, hips.position.z) : Vector3.zero;

            if (!hasPrev)
            {
                prevPosXZ = posXZ;
                prevVel = Vector3.zero;
                CurrentPosXZ = posXZ;
                VelocityMag = 0f;
                AccelMag = 0f;
                hasPrev = true;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 1e-6f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                // freeze kinematics on degenerate dt to avoid NaNs
                CurrentPosXZ = posXZ;
                return;
            }

            Vector3 vel = (posXZ - prevPosXZ) / dt;
            Vector3 acc = (vel - prevVel) / dt;

            CurrentPosXZ = posXZ;
            VelocityMag = vel.magnitude;
            AccelMag = acc.magnitude;

            prevPosXZ = posXZ;
            prevVel = vel;
        }
    }
}
