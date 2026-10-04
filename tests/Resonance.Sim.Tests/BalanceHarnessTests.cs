using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class BalanceHarnessTests
{
    [Fact]
    public void Presets_win_in_band_and_spread_resonances()
    {
        const int n = 12;
        const ulong first = 20261004UL;
        var rows = new PresetRow[PartyPreset.Count];
        var union = new int[8];
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int wins = 0;
            long tickSum = 0;
            int stunsLanded = 0;
            int stunsLost = 0;
            int aegisCasts = 0;
            int absorbed = 0;
            var l2 = new int[8];
            var l3 = new int[5];
            for (int i = 0; i < n; i++)
            {
                BattleSimulator sim = CarapaceEncounter.Create(preset, first + (ulong)i);
                sim.RunToEnd();
                stunsLanded += sim.StunsLanded;
                stunsLost += sim.StunsLost;
                aegisCasts += sim.AegisCasts;
                absorbed += sim.DamageAbsorbed;
                if (sim.Outcome == FightOutcome.Victory)
                {
                    wins++;
                    tickSum += sim.Tick;
                }

                for (int r = 1; r < l2.Length; r++)
                {
                    int count = sim.Detonations((ResonanceId)r);
                    l2[r] += count;
                    union[r] += count;
                }

                for (int a = 1; a < l3.Length; a++)
                {
                    l3[a] += sim.Detonations((ApexId)a);
                }

                Console.WriteLine($"preset {PartyPreset.Name(preset)} seed {first + (ulong)i} {sim.Outcome} tick {sim.Tick} core {sim.Boss.Parts[0].Hp} l3 {sim.Level3Detonations}");
            }

            int average = wins == 0 ? 0 : (int)(tickSum / wins);
            rows[preset] = new PresetRow(PartyPreset.Name(preset), wins, average, l2, l3, stunsLanded, stunsLost, aegisCasts, absorbed);
            int shownTotal = 0;
            int shownMax = 0;
            for (int r = 1; r < l2.Length; r++)
            {
                shownTotal += l2[r];
                if (l2[r] > shownMax)
                {
                    shownMax = l2[r];
                }
            }

            int shownShare = shownTotal == 0 ? 100 : shownMax * 100 / shownTotal;
            Console.WriteLine($"preset {rows[preset].Name} wins {wins}/{n} average {average} l3 {Sum(l3)} share {shownShare}% stuns {stunsLanded} lost {stunsLost} aegis {aegisCasts} absorbed {absorbed}");
            for (int r = 1; r < l2.Length; r++)
            {
                if (l2[r] > 0)
                {
                    Console.WriteLine($"  {(ResonanceId)r} {l2[r]}");
                }
            }

            for (int a = 1; a < l3.Length; a++)
            {
                if (l3[a] > 0)
                {
                    Console.WriteLine($"  {(ApexId)a} {l3[a]}");
                }
            }
        }

        int best = 0;
        for (int i = 1; i < rows.Length; i++)
        {
            if (rows[i].Wins > rows[best].Wins || (rows[i].Wins == rows[best].Wins && rows[i].AverageTick < rows[best].AverageTick))
            {
                best = i;
            }
        }

        int distinct = 0;
        for (int r = 1; r < union.Length; r++)
        {
            if (union[r] > 0)
            {
                distinct++;
            }
        }

        int bestTotal = 0;
        int bestMax = 0;
        for (int r = 1; r < rows[best].L2.Length; r++)
        {
            bestTotal += rows[best].L2[r];
            if (rows[best].L2[r] > bestMax)
            {
                bestMax = rows[best].L2[r];
            }
        }

        int share = bestTotal == 0 ? 100 : bestMax * 100 / bestTotal;
        Console.WriteLine($"best {rows[best].Name} share {share}% distinct {distinct}");

        for (int i = 0; i < rows.Length; i++)
        {
            Assert.InRange(rows[i].Wins, 7, 9);
            Assert.True(rows[i].StunsLanded > 0, rows[i].Name);
            Assert.True(rows[i].StunsLost > 0, rows[i].Name);
            Assert.True(rows[i].AegisCasts > 0, rows[i].Name);
            Assert.True(rows[i].DamageAbsorbed > 0, rows[i].Name);
        }

        Assert.True(distinct >= 5);
        Assert.InRange(share, 0, 60);
    }

    [Fact]
    public void Floor_clears_in_band()
    {
        const int n = 12;
        const ulong first = 20261004UL;
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int clears = 0;
            var wipes = new int[FloorRun.RoomCount];
            for (int i = 0; i < n; i++)
            {
                FloorRun run = FloorRun.Simulate(preset, first + (ulong)i);
                if (run.Cleared)
                {
                    clears++;
                    Console.WriteLine($"floor {PartyPreset.Name(preset)} seed {first + (ulong)i} Cleared ticks {run.Ticks}");
                }
                else
                {
                    wipes[run.WipeRoom]++;
                    Console.WriteLine($"floor {PartyPreset.Name(preset)} seed {first + (ulong)i} Wiped at {FloorRun.Rooms[run.WipeRoom].Name} ticks {run.Ticks}");
                }
            }

            Console.WriteLine($"floor {PartyPreset.Name(preset)} clears {clears}/{n}");
            for (int room = 0; room < wipes.Length; room++)
            {
                if (wipes[room] > 0)
                {
                    Console.WriteLine($"  wipe {FloorRun.Rooms[room].Name} {wipes[room]}");
                }
            }

            Assert.InRange(clears, 6, 9);
        }
    }

    [Fact]
    public void Every_four_hero_combo_stays_inside_the_clear_band()
    {
        const int n = 24;
        const ulong first = 20261004UL;
        int[][] combos = HeroRoster.Combinations();
        Assert.Equal(15, combos.Length);
        foreach (int[] ids in combos)
        {
            int clears = 0;
            for (int i = 0; i < n; i++)
            {
                FloorRun run = FloorRun.SimulateCustom(ids, first + (ulong)i);
                if (run.Cleared)
                {
                    clears++;
                }
            }

            Console.WriteLine($"combo {HeroRoster.Label(ids)} clears {clears}/{n}");
            Assert.InRange(clears, 6, 18);
        }
    }

    private static int Sum(int[] values)
    {
        int total = 0;
        for (int i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }

    private readonly struct PresetRow
    {
        public PresetRow(string name, int wins, int averageTick, int[] l2, int[] l3, int stunsLanded, int stunsLost, int aegisCasts, int damageAbsorbed)
        {
            Name = name;
            Wins = wins;
            AverageTick = averageTick;
            L2 = l2;
            L3 = l3;
            StunsLanded = stunsLanded;
            StunsLost = stunsLost;
            AegisCasts = aegisCasts;
            DamageAbsorbed = damageAbsorbed;
        }

        public string Name { get; }

        public int Wins { get; }

        public int AverageTick { get; }

        public int[] L2 { get; }

        public int[] L3 { get; }

        public int StunsLanded { get; }

        public int StunsLost { get; }

        public int AegisCasts { get; }

        public int DamageAbsorbed { get; }
    }
}
