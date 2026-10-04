namespace Resonance.Sim.Sim;

public enum FloorRoomKind
{
    Trash,
    Elite,
    Rest,
    Boss,
}

public readonly struct FloorRoom
{
    public FloorRoom(int index, FloorRoomKind kind, string name, string detail)
    {
        Index = index;
        Kind = kind;
        Name = name;
        Detail = detail;
    }

    public int Index { get; }

    public FloorRoomKind Kind { get; }

    public string Name { get; }

    public string Detail { get; }
}

public struct HeroVitals
{
    public string Name;
    public int Hp;
    public int MaxHp;
    public int Mp;
    public int MaxMp;

    public readonly bool Alive => Hp > 0;
}

/// <summary>
/// One linear floor. Fights are separate sims; only HP and MP move from one room to the next.
/// </summary>
public sealed class FloorRun
{
    public const int RestHpOfMissingBp = 7_000;
    public const int RestMpOfMissingBp = 9_900;
    public const int RoomCount = 5;

    public static FloorRoom[] Rooms { get; } =
    [
        new(0, FloorRoomKind.Trash, "Cinder Mite", "Trash. A short fire spit and a bite."),
        new(1, FloorRoomKind.Trash, "Slag Skitter", "Trash. An instant physical bite."),
        new(2, FloorRoomKind.Elite, "Kiln Warden", "Elite. A core and a brand."),
        new(3, FloorRoomKind.Rest, "Kiln Cool", "Rest. Regain part of the HP and MP you are missing."),
        new(4, FloorRoomKind.Boss, "Carapace Engine Mk. II", "Floor boss. The grey-box Carapace fight."),
    ];

    private FloorRun(int preset, ulong seed, HeroVitals[] party)
    {
        Preset = preset;
        Seed = seed;
        Party = party;
        WipeRoom = -1;
    }

    public int Preset { get; }

    public ulong Seed { get; }

    public int RoomIndex { get; private set; }

    public bool Finished { get; private set; }

    public bool Cleared { get; private set; }

    public int WipeRoom { get; private set; }

    public int Ticks { get; private set; }

    public HeroVitals[] Party { get; private set; }

    public FloorRoom Current => Rooms[RoomIndex];

    public int RoomsCleared => Cleared ? RoomCount : WipeRoom < 0 ? RoomIndex : WipeRoom;

    public ulong FightSeed => Seed + (ulong)RoomIndex;

    public static FloorRun Start(int preset, ulong seed)
    {
        HeroState[] heroes = CarapaceEncounter.CreateParty(preset);
        return new FloorRun(preset, seed, Snapshot(heroes));
    }

    public static FloorRun Simulate(int preset, ulong seed)
    {
        FloorRun run = Start(preset, seed);
        while (!run.Finished)
        {
            if (run.Current.Kind == FloorRoomKind.Rest)
            {
                run.Rest();
                continue;
            }

            BattleSimulator sim = run.BeginFight();
            sim.RunToEnd();
            run.Commit(sim);
        }

        return run;
    }

    public BattleSimulator BeginFight()
    {
        if (Finished || Current.Kind == FloorRoomKind.Rest)
        {
            throw new InvalidOperationException("This room is not a fight.");
        }

        BattleSimulator sim = RoomIndex switch
        {
            0 => ShaftEncounter.CinderMite(Preset, FightSeed),
            1 => ShaftEncounter.SlagSkitter(Preset, FightSeed),
            2 => ShaftEncounter.KilnWarden(Preset, FightSeed),
            _ => CarapaceEncounter.Create(Preset, FightSeed),
        };
        Apply(sim.Heroes, Party);
        return sim;
    }

    public void Commit(BattleSimulator sim)
    {
        Party = Snapshot(sim.Heroes);
        Ticks += sim.Tick;
        if (sim.Outcome != FightOutcome.Victory)
        {
            Finished = true;
            Cleared = false;
            WipeRoom = RoomIndex;
            return;
        }

        Advance();
    }

    public void Rest()
    {
        if (Finished || Current.Kind != FloorRoomKind.Rest)
        {
            throw new InvalidOperationException("Rest is only the kiln-cool room.");
        }

        for (int i = 0; i < Party.Length; i++)
        {
            Party[i] = Restore(Party[i]);
        }

        Advance();
    }

    public static HeroVitals Restore(HeroVitals hero)
    {
        if (hero.Hp <= 0)
        {
            hero.Hp = hero.MaxHp * RestHpOfMissingBp / 10_000;
            if (hero.Hp < 1)
            {
                hero.Hp = 1;
            }

            hero.Mp = hero.MaxMp * RestMpOfMissingBp / 10_000;
            return hero;
        }

        hero.Hp += (hero.MaxHp - hero.Hp) * RestHpOfMissingBp / 10_000;
        hero.Mp += (hero.MaxMp - hero.Mp) * RestMpOfMissingBp / 10_000;
        if (hero.Hp > hero.MaxHp)
        {
            hero.Hp = hero.MaxHp;
        }

        if (hero.Mp > hero.MaxMp)
        {
            hero.Mp = hero.MaxMp;
        }

        return hero;
    }

    public string ResultTitle => Cleared ? "Floor cleared" : "Run wiped";

    public string ResultBody
    {
        get
        {
            string where = Cleared
                ? $"Rooms {RoomCount}/{RoomCount}"
                : $"Wiped in {Rooms[WipeRoom].Name}. Rooms cleared {RoomsCleared}/{RoomCount}";
            int alive = 0;
            int hp = 0;
            int maxHp = 0;
            int mp = 0;
            int maxMp = 0;
            for (int i = 0; i < Party.Length; i++)
            {
                if (Party[i].Alive)
                {
                    alive++;
                }

                hp += Party[i].Hp;
                maxHp += Party[i].MaxHp;
                mp += Party[i].Mp;
                maxMp += Party[i].MaxMp;
            }

            return $"{PartyPreset.Name(Preset)} · seed {Seed}\n{where}\nCombat ticks {Ticks}\nStanding {alive}/{Party.Length}\nHP {hp}/{maxHp} · MP {mp}/{maxMp}";
        }
    }

    private void Advance()
    {
        RoomIndex++;
        if (RoomIndex >= Rooms.Length)
        {
            RoomIndex = Rooms.Length - 1;
            Finished = true;
            Cleared = true;
            WipeRoom = -1;
        }
    }

    private static void Apply(IReadOnlyList<HeroState> heroes, HeroVitals[] party)
    {
        int n = heroes.Count < party.Length ? heroes.Count : party.Length;
        for (int i = 0; i < n; i++)
        {
            heroes[i].Hp = party[i].Hp;
            heroes[i].Mp = party[i].Mp;
        }
    }

    private static HeroVitals[] Snapshot(IReadOnlyList<HeroState> heroes)
    {
        var party = new HeroVitals[heroes.Count];
        for (int i = 0; i < heroes.Count; i++)
        {
            HeroState hero = heroes[i];
            int hp = hero.Hp;
            if (hp < 0)
            {
                hp = 0;
            }

            if (hp > hero.MaxHp)
            {
                hp = hero.MaxHp;
            }

            int mp = hero.Mp;
            if (mp < 0)
            {
                mp = 0;
            }

            if (mp > hero.MaxMp)
            {
                mp = hero.MaxMp;
            }

            party[i] = new HeroVitals
            {
                Name = hero.Name,
                Hp = hp,
                MaxHp = hero.MaxHp,
                Mp = mp,
                MaxMp = hero.MaxMp,
            };
        }

        return party;
    }
}
