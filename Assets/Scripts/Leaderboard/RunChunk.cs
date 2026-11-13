using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Leaderboard
{
    public static class RunEventTypes
    {
        public const string Damage = "damage";
        public const string Heal = "heal";
        public const string Upgrade = "upgrade";
        public const string PauseStart = "pauseStart";
        public const string PauseEnd = "pauseEnd";
        public const string Death = "death";
    }

    public sealed class RunEventRecord
    {
        public string type;
        public long tick;
        public int? amount;
        public int? upgradeId;
    }

    public sealed class RunInputHistogram
    {
        public int look, freeze, dash, shot;

        public void Add(RunInputKind kind)
        {
            switch (kind)
            {
                case RunInputKind.Look: look++; break;
                case RunInputKind.Freeze: freeze++; break;
                case RunInputKind.Dash: dash++; break;
                case RunInputKind.Shot: shot++; break;
            }
        }
    }

    // A RunCheckpoint sample plus the run tick it was taken at.
    public sealed class RunCheckpointEntry
    {
        public long tick;
        public RunCheckpoint state;
    }

    // One POST /runs/{id}/chunks body. hmac and hash are filled by RunHashChain
    // when the chunk is sealed; hash never goes on the wire.
    public sealed class RunChunk
    {
        public int seq;
        public long tick;
        public List<RunCheckpointEntry> checkpoints = new List<RunCheckpointEntry>();
        public List<RunEventRecord> events = new List<RunEventRecord>();
        public RunInputHistogram inputHistogram = new RunInputHistogram();
        public string prevHash;
        public string hmac;
        public string hash;
    }

    // Canonical JSON identical to the Deno side: object keys sorted, optional
    // fields omitted, numbers in ECMAScript Number::toString form. The bytes
    // produced here are exactly what both peers hash and HMAC-sign.
    public static class RunChunkJson
    {
        public static string BodyToCanonical(RunChunk chunk)
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');
            sb.Append("\"checkpoints\":").Append(Checkpoints(chunk.checkpoints));
            sb.Append(",\"events\":").Append(Events(chunk.events));
            sb.Append(",\"inputHistogram\":").Append(Histogram(chunk.inputHistogram));
            sb.Append(",\"seq\":").Append(chunk.seq);
            sb.Append(",\"tick\":").Append(chunk.tick);
            sb.Append('}');
            return sb.ToString();
        }

        public static string ChunkToCanonical(RunChunk chunk)
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');
            sb.Append("\"checkpoints\":").Append(Checkpoints(chunk.checkpoints));
            sb.Append(",\"events\":").Append(Events(chunk.events));
            sb.Append(",\"hmac\":").Append(Quoted(chunk.hmac));
            sb.Append(",\"inputHistogram\":").Append(Histogram(chunk.inputHistogram));
            sb.Append(",\"prevHash\":").Append(Quoted(chunk.prevHash));
            sb.Append(",\"seq\":").Append(chunk.seq);
            sb.Append(",\"tick\":").Append(chunk.tick);
            sb.Append('}');
            return sb.ToString();
        }

        private static string Checkpoints(List<RunCheckpointEntry> list)
        {
            var sb = new StringBuilder(list.Count * 128 + 2);
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var c = list[i].state;
                sb.Append('{');
                sb.Append("\"alive\":").Append(c.alive ? "true" : "false");
                sb.Append(",\"angularVelocity\":").Append(Q(c.angularVelocity));
                sb.Append(",\"dashCooldown\":").Append(Q(c.dashCooldown));
                sb.Append(",\"dashing\":").Append(c.dashing ? "true" : "false");
                sb.Append(",\"frozen\":").Append(c.frozen ? "true" : "false");
                sb.Append(",\"hp\":").Append(c.hp);
                sb.Append(",\"rotation\":").Append(Q(c.rotation));
                sb.Append(",\"tick\":").Append(list[i].tick);
                sb.Append(",\"upgrades\":[");
                for (int u = 0; u < c.upgrades.Length; u++)
                {
                    if (u > 0) sb.Append(',');
                    sb.Append(c.upgrades[u]);
                }
                sb.Append(']');
                sb.Append(",\"vx\":").Append(Q(c.vx));
                sb.Append(",\"vy\":").Append(Q(c.vy));
                sb.Append(",\"x\":").Append(Q(c.x));
                sb.Append(",\"y\":").Append(Q(c.y));
                sb.Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string Events(List<RunEventRecord> list)
        {
            var sb = new StringBuilder(list.Count * 64 + 2);
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var e = list[i];
                sb.Append('{');
                bool first = true;
                if (e.amount.HasValue)
                {
                    sb.Append("\"amount\":").Append(e.amount.Value);
                    first = false;
                }
                if (!first) sb.Append(',');
                sb.Append("\"tick\":").Append(e.tick);
                sb.Append(",\"type\":").Append(Quoted(e.type));
                if (e.upgradeId.HasValue) sb.Append(",\"upgradeId\":").Append(e.upgradeId.Value);
                sb.Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string Histogram(RunInputHistogram h)
        {
            var sb = new StringBuilder(48);
            sb.Append('{');
            bool first = true;
            AppendKind(sb, ref first, "dash", h.dash);
            AppendKind(sb, ref first, "freeze", h.freeze);
            AppendKind(sb, ref first, "look", h.look);
            AppendKind(sb, ref first, "shot", h.shot);
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendKind(StringBuilder sb, ref bool first, string name, int count)
        {
            if (count <= 0) return;
            if (!first) sb.Append(',');
            first = false;
            sb.Append(Quoted(name)).Append(':').Append(count);
        }

        private static string Quoted(string value)
        {
            if (value == null) return "null";
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // Mono formats 16-17 significant digits inexactly, so JS and Unity would
        // disagree on the hashed bytes. Checkpoint values are quantised to 1e-4
        // first; such doubles have short, exactly formattable representations.
        public static string Q(double value) =>
            JsNumber(Math.Round(value, 4, MidpointRounding.AwayFromZero));

        // Mono's "R" is not guaranteed to be the shortest round-trip form, which
        // JS always produces; search the minimal precision explicitly.
        private static string ShortestRoundTrip(double value)
        {
            for (int p = 0; p < 16; p++)
            {
                string s = value.ToString("E" + p, CultureInfo.InvariantCulture);
                if (double.Parse(s, CultureInfo.InvariantCulture) == value) return s;
            }
            return value.ToString("E16", CultureInfo.InvariantCulture);
        }

        // ECMAScript Number::toString: shortest round-trip digits, decimal
        // notation inside [1e-6, 1e21), exponential outside. Must match what
        // JSON.stringify emits on the server for the same parsed value.
        public static string JsNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "null";
            if (value == 0) return "0";

            string shortest = ShortestRoundTrip(value);
            bool negative = shortest[0] == '-';
            if (negative) shortest = shortest.Substring(1);

            string mantissa = shortest;
            int exp = 0;
            int ePos = shortest.IndexOf('E');
            if (ePos < 0) ePos = shortest.IndexOf('e');
            if (ePos >= 0)
            {
                mantissa = shortest.Substring(0, ePos);
                exp = int.Parse(shortest.Substring(ePos + 1), CultureInfo.InvariantCulture);
            }

            string digits;
            int pointPos;
            int dot = mantissa.IndexOf('.');
            if (dot < 0)
            {
                digits = mantissa;
                pointPos = mantissa.Length;
            }
            else
            {
                digits = mantissa.Substring(0, dot) + mantissa.Substring(dot + 1);
                pointPos = dot;
            }
            int k = pointPos + exp; // digits before the decimal point
            while (digits.Length > 1 && digits[0] == '0')
            {
                digits = digits.Substring(1);
                k--;
            }
            int n = digits.Length;

            string body;
            if (n <= k && k <= 21)
            {
                body = digits + new string('0', k - n);
            }
            else if (0 < k && k < n)
            {
                body = digits.Substring(0, k) + "." + digits.Substring(k);
            }
            else if (-6 < k && k <= 0)
            {
                body = "0." + new string('0', -k) + digits;
            }
            else
            {
                int e = k - 1;
                body = n > 1 ? digits[0] + "." + digits.Substring(1) : digits.Substring(0, 1);
                body += e >= 0 ? "e+" + e : "e-" + (-e);
            }
            return negative ? "-" + body : body;
        }
    }
}
