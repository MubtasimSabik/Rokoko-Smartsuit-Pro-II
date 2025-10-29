// Purpose: CSV writer with invariant culture; column names match written data exactly.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Gait.Core;

namespace Gait.Recording
{
    public static class CsvExporter
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly string Header =
            "DateTime,DateTimeDiffMs,Frame,TimeSum,SysTimestamp,StepCount,LastStepMs," +
            "RightFootGround,LeftFootGround,StepLengthAccum," +
            "HipPosX,HipPosY,HipPosZ," +
            "StepHipDistance,LegLength," +
            "StepLength,StepLengthRatio," +
            "StrideWidth,StrideWidthRatio," +
            "StrideLengthRight,StrideLengthRightRatio,StrideTimeRight," +
            "StrideLengthLeft,StrideLengthLeftRatio,StrideTimeLeft," +
            "Velocity,VelocityRatio,Acceleration,AccelerationRatio," +
            "RightThighRotationX,RightThighRotationY,RightThighRotationZ,RightThighRotationW," +
            "LeftThighRotationX,LeftThighRotationY,LeftThighRotationZ,LeftThighRotationW," +
            "RightShinRotationX,RightShinRotationY,RightShinRotationZ,RightShinRotationW," +
            "LeftShinRotationX,LeftShinRotationY,LeftShinRotationZ,LeftShinRotationW," +
            "RightShoulderRotationX,RightShoulderRotationY,RightShoulderRotationZ,RightShoulderRotationW," +
            "LeftShoulderRotationX,LeftShoulderRotationY,LeftShoulderRotationZ,LeftShoulderRotationW," +
            "HeadRotationX,HeadRotationY,HeadRotationZ,HeadRotationW";

        // Main entry
        public static void Write(string fullPath, System.Collections.Generic.IReadOnlyList<GaitFrame> frames)
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            // UTF-8 
            using var writer = new StreamWriter(fullPath, append: false, encoding: new UTF8Encoding(false));
            Write(writer, frames);
        }

        // In-memory / test entry
        public static void Write(TextWriter writer, System.Collections.Generic.IReadOnlyList<GaitFrame> frames)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (frames == null) throw new ArgumentNullException(nameof(frames));

            writer.WriteLine(Header);

            // Heuristic per-row capacity to minimize reallocs
            var sb = new StringBuilder(capacity: 2048);

            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                sb.Clear();

                // --- Time block ---
                // ISO 8601 with offset (round-trippable)
                Append(sb, f.sysTime.ToString("O", Invariant)); sb.Append(',');
                Append(sb, f.sysTimeDiffMs); sb.Append(',');
                sb.Append(f.frame.ToString(Invariant)); sb.Append(',');
                Append(sb, f.timeSum, "F6"); sb.Append(',');
                // Unix epoch seconds (double, UTC)
                var epochSeconds = f.sysTime.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
                Append(sb, epochSeconds, "F6"); sb.Append(',');

                // --- Steps / flags ---
                sb.Append(f.stepCount.ToString(Invariant)); sb.Append(',');
                Append(sb, f.lastStepDurationMs); sb.Append(',');
                sb.Append(f.ground.rightGrounded ? "1," : "0,");
                sb.Append(f.ground.leftGrounded  ? "1," : "0,");

                // --- Per-step accumulators ---
                Append(sb, f.stepLengthAccum); sb.Append(',');

                // --- Hips (GaitAnalysis: XZ in position.y==0f, still XYZ) ---
                Append(sb, f.hips.hipPosition.x, "F6"); sb.Append(',');
                Append(sb, f.hips.hipPosition.y, "F6"); sb.Append(',');
                Append(sb, f.hips.hipPosition.z, "F6"); sb.Append(',');

                Append(sb, f.stepHipDistance); sb.Append(',');

                // --- Lengths & ratios (use helpers for consistency) ---
                Append(sb, f.legLength, "F6"); sb.Append(',');

                Append(sb, f.stepLengthInst, "F6"); sb.Append(',');
                Append(sb, f.StepLengthRatio(), "F6"); sb.Append(',');

                Append(sb, f.strideWidth, "F6"); sb.Append(',');
                Append(sb, f.StrideWidthRatio(), "F6"); sb.Append(',');

                Append(sb, f.strideLengthRight, "F6"); sb.Append(',');
                Append(sb, f.StrideLengthRightRatio(), "F6"); sb.Append(',');
                Append(sb, f.strideTimeRight, "F6"); sb.Append(',');

                Append(sb, f.strideLengthLeft, "F6"); sb.Append(',');
                Append(sb, f.StrideLengthLeftRatio(), "F6"); sb.Append(',');
                Append(sb, f.strideTimeLeft, "F6"); sb.Append(',');

                // --- Kinematics & ratios ---
                Append(sb, f.hips.velocity, "F6"); sb.Append(',');
                Append(sb, f.VelocityRatio(), "F6"); sb.Append(',');
                Append(sb, f.hips.acceleration, "F6"); sb.Append(',');
                Append(sb, f.AccelerationRatio(), "F6"); sb.Append(',');

                // --- Quaternions: order = x,y,z,w per joint ---
                // Right thigh
                Append(sb, f.joints.rightThigh.x, "F6"); sb.Append(',');
                Append(sb, f.joints.rightThigh.y, "F6"); sb.Append(',');
                Append(sb, f.joints.rightThigh.z, "F6"); sb.Append(',');
                Append(sb, f.joints.rightThigh.w, "F6"); sb.Append(',');

                // Left thigh
                Append(sb, f.joints.leftThigh.x, "F6"); sb.Append(',');
                Append(sb, f.joints.leftThigh.y, "F6"); sb.Append(',');
                Append(sb, f.joints.leftThigh.z, "F6"); sb.Append(',');
                Append(sb, f.joints.leftThigh.w, "F6"); sb.Append(',');

                // Right shin
                Append(sb, f.joints.rightShin.x, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShin.y, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShin.z, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShin.w, "F6"); sb.Append(',');

                // Left shin
                Append(sb, f.joints.leftShin.x, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShin.y, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShin.z, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShin.w, "F6"); sb.Append(',');

                // Right shoulder
                Append(sb, f.joints.rightShoulder.x, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShoulder.y, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShoulder.z, "F6"); sb.Append(',');
                Append(sb, f.joints.rightShoulder.w, "F6"); sb.Append(',');

                // Left shoulder
                Append(sb, f.joints.leftShoulder.x, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShoulder.y, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShoulder.z, "F6"); sb.Append(',');
                Append(sb, f.joints.leftShoulder.w, "F6"); sb.Append(',');

                // Head 
                Append(sb, f.joints.head.x, "F6"); sb.Append(',');
                Append(sb, f.joints.head.y, "F6"); sb.Append(',');
                Append(sb, f.joints.head.z, "F6"); sb.Append(',');
                Append(sb, f.joints.head.w, "F6");

                writer.WriteLine(sb);
            }
        }

        private static void Append(StringBuilder sb, float value, string? numericFormat = null)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { sb.Append(""); return; }
            if (numericFormat == null) sb.Append(value.ToString(Invariant));
            else sb.Append(value.ToString(numericFormat, Invariant));
        }

        private static void Append(StringBuilder sb, double value, string? numericFormat = null)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) { sb.Append(""); return; }
            if (numericFormat == null) sb.Append(value.ToString(Invariant));
            else sb.Append(value.ToString(numericFormat, Invariant));
        }
    }
}
