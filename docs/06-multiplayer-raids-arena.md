# 06 — Multiplayer: Ghost Protocol Arena, Alliance Raids, Netcode and Backend

---

## 6.1 Principles

1. **One simulation, everywhere.** `Resonance.Sim` (02 §2.16) is a deterministic, integer-only C# library. The Godot client, the arena validator and the raid server all run the same assembly, identified by a `SimVersion` hash.
2. **Clients submit inputs, never results.** The server either re-simulates (arena) or is the authority (raids).
3. **Async first.** The ranked mode (Ghost Protocol) never requires two players online at the same time. Only raids are real-time, and raids tolerate latency because CTB combat pauses at decision points.
4. **Small-team operable.** One backend language (C#), one database, one cache; all services run as containers.

---

## 6.2 Ghost Protocol: asynchronous arena

### 6.2.1 Concept

A player uploads a **Ghost**: a frozen snapshot of a 4–6 unit squad, including every hero's stats, gear, four loadouts, socketed chips and Gambit Deck. Ghosts fight other ghosts (and live challengers) on the server. Squads are ranked by **how few ticks they need to clear opponents with zero casualties**.

### 6.2.2 Modes

| Mode | Attacker control | Defender | Ranking |
|---|---|---|---|
| Ghost Duel (ranked) | Attacker's own ghost, gambits only | Matched ghost | Glicko-2 rating per ghost (6.3) |
| Live Assault (ranked) | Player plays live with Tactical Pause and manual commands | Matched ghost | Same rating; the input log is validated (6.3) |
| Weekly Gauntlet | Gambits or live | Fixed sequence of 5 curated ghosts, same for everyone that week | Leaderboard by total ticks (6.2.4) |

Tactical Pause in Live Assault costs no ticks; ranking is on simulated ticks, not wall-clock time, so pausing to think is never punished.

### 6.2.3 Squad legality

| Rule | Value |
|---|---|
| Squad size | 4–6 heroes |
| Duplicate heroes | Not allowed (each of the 24 heroes at most once) |
| Archetype limit | At most 2 heroes of the same archetype |
| Resource Battery limit | At most 2 |
| Legendary Prototypes | At most 3 per squad |
| Squad Power (SP) | Sum of hero Power Ratings (hero level × 10 + gear score); matchmaking brackets of 2,000 SP |
| Gambit Deck limits | As in 04 §4.12.5 (6–10 slots, 3 conditions per slot, 20-condition budget, 2 DEFER slots) |
| Arena normalization | All heroes are scaled to Level 50 reference stats; gear stat lines are normalized to Rare values ("Protocol Normalization") in the Normalized ladder; the Open ladder uses real gear |

### 6.2.4 Clear definition and scoring

A match is **cleared** when every defender unit is KO'd within the **tick limit of 120,000 ticks**.

| Outcome | Condition | Glicko score `s` |
|---|---|---|
| Flawless Clear | Cleared with 0 attacker KOs | 1.00 |
| Clear with casualties | Cleared, `k` attacker KOs | `max(0.60, 0.90 − 0.10 × k)` |
| Timeout | Not cleared by 120,000 ticks | 0.25 |
| Defeat | All attackers KO'd | 0.00 |

The defender ghost receives `1 − s`.

**Protocol Score** (leaderboard value, separate from rating):

```
Par         = 40,000 ticks (Normalized ladder) or per-Gauntlet-stage par set by designers
ClearTicks  = tick on which the last defender unit was KO'd
Base        = floor(10,000 × Par / ClearTicks), capped at 25,000
Flawless    = Base
Casualties  = floor(Base × (1 − 0.25 × k)), floored at 0
Timeout / Defeat = 0
```

- The **Flawless Board** (the headline leaderboard) ranks only Flawless Clears, by fewest ClearTicks. Ties break by lower total damage taken, then by earlier submission time.
- The **Protocol Board** ranks by season-total Protocol Score over the best 20 matches.
- Weekly Gauntlet total = sum of ClearTicks over the 5 stages; any non-flawless stage adds a 20,000-tick penalty per attacker KO; a timeout counts as 120,000 ticks plus 60,000.

Worked example: a Flawless Clear in 32,000 ticks scores floor(10,000 × 40,000 / 32,000) = 12,500. The same clear with 1 KO scores floor(12,500 × 0.75) = 9,375 and does not enter the Flawless Board.

### 6.2.5 Rating system: Glicko-2

| Parameter | Value |
|---|---|
| Initial rating / RD / volatility | 1,500 / 350 / 0.06 |
| System constant τ | 0.5 |
| Rating period | 24 hours (00:00 UTC; 18:00 or 19:00 CT depending on daylight saving) |
| Minimum RD | 50 (inactive ghosts' RD grows back toward 350 by the standard Glicko-2 period step) |
| Displayed rating | `rating − 2 × RD` (conservative), so new ghosts cannot top the ladder on luck |
| Matchmaking | Attacker is offered 3 defenders: one at −150, one at ±50, one at +150 rating from the attacker's ghost, all within the same SP bracket |
| Tiers | Bronze < 1,300 ≤ Silver < 1,550 ≤ Gold < 1,800 ≤ Resonant < 2,050 ≤ Apex |

Each ghost has its own rating; a player may keep 3 ghosts registered and the account's arena rank is the highest displayed rating among them.

### 6.2.6 Anti-cheese rules

| Exploit | Countermeasure |
|---|---|
| Client-side result forgery | Server never accepts a claimed result; it re-simulates (6.3) |
| Seed shopping (retrying until RNG is favorable) | Server issues the seed at match start: `seed = HMAC-SHA256(serverSecret, matchId)[0..8]`; each matchId can be attempted once; abandoning counts as a Defeat |
| Turtle defenders (heal loops that force timeouts) | Timeout gives the attacker `s = 0.25` (not a loss). A ghost whose defenses timeout in more than 40% of the last 50 matches is flagged **Stalling** and removed from matchmaking offers until re-uploaded with a changed deck |
| Infinite DEFER / stall gambits | 2 DEFER slots max, DEFER N ≤ 1,500; a unit that defers more than 30% of total fight ticks is forced to `USE [Attack]` |
| Farming the same weak defender many times | Rating gain vs the same defender within 24 h is multiplied by 1.0, 0.5, 0.25, then 0 |
| Win trading between accounts | Pairwise graph analysis: pairs with > 10 mutual matches per week and > 90% one-directional wins are reviewed; ratings from those matches are reverted on confirmation |
| Snapshot tampering | Ghost snapshots are canonicalized JSON with a SHA-256 hash stored server-side; defenders are loaded from the server copy only |
| Exploiting sim bugs | Leaderboard replays (top 100 per board) are re-validated on every new SimVersion; results that no longer reproduce are moved to a "Legacy" board rather than deleted |
| Multi-accounting to seed ghosts | Ranked arena requires a Steam account in good standing (Steam auth ticket) and account level 15 |
| Upload spam | 1 upload per ghost slot per hour; 30 ranked attacks per day |

### 6.2.7 Seasons and rewards

- Season length: 8 weeks. Soft reset at season start: `rating = 1,500 + 0.5 × (rating − 1,500)`, RD set to max(RD, 150).
- Rewards by final tier: Arena Marks (Bronze 200, Silver 400, Gold 700, Resonant 1,100, Apex 1,600), plus Apex top 100 receive an animated title and a Legendary Prototype selection token.

---

## 6.3 Deterministic replay and validation

### 6.3.1 Determinism requirements (enforced in `Resonance.Sim`)

| Requirement | Enforcement |
|---|---|
| No floating point | Banned-API analyzer: `float`, `double`, `decimal` arithmetic, `System.Math` floating overloads fail the build |
| Seeded RNG only | `Pcg32` streams keyed by `(seed, streamId)`; stream ids: 0 hit, 1 crit, 2 multi-attack, 3 interrupt, 4 proc/status, 5 boss AI, 6 loot (never used in arena) |
| Stable ordering | No `Dictionary`/`HashSet` iteration in sim logic; units stored in fixed arrays by slot; sorting uses a stable insertion sort with explicit tie-breakers (02 §2.2.3) |
| No wall-clock | Sim never reads `DateTime`, `Stopwatch` or `Environment.TickCount` |
| No threading | The sim is single-threaded per battle; parallelism happens only across battles |
| Versioning | `SimVersion = SHA-256(Resonance.Sim.dll IL bytes + compiled data tables)`; stored on every replay and ghost |

### 6.3.2 Replay file format (`.rsr`, CBOR-encoded)

| Field | Type | Content |
|---|---|---|
| magic | 4 bytes | `RSR1` |
| simVersion | 32 bytes | SimVersion hash |
| dataHash | 32 bytes | Hash of the ability/hero/boss tables |
| seed | uint64 | Match seed |
| attacker | GhostSnapshot | Full snapshot (6.3.4) |
| defender | GhostSnapshot | Full snapshot |
| inputs | array of Input | `{tick: int32, slot: uint8, verb: uint8, abilityId: uint16, targetPart: uint8, flags: uint8}`; empty for Ghost Duels |
| checkpoints | array of uint64 | xxHash64 of BattleState every 1,000 ticks |
| result | Result | outcome, clearTicks, casualties, finalStateHash |

A typical Live Assault replay is 6–20 KB.

### 6.3.3 Validation pipeline

1. Client requests a match: `POST /arena/matches` → server returns `matchId`, `seed`, defender snapshot, and a signed match ticket (expires in 2 hours).
2. Client plays (live) or simply requests simulation (Ghost Duel, which is server-only: the client never simulates ranked Ghost Duels, it only downloads the replay to watch).
3. For Live Assault, the client uploads `inputs` + `claimedResult` + `checkpoints` to `POST /arena/matches/{id}/submit`.
4. A **Validator worker** (headless .NET 8 console app referencing `Resonance.Sim`) re-simulates using the server copies of both snapshots, the server seed and the submitted inputs.
5. If the final state hash matches, the result is committed (rating update queued for the rating period, Protocol Score updated immediately).
6. If it mismatches, the first mismatching checkpoint is logged; the match is recorded as Defeat and the account's `desyncCount` increments. Three desyncs in 7 days trigger manual review (honest desyncs indicate a sim bug, which QA investigates via the checkpoint trail).
7. Inputs are validated for legality as they are replayed: an input for a unit that is not ready at that tick, or an ability the unit cannot use, invalidates the submission.

Validator cost: event-driven time skipping (02 §2.16.2) resolves a 40,000-tick fight in about 300 events; measured target ≤ 5 ms per match on one core, so one 4-vCPU container validates about 800 matches per second.

### 6.3.4 GhostSnapshot schema (canonical JSON, then hashed)

```json
{
  "schema": 3,
  "simVersion": "b3f1c07e9a4d52e81f6a0c3b7d9e2f4a6c8b1d3e5f708192a3b4c5d6e7f80912",
  "squad": [
    {
      "heroId": 17,
      "level": 50,
      "gear": { "idle": [ "g_4411", "g_1203" ], "fastCast": [ "g_0932" ], "midCast": [ "g_8812", "leg_02" ], "weaponSkill": [] },
      "chips": { "idle": [ "c_13:rare:+6" ], "fastCast": [ "c_06:epic:+7" ], "midCast": [ "c_03:rare:+4", "c_05:epic:+9" ], "weaponSkill": [] },
      "gambits": "1 IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II]\n6 IF Field [Always] -> DEFER [600]",
      "formationSlot": 3,
      "anchor": false
    }
  ],
  "ladder": "normalized"
}
```

---

## 6.4 Co-op Alliance Raids

### 6.4.1 Structure

- An **Alliance** is 2 or 3 **squads**. Each squad is 4–6 heroes controlled by one player.
- A **World Boss** is split into **decoupled phase instances**: each squad fights its own battle (its own timeline, its own sim instance) against a subset of the boss's parts, while all squads share one World Boss state.
- The user-specified split: **Squad 1** holds Core aggro, **Squad 2** handles Shield Nodes, and **Squad 3** (in 3-squad alliances) handles the Weapon Arm and adds. In 2-squad alliances, Squad 2 handles Shield Nodes and the Weapon Arm alternately (the arm attacks Squad 2's instance every 3rd boss action).

### 6.4.2 World Boss: Hollow-Sun Colossus

| Part | Assigned to | HP (3 squads) | HP (2 squads) | Stats | Concentration |
|---|---|---|---|---|---|
| Core | Squad 1 | 2,000,000 | 1,400,000 | ATK 820, DEF 600, INT 760, MEVA 480, ACC 380, EVA 150, AGI 13 | 90 |
| Shield Node A / B / C | Squad 2 | 150,000 each | 110,000 each | DEF 700, MEVA 550, AGI 10, no attacks of their own; each emits a pulse (below) | 90 |
| Weapon Arm | Squad 3 (or Squad 2 in 2-squad) | 600,000 | 420,000 | ATK 900, DEF 500, ACC 400, AGI 15 | 90 |
| Ember Adds (spawn waves) | Squad 3 (or Squad 2) | 18,000 each, 3 per wave | 14,000 each, 2 per wave | ATK 420, DEF 250, AGI 14 | 0 |

Resistances: Core Light −20% (weak), Darkness +40%; Nodes Lightning −20%, Fire +50%; Arm Ice −10%, Fire +30%.

### 6.4.3 Coupling rules (how squads affect each other)

| Rule | Source squad | Effect on other squads |
|---|---|---|
| Shield coverage | Squad 2 (nodes alive) | Each living Shield Node reduces all damage to the Core by 20% (3 nodes = −60%) and absorbs true damage into the node's HP instead |
| Node destroyed | Squad 2 | The Core loses that node's 20% reduction; Squad 1 gets −20% DEF Shatter on the Core for 6,000 ticks; nodes regenerate at full HP 30,000 ticks after destruction (each node independently) |
| Node pulse | Shield Nodes | Every 5,000 ticks each living node heals the Core for 0.5% of its Max HP |
| Arm suppression | Squad 3 | While the Weapon Arm is below 50% HP, the Core's "Cleave Relay" attack on Squad 1 is disabled |
| Arm unsuppressed | Squad 3 | Above 50% HP, every 4th Core action is Cleave Relay: Physical 2.20 × ATK on Squad 1's highest-enmity hero plus 1.10 × ATK on the rest |
| Adds leak | Squad 3 | Each Ember Add alive for more than 8,000 ticks explodes into Squad 1's instance: 6% Max HP Fire damage to all Squad 1 heroes (and Heat for Ash-Dravan) |
| **Apex → Global Magic Burst** | Any squad | A Level 3 detonation in any squad triggers a **Global Magic Burst** in every squad (6.4.4) |
| Core HP shared | Squad 1 primarily | All squads can damage the Core via Global Magic Bursts; Core HP is one pool |

### 6.4.4 Global Magic Burst

- **Trigger:** any Level 3 (Apex) detonation in any squad's instance, on any part.
- **Effect:** every squad (including the originator) receives a **Global Burst window** of 1,500 ticks, applied at the start of the next coupling epoch on each squad's own clock (6.5.3).
- **Element set:** the Apex's burst element set (Solar Apex: Light, Wind, Physical; Umbral Zero: Darkness, Ice; Magma Core: Fire, Earth; Tempest Crown: Wind, Lightning).
- **Target:** the Global Burst window applies to **every part in each squad's instance** plus the Core. Every squad can direct a spell at the Core during a Global Burst even if they are not assigned to it: during the window, each hero gains a "Rift Line" target option pointing at the Core.
- **Numbers:** standard Magic Burst effects (100% hit, +50% crit, resistance bypass, 50% MP refund); burst bonus = the Apex's bonus (Solar Apex +120% true, Umbral Zero +110% true, Magma Core +110%, Tempest Crown +110%).
- **Stacking:** a Global Burst and a local burst window on the same part do not stack; a spell uses whichever gives the larger BurstBucket. Only one Global Burst can be active per squad; a new Apex during an active Global Burst refreshes the window to 1,500 ticks from the next epoch.
- **Kith-Lir extension:** a Kith-Lir burst during a Global Burst extends the Global window only in its own squad (+600, cap +1,200).
- **Originator bonus:** the squad whose Apex triggered the Global Burst gets +10% Protocol Raid Score for that window's damage.

### 6.4.5 Phases

| Phase | Core HP | Changes |
|---|---|---|
| P1 "Eclipse Stance" | 100% – 70% | Nodes regenerate after 30,000 ticks; Arm attacks every action |
| P2 "Corona Split" | 70% – 35% | Nodes regenerate after 20,000 ticks; Core chants **Sunfall** every 6th action (Magical Light 1.80 × INT to all of Squad 1, CT 2,000, interruptible only by Tempest Crown); Ember Add waves every 10,000 ticks |
| P3 "Hollow Collapse" | 35% – 0% | Nodes stop regenerating; Frenzy (05 §5.2.5 formulas at ADM 60); every Global Burst also heals each squad for 5% Max HP |
| Hard enrage | 240,000 ticks on the Core squad's clock | Core ATK and INT ×3; Shield coverage becomes −90% |

### 6.4.6 Raid rewards

| Result | Reward |
|---|---|
| Kill | 3 Prototype Cores per player, 1 guaranteed Legendary roll at 15% (+pity), 2,000 Aether Shards |
| Kill under 150,000 Core-clock ticks | +1 Prototype Core, "Sunbreaker" title progress |
| Wipe of one squad | That squad's heroes are KO'd; the squad may **re-deploy** once after 20,000 ticks at 50% HP |
| Alliance wipe or enrage kill | 25% of kill Aether Shards, no Cores |

---

## 6.5 Netcode and synchronization model

### 6.5.1 Model summary

**Server-authoritative squad simulations with deterministic client prediction, coupled at fixed epochs.**

- The raid server runs one `BattleState` per squad plus one `WorldBossState`.
- Each client runs a local copy of **its own squad's** sim for presentation, fed by the same inputs, so it can render immediately.
- Clients send **commands**, the server applies them at an agreed tick, and the server sends per-epoch state hashes. A client whose hash differs rolls back to the last confirmed checkpoint and re-simulates with the server's inputs (cheap, because the sim is deterministic and event-driven).

### 6.5.2 Why this works for CTB

In CTB, player input matters only when a hero under manual control becomes ready. When that happens the squad's sim **stops and waits** (the "awaiting command" state, identical to auto-pause), so network latency never costs simulated time. Gambit-controlled heroes need no input at all. The only real-time pressure is keeping squads' clocks roughly aligned, handled by epochs.

### 6.5.3 Coupling epochs

| Parameter | Value |
|---|---|
| Epoch length | 500 ticks on each squad's clock (aligned with the VE decay boundary) |
| Cross-squad event collection | All coupling events generated by a squad in epoch E (Core damage, node kills, Apex triggers, add leaks) are sent to the server tagged `(E, squadId, seq)` |
| Application | The server applies all epoch-E events to every squad at the start of epoch E + 1, sorted by `(E, squadId, seq)` so the order is deterministic |
| Maximum clock skew | A squad cannot start epoch E + 3 until every squad has finished epoch E. A squad that hits the limit waits (HUD: "Synchronizing with Alliance") |
| Speed | Each squad chooses 1×, 2× or 4× presentation speed; the skew limit keeps fast squads from outrunning slow ones by more than 1,500 ticks |

### 6.5.4 Tactical Pause in raids

- Each squad has a **pause budget** of 90 real-time seconds per raid phase (P1, P2, P3). Awaiting-command stops also draw from this budget when they last longer than 3 seconds.
- When the budget is spent, pause becomes **slow-motion**: the squad's sim continues at 0.25× speed while the menu is open, and awaiting-command stops auto-resolve through the hero's Gambit Deck after 5 seconds.
- Pausing never blocks other squads by more than the skew limit.

### 6.5.5 Messages

| Message | Direction | Payload | Frequency |
|---|---|---|---|
| `Command` | Client → server | `{squadId, tick, slot, verb, abilityId, targetPart, flags}` | Only when issuing a manual action |
| `EpochReport` | Server → client | `{squadId, epoch, stateHash, confirmedInputs[]}` | Every 500 ticks of that squad's clock |
| `CouplingEvents` | Server → all clients | `{epoch, events[]}` | Every epoch that has any cross-squad event |
| `WorldBossState` | Server → all clients | `{coreHp, nodes[3]{hp, regenAt}, armHp, phase}` | Every epoch |
| `PauseState` | Client ↔ server | `{squadId, paused, budgetRemainingMs}` | On change |
| `Resync` | Server → client | `{checkpointTick, battleStateBlob}` | On desync or reconnect |

Transport: WebSocket over TLS via ASP.NET Core SignalR (MessagePack protocol). Measured target bandwidth under 4 KB/s per client.

### 6.5.6 Disconnects

- On disconnect, the squad switches every hero to Gambit Deck control and continues; the skew limit and pause budget still apply.
- Reconnect within 5 minutes restores control after a `Resync`. After 5 minutes the squad stays on gambits for the rest of the raid.

---

## 6.6 Backend recommendation

### 6.6.1 Options compared

| Criterion | A: Custom ASP.NET Core 8 + PostgreSQL + Redis (recommended) | B: Nakama + separate .NET sim service | C: PlayFab + Azure Functions (C#) |
|---|---|---|---|
| Language | C# end to end | Go/TypeScript/Lua server logic + C# sim service | C# functions, managed services |
| Shares `Resonance.Sim` directly | Yes (same assembly) | Only in the separate sim service | Yes, inside Functions (cold starts hurt validation latency) |
| Godot C# client | `Microsoft.AspNetCore.SignalR.Client` + `HttpClient` (standard .NET libraries run in Godot's .NET runtime on desktop) | `nakama-dotnet` client (official .NET SDK) | PlayFab C# SDK |
| Leaderboards | Redis sorted sets (self-built, simple) | Built-in | Built-in |
| Realtime raids | SignalR hubs hosting raid rooms | Nakama authoritative matches cannot run C# sim, so raids must live in the sim service anyway | Needs a separate realtime host (PlayFab Multiplayer Servers) |
| Ops burden for 1–3 people | Medium (you own everything, but it's one stack) | Medium-high (two stacks) | Low-medium, vendor lock-in and cost at scale |
| Cost at launch scale (10k DAU) | Low (2–4 small containers + managed Postgres + Redis) | Low-medium | Medium |

### 6.6.2 Recommended architecture (Option A)

```
                 ┌───────────────────────────────┐
 Godot C# client │ NetClient (Autoload)          │
                 │  HttpClient ──► REST API       │
                 │  SignalR client ──► Raid hub   │
                 └──────────┬────────────────────┘
                            │ TLS
               ┌────────────▼────────────┐
               │ Resonance.Api (ASP.NET) │  auth (Steam ticket), profiles, ghosts,
               │  minimal APIs + SignalR │  arena match tickets, leaderboards, raid lobby
               └──┬──────────┬───────────┘
                  │          │
       ┌──────────▼──┐   ┌───▼────────────────┐
       │ PostgreSQL  │   │ Redis              │ leaderboards (sorted sets), rate limits,
       │ accounts,   │   │ job queue (streams)│ raid room registry
       │ ghosts,     │   └───┬────────────────┘
       │ replays     │       │
       └─────────────┘   ┌───▼────────────────────┐
                         │ Resonance.Validator    │ N worker containers, headless .NET 8,
                         │ (references Sim)       │ re-simulate arena submissions
                         └────────────────────────┘
                         ┌────────────────────────┐
                         │ Resonance.RaidHost     │ SignalR hub; runs squad BattleStates
                         │ (references Sim)       │ and WorldBossState, epoch coupling
                         └────────────────────────┘
```

- **Identity:** Steam session tickets validated server-side with the Steam Web API; the client obtains tickets through a Steamworks C# wrapper (Steamworks.NET or Facepunch.Steamworks).
- **Storage:** replays over 30 days old (except leaderboard top 100) move to object storage (S3-compatible) as compressed `.rsr`.
- **Deployment:** Docker images; any container host (a single VPS with Docker Compose for alpha, a managed container service for launch).
- **Offline:** the full single-player game (dungeons, story) works offline; only arena and raids need the backend.

### 6.6.3 Core REST endpoints

| Method | Path | Purpose |
|---|---|---|
| POST | `/auth/steam` | Exchange Steam ticket for a session JWT (1 h) + refresh token |
| GET | `/profile` | Account, arena ratings, pity counters |
| PUT | `/ghosts/{slot}` | Upload a GhostSnapshot (validated for legality, hashed, stored) |
| GET | `/arena/offers` | 3 matchmaking offers |
| POST | `/arena/matches` | Create a match: returns matchId, seed, defender snapshot, signed ticket |
| POST | `/arena/matches/{id}/submit` | Submit Live Assault inputs and claimed result |
| GET | `/arena/matches/{id}/replay` | Download replay |
| GET | `/leaderboards/{board}?season=` | Flawless Board, Protocol Board, Gauntlet |
| POST | `/raids/lobbies` | Create an Alliance lobby |
| POST | `/raids/lobbies/{id}/join` | Join as a squad |
| WS | `/hubs/raid` | SignalR raid hub |

---

## 6.7 Godot C# client structure for online features

```
Autoloads/
  NetClient.cs            // HttpClient wrapper, JWT refresh, retry with exponential backoff, offline detection
  ArenaService.cs         // offers, matches, submission, replay download; async Task-based API
  RaidSession.cs          // SignalR HubConnection, epoch handling, rollback/resync against local Sim
Scenes/
  Arena/ArenaLobby.tscn   // offers, ghost slot management
  Arena/ReplayViewer.tscn // loads .rsr, drives BattleScene through SimBridge in replay mode (scrubbable)
  Raid/RaidLobby.tscn
  Raid/RaidBattle.tscn    // BattleScene + AllianceStrip (other squads' status, Global Burst banner)
```

- All network calls are `async Task` and are awaited from Godot nodes; results are marshaled back to the main thread with `CallDeferred` (or awaiting `ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame)`) before touching nodes.
- `RaidSession` never mutates `BattleState` from a network thread: incoming messages are queued into a `ConcurrentQueue` and applied in `_Process` before `SimBridge` advances the sim.
- Platform note: Godot 4.x C# projects export to Windows, macOS and Linux (including Steam Deck); C# web export is not available in Godot 4.x, so the game ships desktop-only. Mobile C# export is not a target for this project.
