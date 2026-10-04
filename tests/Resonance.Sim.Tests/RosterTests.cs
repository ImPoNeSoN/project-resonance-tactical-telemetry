using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class RosterTests
{
    [Fact]
    public void Six_heroes_cover_tank_healer_dps_and_every_implemented_race()
    {
        Assert.Equal(6, HeroRoster.Count);
        Assert.Equal(15, HeroRoster.Combinations().Length);
        Assert.Equal(HeroRole.Tank, HeroRoster.Role(HeroRoster.Korrith));
        Assert.Equal(HeroRole.Healer, HeroRoster.Role(HeroRoster.Seraphine));
        Assert.Equal("Veth-Kari", HeroRoster.RaceName(HeroRoster.Korrith));
        Assert.Equal("Sylvari-Mor", HeroRoster.RaceName(HeroRoster.Saeli));
        Assert.Equal("Sylvari-Mor", HeroRoster.RaceName(HeroRoster.Kaelis));
        Assert.Equal("Kith-Lir", HeroRoster.RaceName(HeroRoster.Zeph));
        Assert.Equal("Aethel-Born", HeroRoster.RaceName(HeroRoster.Aurel));
        Assert.Equal("Aethel-Born", HeroRoster.RaceName(HeroRoster.Seraphine));
        Assert.Contains(HeroRoster.Ids, id => HeroRoster.Name(id).Contains("Saeli", StringComparison.Ordinal));
        Assert.DoesNotContain(HeroRoster.Ids, id => HeroRoster.Name(id).Contains("Mirrim", StringComparison.Ordinal));
    }

    [Fact]
    public void Presets_are_four_heroes_from_the_roster()
    {
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int[] ids = HeroRoster.PresetIds(preset);
            Assert.Equal(4, ids.Length);
            Assert.Equal(preset, HeroRoster.MatchPreset(ids));
            HeroState[] party = HeroRoster.PresetParty(preset);
            Assert.Equal(4, party.Length);
            Assert.Equal("Korrith Vael-Dun", party[0].Name);
            Assert.NotNull(party[0].Deck);
        }

        HeroState[] shatter = CarapaceEncounter.CreateParty(PartyPreset.ShatterChoir);
        Assert.Equal("Saeli Thorn-Vesper", shatter[1].Name);
        Assert.Equal("Aurel Nine-Vesper", shatter[2].Name);
        Assert.Equal(SimConst.SaeliCadenceRefundAp, shatter[1].CadenceRefundAp);
    }

    [Fact]
    public void Party_check_requires_four_and_warns_without_tank_or_healer()
    {
        PartyCheck empty = HeroRoster.Check([]);
        Assert.False(empty.CanStart);
        Assert.Contains("Select 4 heroes", empty.Status, StringComparison.Ordinal);

        PartyCheck ready = HeroRoster.Check(HeroRoster.PresetIds(PartyPreset.IceLattice));
        Assert.True(ready.CanStart);
        Assert.True(ready.HasTank);
        Assert.True(ready.HasHealer);
        Assert.Equal("Party of 4.", ready.Status);

        PartyCheck noHealer = HeroRoster.Check([HeroRoster.Korrith, HeroRoster.Saeli, HeroRoster.Kaelis, HeroRoster.Zeph]);
        Assert.True(noHealer.CanStart);
        Assert.False(noHealer.HasHealer);
        Assert.Contains("No healer", noHealer.Status, StringComparison.Ordinal);

        PartyCheck noTank = HeroRoster.Check([HeroRoster.Saeli, HeroRoster.Kaelis, HeroRoster.Zeph, HeroRoster.Seraphine]);
        Assert.True(noTank.CanStart);
        Assert.False(noTank.HasTank);
        Assert.Contains("No tank", noTank.Status, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_roster_kit_has_a_description_and_an_effect_line()
    {
        HeroState[] party = HeroRoster.CustomParty(HeroRoster.Ids.Take(4).ToArray());
        var boss = CarapaceEncounter.Create(party, 1UL);
        foreach (int id in HeroRoster.Ids)
        {
            HeroState hero = HeroRoster.Create(id, 0);
            Assert.NotNull(hero.Deck);
            Assert.False(string.IsNullOrWhiteSpace(HeroRoster.KitSummary(id)));
            foreach (int abilityId in hero.Kit)
            {
                AbilityDef ability = AbilityCatalog.Get(abilityId);
                Assert.False(string.IsNullOrWhiteSpace(ability.Description), ability.Name);
                string panel = AbilityBrief.Format(ability, hero, hero, boss.Boss, 0, 0, party);
                Assert.StartsWith(ability.Name + "\nEffect: ", panel, StringComparison.Ordinal);
                Assert.Contains(ability.Description, panel, StringComparison.Ordinal);
            }
        }
    }
}
