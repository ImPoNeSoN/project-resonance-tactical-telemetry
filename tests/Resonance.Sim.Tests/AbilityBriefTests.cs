using Resonance.Sim.Combat;
using Resonance.Sim.Core;
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
                    string panel = Panel(sim, hero, ability);
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
    public void Every_preset_ability_produces_an_effect_line()
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
                    string panel = Panel(sim, hero, ability);
                    Assert.StartsWith(ability.Name + "\nEffect: ", panel, StringComparison.Ordinal);
                    int effectAt = panel.IndexOf("Effect: ", StringComparison.Ordinal);
                    int mpAt = panel.IndexOf("\nMP ", StringComparison.Ordinal);
                    Assert.True(mpAt > effectAt, ability.Name);
                    string effect = panel[(effectAt + "Effect: ".Length)..mpAt].Trim();
                    Assert.False(string.IsNullOrWhiteSpace(effect), ability.Name);
                }
            }
        }

        Assert.True(seen.Count >= 20);
    }

    [Fact]
    public void Panel_reads_power_interrupt_and_chain_links_from_the_definitions()
    {
        string bash = Panel(AbilityCatalog.ShieldBash);
        Assert.Contains("Target: single part", bash, StringComparison.Ordinal);
        Assert.Contains("Power: physical, about 80% ATK", bash, StringComparison.Ordinal);
        Assert.Contains("flat interrupt 35% on the casting part", bash, StringComparison.Ordinal);
        Assert.Contains("Darkness→Distortion (purges Kiln Guard)", bash, StringComparison.Ordinal);

        string blizzard = Panel(AbilityCatalog.BlizzardII);
        Assert.Contains("Property: Ice", blizzard, StringComparison.Ordinal);
        Assert.Contains("Power: magical, about 300% INT", blizzard, StringComparison.Ordinal);
        Assert.Contains("Charge: 1000 tick chant", blizzard, StringComparison.Ordinal);
        Assert.Contains("Piercing→Induration", blizzard, StringComparison.Ordinal);

        string finisher = Panel(AbilityCatalog.RavelExecution);
        Assert.Contains("Power: physical, about 520% ATK", finisher, StringComparison.Ordinal);
        Assert.Contains("Tier-3 finisher", finisher, StringComparison.Ordinal);
        Assert.Contains("Slashing→Fragmentation", finisher, StringComparison.Ordinal);
        Assert.Contains("Threat: VE 0 · CE 1500", finisher, StringComparison.Ordinal);

        string heal = Panel(AbilityCatalog.CureCascade);
        Assert.Contains("Target: ally", heal, StringComparison.Ordinal);
        Assert.Contains("Power: heal, about 320% INT", heal, StringComparison.Ordinal);

        string party = Panel(AbilityCatalog.PhaseSanctuary);
        Assert.Contains("Target: all", party, StringComparison.Ordinal);
        Assert.Contains("3,000 ticks", party, StringComparison.Ordinal);
    }

    [Fact]
    public void Shield_bash_effect_is_the_modeled_hit_and_interrupt()
    {
        BattleSimulator sim = CarapaceEncounter.Create(2, 1UL);
        HeroState korrith = sim.Heroes[0];
        AbilityDef bash = AbilityCatalog.Get(AbilityCatalog.ShieldBash);
        string panel = Panel(sim, korrith, bash);

        int attack = Formulas.EffectiveAttack(korrith.Atk, 0, korrith.Weapon.WsStr, korrith.Weapon.WsDex, WeaponProperty.Blunt);
        int defense = sim.Boss.Def + sim.Boss.Parts[0].DefBonus;
        var glancing = new DamageRequest
        {
            Power = attack,
            MultiplierBp = bash.MultiplierBp,
            MitigationStat = defense,
            MitigationConstant = SimConst.PhysicalDrConstant,
            CritMultiplierBp = SimConst.Bp,
            BurstDiminishBp = SimConst.Bp,
        };
        var crit = new DamageRequest
        {
            Power = attack,
            MultiplierBp = bash.MultiplierBp,
            MitigationStat = defense,
            MitigationConstant = SimConst.PhysicalDrConstant,
            CritMultiplierBp = Formulas.CritMultiplierBp(korrith.Weapon.WsCritDamageBp),
            BurstDiminishBp = SimConst.Bp,
        };
        int low = DamagePipeline.Resolve(glancing).Total;
        int high = DamagePipeline.Resolve(crit).Total;
        Assert.Contains($"About {low}–{high} damage to Core after DEF.", panel, StringComparison.Ordinal);
        Assert.Contains("35% chance to interrupt a chant on that part when the hit lands.", panel, StringComparison.Ordinal);
        Assert.Contains("No stun.", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("Stun", panel, StringComparison.Ordinal);
        Assert.Contains("Applies Blunt.", panel, StringComparison.Ordinal);
        Assert.Contains("Hit 95%.", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void Circuit_benediction_effect_states_each_regen_pulse()
    {
        BattleSimulator sim = CarapaceEncounter.Create(2, 1UL);
        HeroState healer = sim.Heroes[3];
        HeroState ally = sim.Heroes[2];
        AbilityDef ability = AbilityCatalog.Get(AbilityCatalog.CircuitBenediction);
        string panel = AbilityBrief.Format(ability, healer, ally, sim.Boss, 0, sim.Tick, sim.Heroes);
        int per = FixedMath.MulBp(healer.Intel + healer.MidCast.MidInt, ability.MultiplierBp);
        int pulses = 0;
        for (int next = SimConst.GlobalPhase; next <= 4_000; next += SimConst.GlobalPhase)
        {
            pulses++;
        }

        Assert.Contains(
            $"Restores {per} HP every {SimConst.GlobalPhase} ticks for 4000 ticks (total {per * pulses}) to {ally.Name}.",
            panel,
            StringComparison.Ordinal);
        Assert.Contains("Concentration +20", panel, StringComparison.Ordinal);
    }

    private static string Panel(int abilityId)
    {
        BattleSimulator sim = CarapaceEncounter.Create(2, 1UL);
        AbilityDef ability = AbilityCatalog.Get(abilityId);
        HeroState actor = sim.Heroes[0];
        foreach (HeroState hero in sim.Heroes)
        {
            foreach (int id in hero.Kit)
            {
                if (id == abilityId)
                {
                    actor = hero;
                }
            }
        }

        return Panel(sim, actor, ability);
    }

    private static string Panel(BattleSimulator sim, HeroState actor, AbilityDef ability)
    {
        HeroState ally = sim.Heroes[sim.Heroes.Count > 2 ? 2 : 0];
        return AbilityBrief.Format(ability, actor, ally, sim.Boss, 0, sim.Tick, sim.Heroes);
    }
}
