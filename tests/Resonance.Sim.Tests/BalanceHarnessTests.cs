using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class BalanceHarnessTests
{
    [Fact]
    public void Default_decks_win_most_carapace_seeds()
    {
        const int n = 12;
        int wins = 0;
        long tickSum = 0;
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < n; i++)
        {
            BattleSimulator sim = CarapaceEncounter.Create(20261004UL + (ulong)i);
            sim.RunToEnd();
            if (sim.Outcome == FightOutcome.Victory)
            {
                wins++;
                tickSum += sim.Tick;
            }

            foreach (CombatEvent evt in sim.Events)
            {
                Count(counts, evt.Text, "Liquefaction");
                Count(counts, evt.Text, "Induration");
                Count(counts, evt.Text, "Fragmentation");
                Count(counts, evt.Text, "Distortion");
                Count(counts, evt.Text, "Conduction");
                Count(counts, evt.Text, "Tectonic");
                Count(counts, evt.Text, "Radiance");
                Count(counts, evt.Text, "Solar Apex");
                Count(counts, evt.Text, "Umbral Zero");
                Count(counts, evt.Text, "Magma Core");
                Count(counts, evt.Text, "Tempest Crown");
            }

            Console.WriteLine($"seed {20261004 + i} {sim.Outcome} tick {sim.Tick} core {sim.Boss.Parts[0].Hp}");
        }

        int average = wins == 0 ? 0 : (int)(tickSum / wins);
        Console.WriteLine($"wins {wins}/{n} average victory tick {average}");
        foreach (KeyValuePair<string, int> pair in counts.OrderBy(pair => pair.Key))
        {
            Console.WriteLine($"resonance {pair.Key} {pair.Value}");
        }

        Assert.InRange(wins, (n / 2) + 1, n - 1);
    }

    private static void Count(Dictionary<string, int> counts, string text, string name)
    {
        if (!text.Contains(name, StringComparison.Ordinal))
        {
            return;
        }

        counts.TryGetValue(name, out int value);
        counts[name] = value + 1;
    }
}
