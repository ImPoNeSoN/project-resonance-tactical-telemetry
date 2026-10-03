using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class GambitTests
{
    private const string ZephDeck = """
        1  IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II]
        2  IF Target [Resonance: Fire] AND Target [BurstLeft > 1000] -> CAST [Pyre Lattice]
        3  IF Target [Resonance: Lightning] -> CAST [Hex Lance]
        4  IF Target [Window: Piercing] AND Ally(Mirrim Ash-Pounce) [ReadyIn < 300] -> CAST [Blizzard II] {BurstOnly}
        5  IF Target [Window: Fragmentation] -> CAST [Hex Lance]
        6  IF Field [Always] -> DEFER [600]
        """;

    private const string KorrithDeck = """
        1  IF Boss [TankMargin < 1500] -> USE [Lattice Provoke]
        2  IF Boss [CastResolvesIn < 800] AND NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
        3  IF Boss.WeaponArm [MyEnmityRank > 1] -> USE [Lattice Provoke] ON WeaponArm
        4  IF Target [Window: Induration] -> USE [Attack] {NoOverwrite}
        5  IF Field [Always] -> USE [Seismic Maul]
        """;

    [Fact]
    public void Parser_accepts_the_two_text_examples_and_the_published_decks()
    {
        GambitCompileResult ice = GambitCompiler.Compile(
            "IF Target [Resonance: Ice] -> CAST [Blizzard II]",
            GambitCompileOptions.Standard());
        Assert.True(ice.Ok, string.Join("; ", ice.Errors));
        Assert.Equal(AbilityCatalog.BlizzardII, ice.Program.Slots[0].AbilityId);

        GambitCompileResult bash = GambitCompiler.Compile(
            "IF Boss [Casting] -> USE [Shield Bash]",
            GambitCompileOptions.Standard(bulwarkBash: true));
        Assert.True(bash.Ok, string.Join("; ", bash.Errors));
        Assert.Equal(AbilityCatalog.ShieldBash, bash.Program.Slots[0].AbilityId);

        GambitCompileResult denied = GambitCompiler.Compile(
            "IF Boss [Casting] -> USE [Shield Bash]",
            GambitCompileOptions.Standard());
        Assert.False(denied.Ok);
        Assert.Contains("Bulwark Bash", string.Join("; ", denied.Errors), StringComparison.Ordinal);

        GambitCompileResult zeph = GambitCompiler.Compile(ZephDeck, GambitCompileOptions.Standard());
        Assert.True(zeph.Ok, string.Join("; ", zeph.Errors));
        Assert.Equal(6, zeph.Program.Slots.Length);
        Assert.Equal(GambitFlags.BurstOnly, zeph.Program.Slots[3].Flags);

        GambitCompileResult korrith = GambitCompiler.Compile(KorrithDeck, GambitCompileOptions.Standard());
        Assert.True(korrith.Ok, string.Join("; ", korrith.Errors));
        Assert.Equal(AbilityCatalog.SeismicMaul, korrith.Program.Slots[4].AbilityId);
    }

    [Fact]
    public void Validator_enforces_slots_budget_defer_and_rank_unlocks()
    {
        string seven = string.Join('\n', Enumerable.Range(1, 7).Select(i => $"{i} IF Field [Always] -> USE [Attack]"));
        GambitCompileResult capped = GambitCompiler.Compile(seven, GambitCompileOptions.Standard());
        Assert.False(capped.Ok);
        GambitCompileResult ranked = GambitCompiler.Compile(seven, GambitCompileOptions.ForRank(10));
        Assert.True(ranked.Ok, string.Join("; ", ranked.Errors));
        Assert.Equal(7, GambitCompileOptions.ForRank(10).MaxSlots);
        Assert.Equal(10, GambitCompileOptions.ForRank(40).MaxSlots);

        var budget = new System.Text.StringBuilder();
        for (int i = 0; i < 7; i++)
        {
            budget.AppendLine("IF Self [HP% < 100] AND Self [MP% < 100] AND Self [Heat < 100] -> USE [Attack]");
        }

        GambitCompileResult over = GambitCompiler.Compile(budget.ToString(), GambitCompileOptions.ForRank(10));
        Assert.False(over.Ok);
        Assert.Contains("logic budget", string.Join("; ", over.Errors), StringComparison.Ordinal);

        GambitCompileResult defers = GambitCompiler.Compile(
            """
            IF Field [Always] -> DEFER [100]
            IF Field [Always] -> DEFER [100]
            IF Field [Always] -> DEFER [100]
            """,
            GambitCompileOptions.Standard());
        Assert.False(defers.Ok);
        Assert.Contains("DEFER", string.Join("; ", defers.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void Ice_resonance_casts_blizzard_and_boss_casting_uses_shield_bash()
    {
        BattleSimulator ice = Fight(
            """
            IF Target [Resonance: Ice] -> CAST [Blizzard II]
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out BossPartState core);
        core.BurstActive = true;
        core.BurstOpened = 0;
        core.BurstExpires = 100_000;
        core.BurstMask = ElementMask.Ice;
        ice.Heroes[0].Mp = 500;
        ice.RunUntil(1);
        Assert.Contains("starts Blizzard II", Transcript(ice), StringComparison.Ordinal);

        BattleSimulator plain = Fight(
            """
            IF Target [Resonance: Ice] -> CAST [Blizzard II]
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out _);
        plain.RunUntil(1);
        Assert.Contains("Attack →", Transcript(plain), StringComparison.Ordinal);
        Assert.DoesNotContain("Blizzard II", Transcript(plain), StringComparison.Ordinal);

        BattleSimulator bash = Fight(
            """
            IF Boss [Casting] -> USE [Shield Bash]
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: true,
            chip: true,
            out _,
            interrupt: 0);
        bash.Boss.Casting = true;
        bash.Boss.CastAbilityId = AbilityCatalog.PistonSweep;
        bash.Boss.CastPart = 0;
        bash.Boss.CastResolveTick = 50_000;
        bash.RunUntil(1);
        Assert.Contains("Shield Bash", Transcript(bash), StringComparison.Ordinal);
        Assert.Contains("Boss chant interrupted", Transcript(bash), StringComparison.Ordinal);

        BattleSimulator locked = Fight(
            """
            IF Boss [Casting] -> USE [Shield Bash]
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: true,
            chip: false,
            out _);
        locked.Boss.Casting = true;
        locked.Boss.CastAbilityId = AbilityCatalog.PistonSweep;
        locked.Boss.CastResolveTick = 50_000;
        locked.RunUntil(1);
        Assert.Contains("Attack →", Transcript(locked), StringComparison.Ordinal);
        Assert.DoesNotContain("Shield Bash", Transcript(locked), StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_command_beats_gambits_and_replays_from_the_stream()
    {
        BattleSimulator live = Fight(
            "IF Field [Always] -> CAST [Glacier Snap]",
            bulwark: false,
            chip: false,
            out _);
        live.QueueManual(0, AbilityCatalog.LatticeProvoke, 0, -1);
        live.RunUntil(1);
        live.QueueManual(0, AbilityCatalog.KeratinBastion, 0, -1);
        live.RunUntil(80);
        string liveText = Transcript(live);
        Assert.Contains("Lattice Provoke", liveText, StringComparison.Ordinal);
        Assert.Contains("Keratin Bastion", liveText, StringComparison.Ordinal);
        Assert.Equal(0, live.Commands[0].ApplyAtTick);
        Assert.Equal(2, live.Commands[1].ApplyAtTick);

        BattleSimulator replay = Fight(
            "IF Field [Always] -> CAST [Glacier Snap]",
            bulwark: false,
            chip: false,
            out _);
        replay.LoadCommandStream(live.Commands);
        replay.RunUntil(80);
        Assert.Equal(liveText, Transcript(replay));
    }

    [Fact]
    public void Defer_caps_ap_and_does_not_act_until_the_window_ends()
    {
        BattleSimulator sim = Fight(
            """
            IF Field [Always] -> DEFER [30]
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out _);
        sim.Heroes[0].Agi = 400;
        sim.Heroes[0].Ap.Centi = SimConst.ReadyCenti - 40_000;
        sim.RunUntil(20);
        string mid = Transcript(sim);
        Assert.Contains("defers until", mid, StringComparison.Ordinal);
        Assert.Contains("AP 12000", mid, StringComparison.Ordinal);
        Assert.DoesNotContain("Attack →", mid, StringComparison.Ordinal);

        sim.RunUntil(40);
        Assert.Contains("Attack →", Transcript(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void NoOverwrite_skips_a_restart_and_burst_only_waits_for_the_window()
    {
        BattleSimulator skipped = Fight(
            """
            IF Field [Always] -> USE [Seismic Maul] {NoOverwrite}
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out BossPartState core);
        core.Tier = 2;
        core.Resonance = ResonanceId.Induration;
        core.ChainExpires = 100_000;
        skipped.RunUntil(1);
        Assert.Contains("Attack →", Transcript(skipped), StringComparison.Ordinal);
        Assert.DoesNotContain("Seismic Maul", Transcript(skipped), StringComparison.Ordinal);

        BattleSimulator early = Fight(
            """
            IF Field [Always] -> CAST [Blizzard II] {BurstOnly}
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out _);
        early.Heroes[0].Mp = 500;
        early.RunUntil(1);
        Assert.Contains("Attack →", Transcript(early), StringComparison.Ordinal);

        BattleSimulator inside = Fight(
            """
            IF Field [Always] -> CAST [Blizzard II] {BurstOnly}
            IF Field [Always] -> USE [Attack]
            """,
            bulwark: false,
            chip: false,
            out BossPartState burst);
        burst.BurstActive = true;
        burst.BurstOpened = 0;
        burst.BurstExpires = 100_000;
        burst.BurstMask = ElementMask.Ice;
        inside.Heroes[0].Mp = 500;
        inside.RunUntil(1);
        Assert.Contains("starts Blizzard II", Transcript(inside), StringComparison.Ordinal);
    }

    [Fact]
    public void Pause_stops_the_timeline_and_step_advances_one_tick()
    {
        BattleSimulator sim = Fight(
            "IF Field [Always] -> USE [Attack]",
            bulwark: false,
            chip: false,
            out _);
        sim.Pause();
        Assert.False(sim.TryStep());
        Assert.Equal(0, sim.Tick);
        Assert.True(sim.StepOne());
        Assert.True(sim.Paused);
        int stepped = sim.Tick;
        Assert.True(stepped > 0);
        sim.RunUntil(stepped + 10_000);
        Assert.Equal(stepped, sim.Tick);
        sim.Resume();
        Assert.True(sim.TryStep());
        Assert.True(sim.Tick > stepped);
    }

    [Fact]
    public void Wait_reduces_the_next_hit_and_blocks_shed()
    {
        BattleSimulator sim = Fight(
            "IF Field [Always] -> WAIT [Guard]",
            bulwark: false,
            chip: false,
            out BossPartState core,
            bossAbility: AbilityCatalog.PistonSweep);
        sim.Boss.Atk = 1_000;
        sim.Boss.Agi = 100;
        sim.Boss.Ap.Centi = SimConst.ReadyCenti - 10_000;
        sim.Boss.Parts[0].Enmity[0].Ce = 1_000;
        sim.RunUntil(1);
        Assert.Equal(SimConst.WaitMitigationBp, sim.Heroes[0].WaitDtBp);
        Assert.Contains("waits", Transcript(sim), StringComparison.Ordinal);
        Assert.DoesNotContain("sheds", Transcript(sim), StringComparison.Ordinal);
        Assert.True(sim.Heroes[0].Hp < sim.Heroes[0].MaxHp);
        Assert.True(sim.Heroes[0].Hp > sim.Heroes[0].MaxHp - 1_600);
        _ = core;
    }

    private static BattleSimulator Fight(
        string deck,
        bool bulwark,
        bool chip,
        out BossPartState core,
        int interrupt = -1,
        int bossAbility = -1)
    {
        GambitCompileResult compiled = GambitCompiler.Compile(deck, GambitCompileOptions.Standard(bulwark));
        Assert.True(compiled.Ok, string.Join("; ", compiled.Errors));
        var hero = new HeroState
        {
            Slot = 0,
            IsAnchor = true,
            Name = "Tester",
            Race = RaceId.VethKari,
            MaxHp = 8_000,
            Hp = 8_000,
            MaxMp = 500,
            Mp = 500,
            Acc = 500,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
            HasBulwarkBash = chip,
            Deck = compiled.Program,
        };
        core = new BossPartState
        {
            Name = "Core",
            MaxHp = 120_000,
            Hp = 120_000,
            Enmity = new EnmitySlot[1],
        };
        var arm = new BossPartState
        {
            Name = "WeaponArm",
            MaxHp = 40_000,
            Hp = 40_000,
            Enmity = new EnmitySlot[1],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core, arm],
        };
        var map = new Dictionary<RngStream, IReadOnlyList<int>>
        {
            [RngStream.Hit] = Repeat(0, 8),
            [RngStream.Crit] = Repeat(9_999, 8),
            [RngStream.MultiAttack] = Repeat(9_999, 8),
        };
        if (interrupt >= 0)
        {
            map[RngStream.Interrupt] = [interrupt];
        }

        var bossPlan = new Queue<BattleAction>();
        if (bossAbility >= 0)
        {
            bossPlan.Enqueue(new BattleAction(bossAbility, 0, -1));
        }

        return new BattleSimulator(
            [hero],
            boss,
            [new Queue<BattleAction>()],
            bossPlan,
            new ScriptedRng(map));
    }

    private static int[] Repeat(int value, int count)
    {
        var list = new int[count];
        Array.Fill(list, value);
        return list;
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
