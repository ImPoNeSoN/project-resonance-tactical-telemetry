using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

/// <summary>
/// §2.15 regenerated from PCG seed <see cref="TrainingArena.GoldenSeed"/>.
/// Every roll the fight draws is in the transcript. ScriptedRng replays that list,
/// which is how a log that omitted a roll would still be pinned.
/// </summary>
public class GoldenCombatLogTests
{
    [Fact]
    public void Worked_log_ends_at_the_seeded_state()
    {
        BattleSimulator sim = TrainingArena.CreateGolden();
        sim.RunToEnd();
        string transcript = Transcript(sim);

        Assert.Equal(3_087, sim.Tick);
        Assert.Contains("roll 9331", transcript, StringComparison.Ordinal);
        Assert.Contains("roll 1300", transcript, StringComparison.Ordinal);
        Assert.Contains("roll 2109", transcript, StringComparison.Ordinal);
        Assert.Contains("no roll", transcript, StringComparison.Ordinal);
        Assert.Contains("sheds 32 CE on Core", transcript, StringComparison.Ordinal);
        Assert.Contains("Dmg 3849", transcript, StringComparison.Ordinal);
        Assert.Contains("Dmg 3272", transcript, StringComparison.Ordinal);
        Assert.Contains("Induration detonation 1636", transcript, StringComparison.Ordinal);

        HeroState korrith = sim.Heroes[0];
        HeroState mirrim = sim.Heroes[1];
        HeroState zeph = sim.Heroes[2];
        HeroState seraphine = sim.Heroes[3];

        Assert.Equal(8_643, korrith.Hp);
        Assert.Equal(270, korrith.Mp);
        Assert.Equal(8_483 * 100, korrith.Ap.Centi);
        Assert.Equal(3_164, sim.Boss.Parts[0].Enmity[0].Ve);
        Assert.Equal(2_303, sim.Boss.Parts[0].Enmity[0].Ce);

        Assert.Equal(4_480, mirrim.Hp);
        Assert.Equal(95, mirrim.Mp);
        Assert.Equal(4_566 * 100, mirrim.Ap.Centi);
        Assert.Equal(2_440, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1_036, sim.Boss.Parts[1].Enmity[1].Ce);

        Assert.Equal(3_710, zeph.Hp);
        Assert.Equal(414, zeph.Mp);
        Assert.Equal(5_831 * 100, zeph.Ap.Centi);
        Assert.Equal(466, sim.Boss.Parts[0].Enmity[2].Ve);
        Assert.Equal(1_899, sim.Boss.Parts[0].Enmity[2].Ce);

        Assert.Equal(4_750, seraphine.Hp);
        Assert.Equal(500, seraphine.Mp);
        Assert.Equal(-3_989 * 100, seraphine.Ap.Centi);
        Assert.Equal(1_717, sim.Boss.Parts[0].Enmity[3].Ve);
        Assert.Equal(317, sim.Boss.Parts[0].Enmity[3].NormalVe);
        Assert.Equal(1_400, sim.Boss.Parts[0].Enmity[3].HeavyVe);
        Assert.Equal(180, sim.Boss.Parts[0].Enmity[3].Ce);

        Assert.Equal(108_698, sim.Boss.Parts[0].Hp);
        Assert.Equal(43_915, sim.Boss.Parts[1].Hp);
        Assert.Equal(30_000, sim.Boss.Parts[2].Hp);
        Assert.Equal(874 * 100, sim.Boss.Ap.Centi);
        Assert.Equal(1, sim.Boss.Parts[0].Tier);
        Assert.Equal(ChainProperty.Ice, sim.Boss.Parts[0].Property);
        Assert.Equal(6_900, sim.Boss.Parts[0].ChainExpires);
        Assert.Equal(4_139, sim.Boss.Parts[0].BurstExpires);
    }

