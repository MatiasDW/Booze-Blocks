using System;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    public readonly struct HordePoseSnapshot
    {
        public HordePoseSnapshot(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }

        public Vector3 Position { get; }
        public float Yaw { get; }
    }

    public static class NetworkSnapshotQuantization
    {
        private const float PositionPrecision = 100f;
        private const float TimePrecision = 10f;

        public static short EncodePosition(float value)
        {
            return (short)Math.Clamp((int)Math.Round(value * PositionPrecision), short.MinValue, short.MaxValue);
        }

        public static float DecodePosition(short value)
        {
            return value / PositionPrecision;
        }

        public static ushort EncodeYaw(float value)
        {
            float normalized = Mathf.Repeat(value, 360f) / 360f;
            return (ushort)Math.Clamp((int)Math.Round(normalized * ushort.MaxValue), 0, ushort.MaxValue);
        }

        public static float DecodeYaw(ushort value)
        {
            return value / (float)ushort.MaxValue * 360f;
        }

        public static byte EncodeRatio(float value)
        {
            return (byte)Math.Clamp((int)Math.Round(Mathf.Clamp01(value) * byte.MaxValue), 0, byte.MaxValue);
        }

        public static float DecodeRatio(byte value)
        {
            return value / (float)byte.MaxValue;
        }

        public static ushort EncodeTime(float seconds)
        {
            return (ushort)Math.Clamp((int)Math.Round(Math.Max(0f, seconds) * TimePrecision), 0, ushort.MaxValue);
        }

        public static float DecodeTime(ushort value)
        {
            return value / TimePrecision;
        }
    }
}
