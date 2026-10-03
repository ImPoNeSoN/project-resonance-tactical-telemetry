using Resonance.Sim.Combat;
using Resonance.Sim.Data;

namespace Resonance.Sim.Sim;

/// <summary>
/// Plain-text resonance snapshot for the debug scene. The sim stays free of Godot types;
/// <c>SimBridge</c> forwards this string on <c>ResonanceChanged</c>.
/// </summary>
public static class ResonanceReadout
{
    public static string Summarize(BattleSimulator sim)
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            BossPartState part = sim.Boss.Parts[i];
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append(part.Name).Append(": ");
            if (part.Tier == 1 && sim.Tick < part.ChainExpires)
            {
                int left = part.ChainExpires - sim.Tick;
                text.Append("L1 ").Append(part.Property).Append(" (").Append(left).Append(" ticks)");
            }
            else if (part.Tier == 2 && sim.Tick < part.ChainExpires)
            {
                int left = part.ChainExpires - sim.Tick;
                text.Append("L2 ").Append(part.Resonance).Append(" (").Append(left).Append(" ticks)");
            }
            else
            {
                text.Append("empty");
            }

            if (part.BurstActive && sim.Tick < part.BurstExpires)
            {
                text.Append(" | burst ").Append(part.BurstMask).Append(" (").Append(part.BurstExpires - sim.Tick).Append(" ticks)");
            }
            else
            {
                text.Append(" | no burst");
            }

            text.Append(" | next: ").Append(ResonanceTable.NextLinks(part.Tier, part.Property, part.Resonance));
            string resist = ResistLine(part);
            if (resist.Length > 0)
            {
                text.Append(" | ").Append(resist);
            }

            if (part.Pressure.Length > 0)
            {
                text.Append(" | ").Append(part.Pressure);
            }

            if (part.DefBonus != 0)
            {
                text.Append(" | DEF ").Append(sim.Boss.Def + part.DefBonus);
            }
        }

        return text.ToString();
    }

    public static string ResistLine(BossPartState part)
    {
        if (part.ResistBp == null)
        {
            return "";
        }

        var text = new System.Text.StringBuilder();
        for (int i = 0; i < part.ResistBp.Length && i < 8; i++)
        {
            int bp = part.ResistBp[i];
            if (bp == 0)
            {
                continue;
            }

            if (text.Length > 0)
            {
                text.Append(", ");
            }

            string sign = bp > 0 ? "+" : "−";
            int shown = bp < 0 ? -bp : bp;
            text.Append((ElementId)i).Append(' ').Append(sign).Append(shown / 100).Append('%');
        }

        return text.ToString();
    }
}
