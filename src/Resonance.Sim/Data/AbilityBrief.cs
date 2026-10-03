using System.Collections.Generic;
using System.Text;
using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Data;

/// <summary>
/// Grey-box ability panel. The effect line is computed for the acting hero. The lines under it
/// are read from <see cref="AbilityDef"/> or the resonance table. <see cref="AbilityDef.Description"/>
/// is only the extra tactical note.
/// </summary>
public static class AbilityBrief
{
    public static string Format(
        AbilityDef ability,
        HeroState actor,
        HeroState ally,
        BossState boss,
        int partIndex,
        int tick,
        IReadOnlyList<HeroState> party)
    {
        var text = new StringBuilder();
        text.Append(ability.Name).Append('\n');
        text.Append(AbilityForecast.Effect(ability, actor, ally, boss, partIndex, tick, party)).Append('\n');
        text.Append("MP ").Append(ability.MpCost).Append(" · AP ").Append(ability.RecoveryAp).Append('\n');
        text.Append("Target: ").Append(Target(ability)).Append('\n');
        text.Append("Property: ").Append(Property(ability)).Append('\n');
        text.Append(ChainLine(ability)).Append('\n');
        text.Append(PowerLine(ability)).Append('\n');
        text.Append(HitLine(ability)).Append('\n');
        text.Append(StatusLine(ability)).Append('\n');
        text.Append(ability.ChantTicks > 0
            ? $"Charge: {ability.ChantTicks} tick chant\n"
            : "Charge: instant\n");
        text.Append("Threat: VE ").Append(ability.BaseVe).Append(" · CE ").Append(ability.BaseCe);
        if (!string.IsNullOrWhiteSpace(ability.Description))
        {
            text.Append('\n').Append(ability.Description);
        }

        return text.ToString();
    }

    public static string Target(AbilityDef ability) => ability.Effect switch
    {
        SupportEffect.KeratinBastion or SupportEffect.TimelineStalk => "self",
        SupportEffect.CircuitBenediction => "ally",
        SupportEffect.PhaseSanctuary => "all",
        _ => ability.Kind switch
        {
            AbilityKind.Healing => "ally",
            AbilityKind.Support => "self",
            _ => "single part",
        },
    };

    private static string Property(AbilityDef ability)
    {
        if (ability.Property == ChainProperty.None && ability.Element == ElementId.None)
        {
            return "none";
        }

        if (ability.Element == ElementId.None || ability.Element.ToString() == ability.Property.ToString())
        {
            return ability.Property == ChainProperty.None ? ability.Element.ToString() : ability.Property.ToString();
        }

        return $"{ability.Property} · element {ability.Element}";
    }

    private static string ChainLine(AbilityDef ability)
    {
        if (ability.Property == ChainProperty.None)
        {
            return "Chain: none";
        }

        var closes = new StringBuilder();
        var opens = new StringBuilder();
        for (int i = 0; i <= (int)ChainProperty.Darkness; i++)
        {
            var other = (ChainProperty)i;
            AppendLink(closes, other, ResonanceTable.LookupL2(other, ability.Property));
            AppendLink(opens, other, ResonanceTable.LookupL2(ability.Property, other));
        }

        var apexes = new StringBuilder();
        for (int r = 1; r <= (int)ResonanceId.Radiance; r++)
        {
            var open = (ResonanceId)r;
            ApexId apex = ResonanceTable.LookupL3(open, ability.Property);
            if (apex == ApexId.None)
            {
                continue;
            }

            if (apexes.Length > 0)
            {
                apexes.Append(", ");
            }

            apexes.Append(Words(open.ToString())).Append("→").Append(Words(apex.ToString()));
        }

        var text = new StringBuilder("Chain:");
        if (closes.Length > 0)
        {
            text.Append(" closes ").Append(closes);
        }

        if (opens.Length > 0)
        {
            text.Append(closes.Length > 0 ? ". Opens " : " opens ").Append(opens);
        }

        if (apexes.Length > 0)
        {
            text.Append(closes.Length > 0 || opens.Length > 0 ? ". Apex " : " apex ").Append(apexes);
        }

        if (closes.Length == 0 && opens.Length == 0 && apexes.Length == 0)
        {
            text.Append(" no link");
        }

        return text.ToString();
    }

    private static void AppendLink(StringBuilder text, ChainProperty other, ResonanceId resonance)
    {
        if (resonance == ResonanceId.None)
        {
            return;
        }

        if (text.Length > 0)
        {
            text.Append(", ");
        }

        text.Append(other).Append("→").Append(Words(resonance.ToString()));
        if (resonance == ResonanceId.Distortion)
        {
            text.Append(" (purges ").Append(SimConst.KilnGuardName).Append(')');
        }
    }

    private static string PowerLine(AbilityDef ability)
    {
        if (ability.MultiplierBp <= 0)
        {
            return ability.Kind == AbilityKind.Enmity ? "Power: no damage (enmity)" : "Power: no damage";
        }

        int percent = ability.MultiplierBp / 100;
        (string kind, string stat) = ability.Kind switch
        {
            AbilityKind.Physical => ("physical", "ATK"),
            AbilityKind.ElementalPhysical => ("elemental physical", "ATK"),
            AbilityKind.Magical => ("magical", "INT"),
            AbilityKind.Healing => ("heal", "INT"),
            AbilityKind.Support => ("support", "INT"),
            _ => ("effect", "ATK"),
        };
        return $"Power: {kind}, about {percent}% {stat}";
    }

    private static string HitLine(AbilityDef ability) =>
        ability.UsesWeaponLoadout
            ? "Hits: 1, plus double or triple attack from weapon gear"
            : "Hits: 1";

    private static string StatusLine(AbilityDef ability)
    {
        var text = new StringBuilder("Status: ");
        bool any = false;
        if (ability.AppliesBurn)
        {
            text.Append("Burn for ").Append(SimConst.BurnDuration).Append(" ticks");
            any = true;
        }

        string effect = ability.Effect switch
        {
            SupportEffect.KeratinBastion => "−35% physical damage taken for 3,000 ticks",
            SupportEffect.TimelineStalk => "next recovery −4,000 AP for 6,000 ticks or until used",
            SupportEffect.CircuitBenediction => $"+20 Concentration and regen {ability.MultiplierBp / 100}% INT per 500 ticks for 4,000 ticks",
            SupportEffect.PhaseSanctuary => "−25% damage taken for the whole party for 3,000 ticks",
            _ => "",
        };
        if (effect.Length > 0)
        {
            if (any)
            {
                text.Append("; ");
            }

            text.Append(effect);
            any = true;
        }

        if (ability.FlatInterruptBp > 0)
        {
            if (any)
            {
                text.Append("; ");
            }

            text.Append("flat interrupt ").Append(ability.FlatInterruptBp / 100).Append("% on the casting part");
            any = true;
        }

        if (!any)
        {
            text.Append("none");
        }

        return text.ToString();
    }

    private static string Words(string raw)
    {
        var text = new StringBuilder(raw.Length + 4);
        for (int i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1]))
            {
                text.Append(' ');
            }

            text.Append(raw[i]);
        }

        return text.ToString();
    }
}
