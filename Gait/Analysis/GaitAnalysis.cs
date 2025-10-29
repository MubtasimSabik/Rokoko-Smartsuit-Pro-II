// Purpose: Orchestrates data collection each frame and writes CSV on quit

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Gait.Core;
using Gait.Metrics;
using Gait.Recording;

namespace Gait.Analysis
{
    public class GaitAnalysisController : MonoBehaviour
    {
        [Header("Output")]
        public string outputDirectory = "F:/GaGa/GaitData/";

        [Header("Layer")]
        public LayerMask groundLayer;

        [Header("Rig")]
        public GaitRig rig = new GaitRig();

        // calculators
        private readonly StepCounter stepCounter = new StepCounter();
        private readonly StrideCalculator stride = new StrideCalculator();
        private readonly HipsTracker hipsTracker = new HipsTracker();

        // state
        private DateTime prevSysTime;
        private float timeSum;
        private bool firstFrame = true;
        private Vector3 lastStepHipPos;
        private float stepLengthAccumulator;
        private float prevStepLengthInstant;

        // collections
        private readonly List<GaitFrame> frames = new List<GaitFrame>(8192);

        private void Start()
        {
            if (!rig.ResolveFrom(transform, out var resolveMsg))
            {
                Debug.LogError($"GaitAnalysis: Rig resolve failed. {resolveMsg}", this);
                enabled = false;
                return;
            }

            prevSysTime = DateTime.Now;
            timeSum = 0f;
            lastStepHipPos = rig.hips.position;
            prevStepLengthInstant = 0f;

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                var dir = Path.GetDirectoryName(Path.Combine(outputDirectory, "output.csv"));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }
        }

        private void Update()
        {
            timeSum += Time.deltaTime;

            var now = DateTime.Now;
            double sysDeltaMs = (now - prevSysTime).TotalMilliseconds;
            prevSysTime = now;

            // Ground states
            bool rightGround = Ground.IsFootGrounded(rig.rightFoot, groundLayer);
            bool leftGround  = Ground.IsFootGrounded(rig.leftFoot, groundLayer);

            // Stride init/update
            if (firstFrame)
            {
                stride.Init(rig.rightFoot.position, rig.leftFoot.position, rightGround, leftGround, Time.time);
                firstFrame = false;
            }
            else
            {
                stride.Update(rig.rightFoot.position, rig.leftFoot.position, rightGround, leftGround, Time.time);
            }

            // Step counting & per-step timers
            bool stepChanged = stepCounter.OnGroundState(leftGround, rightGround, now, out double lastStepDurationMs);

            // Instant metrics
            float stepLengthInstant = Vector3.Distance(rig.rightFoot.position, rig.leftFoot.position);
            float strideWidth = Mathf.Abs(rig.rightFoot.position.x - rig.leftFoot.position.x);

            // Accumulate during step; commit on change
            if (frames.Count == 0) prevStepLengthInstant = stepLengthInstant;
            stepLengthAccumulator += Mathf.Abs(stepLengthInstant - prevStepLengthInstant);
            prevStepLengthInstant = stepLengthInstant;

            float stepLengthAccumToWrite = -1f;
            float stepHipDistanceToWrite = -1f;
            if (stepChanged)
            {
                stepLengthAccumToWrite = stepLengthAccumulator;
                stepLengthAccumulator = 0f;

                // XZ plane hip travel since last step change
                Vector3 hipsXZ = new Vector3(rig.hips.position.x, 0f, rig.hips.position.z);
                Vector3 lastXZ = new Vector3(lastStepHipPos.x, 0f, lastStepHipPos.z);
                stepHipDistanceToWrite = Vector3.Distance(hipsXZ, lastXZ);
                lastStepHipPos = rig.hips.position;
            }

            // Hips kinematics on XZ plane
            hipsTracker.Update(rig.hips);

            // Rotations snapshot 
            var joints = new JointRotations
            {
                rightThigh = rig.rightThigh.rotation,
                leftThigh = rig.leftThigh.rotation,
                rightShin = rig.rightShin.rotation,
                leftShin = rig.leftShin.rotation,
                rightShoulder = rig.rightShoulder.rotation,
                leftShoulder = rig.leftShoulder.rotation,
                head = rig.head.rotation
            };

            // Frame record
            frames.Add(new GaitFrame
            {
                frame = frames.Count,
                timeSum = timeSum,
                sysTime = now,
                sysTimeDiffMs = sysDeltaMs,
                stepCount = stepCounter.stepCount,
                lastStepDurationMs = stepChanged ? lastStepDurationMs : -1,
                stepLengthInst = stepLengthInstant,
                stepLengthAccum = stepLengthAccumToWrite,
                stepHipDistance = stepHipDistanceToWrite,
                strideWidth = strideWidth,
                strideLengthRight = stride.strideLenRight,
                strideLengthLeft  = stride.strideLenLeft,
                strideTimeRight = stride.strideTimeRight,
                strideTimeLeft  = stride.strideTimeLeft,
                legLength = rig.legLength,
                ground = new GroundState(leftGround, rightGround),
                joints = joints,
                hips = new HipsKinematics
                (
                    hipPosition: hipsTracker.CurrentPosXZ,   
                    velocity:    hipsTracker.VelocityMag,
                    acceleration:hipsTracker.AccelMag
                )
            });
        }

        private void OnApplicationQuit()
        {
            if (frames.Count == 0) return;

            try
            {
                string nameSafe = string.IsNullOrEmpty(gameObject.name) ? "GaitSession" : gameObject.name;
                string file = Path.Combine(outputDirectory, $"{nameSafe}.csv");
                CsvExporter.Write(file, frames);
                Debug.Log($"GaitAnalysis: wrote {frames.Count} frames to {file}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GaitAnalysis: Failed to write CSV. {ex.Message}", this);
            }
            finally
            {
                frames.Clear(); // free memory 
            }
        }
    }
}
