using UnityEngine;

namespace Generation
{
    public enum MobilityKind
    {
        None,
        HarpoonAnchor,
        DashPushoff,
        SolidPlatform,
        EatDash
    }

    public sealed class ClimbPlan
    {
        public float MobilityBudget { get; }
        public float StepCap { get; }
        public int MinSupports { get; }
        public float RiskShare { get; }
        public float EntryOffset { get; }
        public float ExitOffset { get; }
        public float EdgeReserve { get; }
        public float StartX { get; }
        public float EndX { get; }
        public bool ChainEnabled { get; }

        public ClimbPlan(float mobilityBudget, float stepCap, int minSupports, float riskShare, float entryOffset,
            float exitOffset, float edgeReserve, float startX, float endX, bool chainEnabled)
        {
            MobilityBudget = mobilityBudget;
            StepCap = stepCap;
            MinSupports = minSupports;
            RiskShare = riskShare;
            EntryOffset = entryOffset;
            ExitOffset = exitOffset;
            EdgeReserve = edgeReserve;
            StartX = startX;
            EndX = endX;
            ChainEnabled = chainEnabled;
        }
    }
}
