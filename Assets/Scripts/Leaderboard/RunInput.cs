using System;

namespace Leaderboard
{
    public enum RunInputKind { Look, Freeze, Dash, Shot }

    [Serializable]
    public struct RunInput
    {
        public RunInputKind kind;
        public float x;
        public float y;
        public bool pressed;

        public bool IsValid => kind >= RunInputKind.Look && kind <= RunInputKind.Shot
            && RunScore.IsFinite(x) && RunScore.IsFinite(y);
    }

    // Managed representation shared by the probe and EditMode tests.
    [Serializable]
    public sealed class RunCheckpoint
    {
        public float x, y, vx, vy, rotation, angularVelocity, dashCooldown;
        public int hp;
        public bool alive, frozen, dashing;
        public int[] upgrades;

        public bool IsValid => RunScore.IsFinite(x) && RunScore.IsFinite(y)
            && RunScore.IsFinite(vx) && RunScore.IsFinite(vy) && RunScore.IsFinite(rotation)
            && RunScore.IsFinite(angularVelocity) && RunScore.IsFinite(dashCooldown)
            && upgrades != null && upgrades.Length <= 1024;

        public string Difference(RunCheckpoint actual)
        {
            if (!IsValid || actual == null || !actual.IsValid) return "invalid checkpoint";
            if (Math.Abs(x - actual.x) > 0.001 || Math.Abs(y - actual.y) > 0.001) return "position";
            if (Math.Abs(vx - actual.vx) > 0.001 || Math.Abs(vy - actual.vy) > 0.001) return "velocity";
            double angle = ((double)rotation - actual.rotation) % 360;
            angle = Math.Min(Math.Abs(angle), 360 - Math.Abs(angle));
            if (angle > 0.01 || Math.Abs(angularVelocity - actual.angularVelocity) > 0.01) return "rotation";
            if (hp != actual.hp || alive != actual.alive) return "health/alive";
            if (frozen != actual.frozen || dashing != actual.dashing
                || Math.Abs(dashCooldown - actual.dashCooldown) > 0.001) return "movement state";
            if (upgrades.Length != actual.upgrades.Length) return "upgrades";
            for (int i = 0; i < upgrades.Length; i++)
                if (upgrades[i] != actual.upgrades[i]) return "upgrades/order";
            return null;
        }
    }
}
