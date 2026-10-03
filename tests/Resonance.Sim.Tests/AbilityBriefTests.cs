using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class AbilityBriefTests
{
    [Fact]
    public void Every_preset_ability_has_a_description()
    {
        var seen = new HashSet<int>();
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            BattleSimulator sim = CarapaceEncounter.Create(preset, 1UL);
            foreach (HeroState hero in sim.Heroes)
            {
                foreach (int id in hero.Kit)
                {
                    if (!seen.Add(id))
                    {
                        continue;
                    }

                    AbilityDef ability = AbilityCatalog.Get(id);
                    Assert.False(string.IsNullOrWhiteSpace(ability.Description), ability.Name);
                    string panel = AbilityBrief.Format(ability);
                    Assert.Contains(ability.Name, panel, StringComparison.Ordinal);
                    Assert.Contains(ability.Description, panel, StringComparison.Ordinal);
                    Assert.Contains($"MP {ability.MpCost}", panel, StringComparison.Ordinal);
                    Assert.Contains($"AP {ability.RecoveryAp}", panel, StringComparison.Ordinal);
                }
            }
        }

        Assert.True(seen.Count >= 20);
    }

    [Fact]
    public void Panel_reads_power_interrupt_and_chain_links_from_the_definitions()
    {
        string bash = AbilityBrief.Format(AbilityCatalog.Get(AbilityCatalog.ShieldBash));
        Assert.Contains("Target: single part", bash, StringComparison.Ordinal);
        Assert.Contains("Power: physical, about 80% ATK", bash, StringComparison.Ordinal);
        Assert.Contains("flat interrupt 35% on the casting part", bash, StringComparison.Ordinal);
        Assert.Contains("Darkness→Distortion (purges Kiln Guard)", bash, StringComparison.Ordinal);

        string blizzard = AbilityBrief.Format(AbilityCatalog.Get(AbilityCatalog.BlizzardII));
        Assert.Contains("Property: Ice", blizzard, StringComparison.Ordinal);
        Assert.Contains("Power: magical, about 300% INT", blizzard, StringComparison.Ordinal);
        Assert.Contains("Charge: 1000 tick chant", blizzard, StringComparison.Ordinal);
        Assert.Contains("Piercing→Induration", blizzard, StringComparison.Ordinal);

        string finisher = AbilityBrief.Format(AbilityCatalog.Get(AbilityCatalog.RavelExecution));
        Assert.Contains("Power: physical, about 520% ATK", finisher, StringComparison.Ordinal);
        Assert.Contains("Tier-3 finisher", finisher, StringComparison.Ordinal);
        Assert.Contains("Slashing→Fragmentation", finisher, StringComparison.Ordinal);
        Assert.Contains("Threat: VE 0 · CE 1500", finisher, StringComparison.Ordinal);

        string heal = AbilityBrief.Format(AbilityCatalog.Get(AbilityCatalog.CureCascade));
        Assert.Contains("Target: ally", heal, StringComparison.Ordinal);
        Assert.Contains("Power: heal, about 320% INT", heal, StringComparison.Ordinal);

        string party = AbilityBrief.Format(AbilityCatalog.Get(AbilityCatalog.PhaseSanctuary));
        Assert.Contains("Target: all", party, StringComparison.Ordinal);
        Assert.Contains("3,000 ticks", party, StringComparison.Ordinal);
    }
}
