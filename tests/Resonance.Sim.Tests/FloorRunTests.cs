using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class FloorRunTests
{
    [Fact]
    public void Floor_is_five_rooms_ending_at_the_carapace()
    {
        Assert.Equal(5, FloorRun.Rooms.Length);
        Assert.Equal(FloorRoomKind.Trash, FloorRun.Rooms[0].Kind);
        Assert.Equal(FloorRoomKind.Trash, FloorRun.Rooms[1].Kind);
        Assert.Equal(FloorRoomKind.Elite, FloorRun.Rooms[2].Kind);
        Assert.Equal(FloorRoomKind.Rest, FloorRun.Rooms[3].Kind);
        Assert.Equal(FloorRoomKind.Boss, FloorRun.Rooms[4].Kind);
        Assert.Equal("Carapace Engine Mk. II", FloorRun.Rooms[4].Name);
    }

    [Fact]
    public void Hp_and_mp_carry_into_the_next_fight()
    {
        FloorRun run = FloorRun.Start(PartyPreset.IceLattice, 20261004UL);
        BattleSimulator first = run.BeginFight();
        first.RunToEnd();
        Assert.Equal(FightOutcome.Victory, first.Outcome);
        int hp0 = first.Heroes[0].Hp;
        int mp0 = first.Heroes[0].Mp;
        first.Heroes[3].Hp = 999;
        first.Heroes[3].Mp = 15;
        first.Heroes[1].Hp = 0;
        first.Heroes[1].Mp = 0;
        run.Commit(first);

        Assert.Equal(1, run.RoomIndex);
        BattleSimulator second = run.BeginFight();
        Assert.Equal(999, second.Heroes[3].Hp);
        Assert.Equal(15, second.Heroes[3].Mp);
        Assert.Equal(0, second.Heroes[1].Hp);
        Assert.Equal(0, second.Heroes[1].Mp);
        Assert.Equal(hp0, second.Heroes[0].Hp);
        Assert.Equal(mp0, second.Heroes[0].Mp);
        HeroState[] fresh = CarapaceEncounter.CreateParty(PartyPreset.IceLattice);
        Assert.Equal(fresh[0].Ap.Centi, second.Heroes[0].Ap.Centi);
    }

    [Fact]
    public void Rest_restores_part_of_missing_hp_and_mp_and_raises_a_ko()
    {
        FloorRun run = FloorRun.Start(PartyPreset.ShatterChoir, 20261004UL);
        BattleSimulator mite = run.BeginFight();
        mite.RunToEnd();
        run.Commit(mite);
        BattleSimulator skitter = run.BeginFight();
        skitter.RunToEnd();
        run.Commit(skitter);
        BattleSimulator elite = run.BeginFight();
        elite.RunToEnd();
        Assert.Equal(FightOutcome.Victory, elite.Outcome);
        HeroState living = elite.Heroes[0];
        HeroState fallen = elite.Heroes[2];
        living.Hp = living.MaxHp / 2;
        living.Mp = living.MaxMp / 2;
        fallen.Hp = 0;
        fallen.Mp = 0;
        run.Commit(elite);

        Assert.Equal(FloorRoomKind.Rest, run.Current.Kind);
        int halfHp = run.Party[0].Hp;
        int halfMp = run.Party[0].Mp;
        int maxHp = run.Party[0].MaxHp;
        int maxMp = run.Party[0].MaxMp;
        run.Rest();

        Assert.Equal(halfHp + ((maxHp - halfHp) * FloorRun.RestHpOfMissingBp / 10_000), run.Party[0].Hp);
        Assert.Equal(halfMp + ((maxMp - halfMp) * FloorRun.RestMpOfMissingBp / 10_000), run.Party[0].Mp);
        Assert.Equal(run.Party[2].MaxHp * FloorRun.RestHpOfMissingBp / 10_000, run.Party[2].Hp);
        Assert.Equal(run.Party[2].MaxMp * FloorRun.RestMpOfMissingBp / 10_000, run.Party[2].Mp);
        Assert.Equal(FloorRoomKind.Boss, run.Current.Kind);

        HeroVitals full = FloorRun.Restore(new HeroVitals { Hp = maxHp, MaxHp = maxHp, Mp = maxMp, MaxMp = maxMp });
        Assert.Equal(maxHp, full.Hp);
        Assert.Equal(maxMp, full.Mp);
    }
}