    [Fact]
    public void Checkpoints_match_the_seeded_log()
    {
        var sim = TrainingArena.CreateGolden();

        sim.RunUntil(256);
        Assert.Equal(119_495, sim.Boss.Parts[0].Hp);
        Assert.Equal(540, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1, sim.Boss.Parts[0].Tier);

        sim.RunUntil(534);
        Assert.Equal(3_940, sim.Heroes[1].Hp);
        Assert.Equal(508, sim.Boss.Parts[0].Enmity[1].Ce);

        sim.RunUntil(690);
        Assert.Equal(20, sim.Heroes[2].ConcentrationBuff);
        Assert.Equal(4_690, sim.Heroes[2].ConcentrationBuffExpires);
        Assert.Equal(4_690, sim.Heroes[2].RegenExpires);
        Assert.Equal(1_190, sim.Heroes[2].RegenNextTick);

        sim.RunUntil(812);
        Assert.Equal(118_915, sim.Boss.Parts[0].Hp);
        Assert.Equal(2, sim.Boss.Parts[0].Tier);
        Assert.Equal(1_216, sim.Heroes[1].Ap.WholeAp);

        sim.RunUntil(1_170);
        Assert.Equal(3_849, DamageAt(sim, 1_170));
        Assert.Equal(1, sim.Boss.Parts[0].Tier);
        Assert.Equal(ChainProperty.Ice, sim.Boss.Parts[0].Property);
        Assert.Equal(5_170, sim.Boss.Parts[0].ChainExpires);
        Assert.Equal(2_912, sim.Boss.Parts[0].BurstExpires);
        Assert.Equal(907, sim.Boss.Parts[0].Enmity[2].Ce);
        Assert.Equal(477, sim.Heroes[2].Mp);

        sim.RunUntil(1_523);
        Assert.Equal(44_495, sim.Boss.Parts[1].Hp);
        Assert.Equal(540, sim.Boss.Parts[1].Enmity[1].Ce);
        Assert.Equal(4_014, sim.Heroes[1].Ap.WholeAp);
        Assert.Equal(781_640, sim.Boss.Ap.Centi);

        sim.RunUntil(2_639);
        Assert.Equal(3_272, DamageAt(sim, 2_639));
        Assert.Equal(2, sim.Boss.Parts[0].Tier);
        Assert.Equal(ResonanceId.Induration, sim.Boss.Parts[0].Resonance);
        Assert.Equal(4_139, sim.Boss.Parts[0].BurstExpires);
        Assert.Equal(1_899, sim.Boss.Parts[0].Enmity[2].Ce);
        Assert.Equal(414, sim.Heroes[2].Mp);
        Assert.True(sim.Boss.Casting);

        sim.RunUntil(2_900);
        Assert.Equal(581, DamageAt(sim, 2_900));
        Assert.Equal(1, sim.Boss.Parts[0].Tier);
        Assert.Equal(ChainProperty.Ice, sim.Boss.Parts[0].Property);
        Assert.Equal(2_440, sim.Boss.Parts[0].Enmity[1].Ce);
        Assert.Equal(1_200, sim.Heroes[1].Ap.WholeAp);

        sim.RunUntil(3_087);
        Assert.Equal(-398_900, sim.Heroes[3].Ap.Centi);
        Assert.Equal(1_400, sim.Boss.Parts[0].Enmity[3].HeavyVe);
        Assert.Equal(108_698, sim.Boss.Parts[0].Hp);
    }

    [Fact]
    public void Scripted_replay_matches_the_seed_when_every_roll_is_recorded()
    {
        var log = new RollLog();
        var live = TrainingArena.Create(new LoggingRng(new EncounterRng(TrainingArena.GoldenSeed), log));
        live.RunToEnd();

        var replayRng = new ScriptedRng(log.Snapshot());
        var replay = TrainingArena.Create(replayRng);
        replay.RunToEnd();

        Assert.Equal(live.Boss.Parts[0].Hp, replay.Boss.Parts[0].Hp);
        Assert.Equal(live.Boss.Parts[1].Hp, replay.Boss.Parts[1].Hp);
        Assert.Equal(live.Heroes[1].Ap.Centi, replay.Heroes[1].Ap.Centi);
        Assert.Equal(live.Heroes[2].Mp, replay.Heroes[2].Mp);
        Assert.True(replayRng.IsExhausted);
        Assert.True(log.Hit.Count > 0);
        Assert.True(log.Crit.Count > 0);
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

    private sealed class RollLog
    {
        public List<int> Hit { get; } = [];
        public List<int> Crit { get; } = [];
        public List<int> Multi { get; } = [];

        public Dictionary<RngStream, IReadOnlyList<int>> Snapshot() => new()
        {
            [RngStream.Hit] = Hit,
            [RngStream.Crit] = Crit,
            [RngStream.MultiAttack] = Multi,
        };
    }

    private sealed class LoggingRng : IRng
    {
        private readonly IRng _inner;
        private readonly RollLog _log;

        public LoggingRng(IRng inner, RollLog log)
        {
            _inner = inner;
            _log = log;
        }

        public int RollD10000(RngStream stream, string reason)
        {
            int roll = _inner.RollD10000(stream, reason);
            switch (stream)
            {
                case RngStream.Hit:
                    _log.Hit.Add(roll);
                    break;
                case RngStream.Crit:
                    _log.Crit.Add(roll);
                    break;
                case RngStream.MultiAttack:
                    _log.Multi.Add(roll);
                    break;
            }

            return roll;
        }
    }
}
