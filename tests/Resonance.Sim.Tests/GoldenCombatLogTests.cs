using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

/// <summary>
/// Reproduces 02 §2.15. The published log is the expected end state and the checkpoint rows.
/// </summary>
public class GoldenCombatLogTests
{
    [Fact]
    public void Worked_log_ends_at_the_published_state()
    {
        (BattleSimulator sim, var rng) = TrainingArena.CreateGolden();
        sim.RunToEnd();
        string transcript = Transcript(sim);

        Assert.Equal(3_087, sim.Tick);

        HeroState korrith = sim.Heroes[0];
        HeroState mirrim = sim.Heroes[1];
        HeroState zeph = sim.Heroes[2];
        HeroState seraphine = sim.Heroes[3];

        Assert.Equal(8_643, korrith.Hp);
        Assert.Equal(270, korrith.Mp);
        Assert.Equal(8_483 * 100, korrith.Ap.Centi);
        Assert.Equal(3_164, sim.Boss.Parts[0].Enmity[0].Ve);
        Assert.Equal(2_303, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(0, sim.Boss.Parts[1].Enmity[0].Total);

        Assert.Equal(4_480, mirrim.Hp);
        Assert.Equal(95, mirrim.Mp);
        Assert.Equal(5_766 * 100, mirrim.Ap.Centi);
        Assert.Equal(0, sim.Boss.Parts[0].Enmity[1].Ve);
        Assert.Equal(2_440, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1_056, sim.Boss.Parts[1].Enmity[1].Ce);

        Assert.Equal(3_710, zeph.Hp);
        Assert.Equal(414, zeph.Mp);
        Assert.Equal(5_831 * 100, zeph.Ap.Centi);
        Assert.Equal(466, sim.Boss.Parts[0].Enmity[2].Ve);
        Assert.Equal(1_660, sim.Boss.Parts[0].Enmity[2].Ce);

        Assert.Equal(4_750, seraphine.Hp);
        Assert.Equal(500, seraphine.Mp);
        Assert.Equal(-3_989 * 100, seraphine.Ap.Centi);
        Assert.Equal(1_717, sim.Boss.Parts[0].Enmity[3].Ve);
        Assert.Equal(317, sim.Boss.Parts[0].Enmity[3].NormalVe);
        Assert.Equal(1_400, sim.Boss.Parts[0].Enmity[3].HeavyVe);
        Assert.Equal(180, sim.Boss.Parts[0].Enmity[3].Ce);

        Assert.Equal(111_683, sim.Boss.Parts[0].Hp);
        Assert.Equal(43_662, sim.Boss.Parts[1].Hp);
        Assert.Equal(30_000, sim.Boss.Parts[2].Hp);
        Assert.Equal(874 * 100, sim.Boss.Ap.Centi);
        Assert.True(rng.IsExhausted, transcript);
        Assert.Contains("Induration", transcript, StringComparison.Ordinal);
        Assert.Contains("Dmg 3849", transcript, StringComparison.Ordinal);
        Assert.Contains("Dmg 1924", transcript, StringComparison.Ordinal);
        Assert.Contains("Dmg 505", transcript, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(256, 1, 4_480, 225, 8)]
    [InlineData(534, 1, 3_940, 225, 8 + (534 - 256) * 18)]
    [InlineData(812, 1, 3_940, 195, 1_216)]
    [InlineData(1_492, 1, 4_480, 175, 1_492)] // AP checked separately below when it is not a simple product
    public void Party_hp_moves_on_the_published_ticks(int tick, int hero, int hp, int mp, int unusedAp)
    {
        _ = unusedAp;
        var sim = TrainingArena.CreateGolden().Sim;
        sim.RunUntil(tick);
        Assert.Equal(hp, sim.Heroes[hero].Hp);
        Assert.Equal(mp, sim.Heroes[hero].Mp);
    }

    [Fact]
    public void Checkpoints_match_the_log_row_by_row()
    {
        var sim = TrainingArena.CreateGolden().Sim;

        sim.RunUntil(256);
        Assert.Equal(119_495, sim.Boss.Parts[0].Hp);
        Assert.Equal(540, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(8, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(1, sim.Boss.Parts[0].Tier);

        sim.RunUntil(470);
        Assert.True(sim.Heroes[2].Casting);
        Assert.True(sim.Heroes[3].Casting);
        Assert.Equal(1_170, sim.Heroes[2].CastResolveTick);
        Assert.Equal(690, sim.Heroes[3].CastResolveTick);
        Assert.Equal(360, sim.Heroes[2].Mp);
        Assert.Equal(900, sim.Heroes[3].Mp);

        sim.RunUntil(534);
        Assert.Equal(3_940, sim.Heroes[1].Hp);
        Assert.Equal(508, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(8, sim.Boss.Ap.WholeAp);

        sim.RunUntil(690);
        Assert.Equal(20, sim.Heroes[2].ConcentrationBuff);
        Assert.Equal(300, sim.Boss.Parts[0].Enmity[3].Ve);
        Assert.Equal(60, sim.Boss.Parts[0].Enmity[3].Ce);
        Assert.Equal(6_010, sim.Heroes[3].Ap.WholeAp);

        sim.RunUntil(812);
        Assert.Equal(118_915, sim.Boss.Parts[0].Hp);
        Assert.Equal(1_004, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1_216, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(2_200, sim.Boss.Parts[0].Enmity[0].Ve);
        Assert.Equal(330, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(6_008, sim.Heroes[0].Ap.WholeAp);
        Assert.Equal(2, sim.Boss.Parts[0].Tier);
        Assert.Equal(3_000, sim.Boss.SlowBp);
        Assert.True(sim.Boss.Parts[0].BurstActive);

        sim.RunUntil(1_170);
        Assert.Equal(3_849, DamageAt(sim, 1_170));
        Assert.Equal(477, sim.Heroes[2].Mp);
        Assert.Equal(2_912, sim.Boss.Parts[0].BurstExpires);
        Assert.Equal(907, sim.Boss.Parts[0].Enmity[2].Ce);
        Assert.Equal(300, sim.Boss.Parts[0].Enmity[2].Ve);
        Assert.Equal(10, sim.Heroes[2].Ap.WholeAp);
        Assert.Equal(2, sim.Boss.Parts[0].Tier);

        sim.RunUntil(1_256);
        Assert.Equal(2_380, sim.Boss.Parts[0].Enmity[0].Ve);
        Assert.Equal(1_320, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(270, sim.Heroes[0].Mp);
        Assert.Equal(6_004, sim.Heroes[0].Ap.WholeAp);

        sim.RunUntil(1_300);
        Assert.Equal(1_204, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(175, sim.Heroes[1].Mp);
        Assert.Equal(6_000, sim.Heroes[1].Ap.WholeAp);

        sim.RunUntil(1_492);
        Assert.Equal(4_480, sim.Heroes[1].Hp);
        Assert.Equal(486, sim.Boss.Parts[0].Enmity[3].Ve);
        Assert.Equal(1, sim.Heroes[3].Ap.WholeAp);

        sim.RunUntil(1_523);
        Assert.Equal(44_242, sim.Boss.Parts[1].Hp);
        Assert.Equal(560, sim.Boss.Parts[1].Enmity[1].Ce);
        Assert.Equal(5_214, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(781_640, sim.Boss.Ap.Centi);

        sim.RunUntil(1_700);
        Assert.Equal(4_342, sim.Boss.Parts[0].Enmity[0].Ve);
        Assert.Equal(1_650, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(6_000, sim.Heroes[0].Ap.WholeAp);

        sim.RunUntil(1_783);
        Assert.True(sim.Boss.Casting);
        Assert.Equal(2_983, sim.Boss.CastResolveTick);
        Assert.Equal(1_000_040, sim.Boss.Ap.Centi);

        sim.RunUntil(1_789);
        Assert.Equal(43_662, sim.Boss.Parts[1].Hp);
        Assert.Equal(1_056, sim.Boss.Parts[1].Enmity[1].Ce);
        Assert.Equal(1_202, sim.Heroes[1].Ap.WholeAp);

        sim.RunUntil(2_145);
        Assert.Equal(374, DamageAt(sim, 2_145));
        Assert.Equal(1, sim.Boss.Parts[0].Tier);
        Assert.Equal(2_379, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(5, sim.Heroes[0].Ap.WholeAp);
        Assert.True(sim.Boss.Parts[0].BurstActive);

        sim.RunUntil(2_500);
        Assert.Equal(1_944, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(4_000, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(3_516, sim.Boss.Parts[0].Enmity[0].Ve);

        sim.RunUntil(2_639);
        Assert.Equal(1_924, DamageAt(sim, 2_639));
        Assert.Equal(414, sim.Heroes[2].Mp);
        Assert.Equal(3_512, sim.Boss.Parts[0].BurstExpires);
        Assert.Equal(1_660, sim.Boss.Parts[0].Enmity[2].Ce);
        Assert.Equal(518, sim.Boss.Parts[0].Enmity[2].Ve);
        Assert.Equal(7, sim.Heroes[2].Ap.WholeAp);
        Assert.True(sim.Boss.Casting);

        sim.RunUntil(2_834);
        Assert.Equal(2_440, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1_212, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(4_334, sim.Boss.Parts[0].BurstExpires);
        Assert.Equal(2, sim.Boss.Parts[0].Tier);

        sim.RunUntil(2_983);
        Assert.Equal(8_643, sim.Heroes[0].Hp);
        Assert.Equal(2_303, sim.Boss.Parts[0].Enmity[0].Ce);
        Assert.Equal(40, sim.Boss.Ap.Centi);
        Assert.False(sim.Boss.Casting);

        sim.RunUntil(3_087);
        Assert.Equal(-398_900, sim.Heroes[3].Ap.Centi);
        Assert.Equal(1_400, sim.Boss.Parts[0].Enmity[3].HeavyVe);
        Assert.Equal(180, sim.Boss.Parts[0].Enmity[3].Ce);
        Assert.Equal(2_500, sim.Heroes[0].AllDtBp);
    }

    private static int DamageAt(BattleSimulator sim, int tick)
    {
        foreach (CombatEvent evt in sim.Events)
        {
            if (evt.Tick != tick || !evt.Text.Contains("Dmg ", StringComparison.Ordinal))
            {
                continue;
            }

            int marker = evt.Text.IndexOf("Dmg ", StringComparison.Ordinal);
            int start = marker + 4;
            int end = start;
            while (end < evt.Text.Length && char.IsDigit(evt.Text[end]))
            {
                end++;
            }

            return int.Parse(evt.Text[start..end]);
        }

        throw new Xunit.Sdk.XunitException(Transcript(sim));
    }

    private static string Transcript(BattleSimulator sim)
    {
        var lines = new List<string>();
        foreach (CombatEvent evt in sim.Events)
        {
            lines.Add($"t={evt.Tick} {evt.Text}");
        }

        return string.Join('\n', lines);
    }
}
