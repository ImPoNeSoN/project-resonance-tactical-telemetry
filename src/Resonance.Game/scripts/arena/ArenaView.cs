using System.Globalization;
using System.IO;
using Godot;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Boss-only 3D arena. The simulator stays authoritative; this node only listens.
/// </summary>
public partial class ArenaView : Node3D
{
    // Staggered diagonal, Korrith in front nearest the Carapace. Each step
    // runs across the camera's right and back in depth (~3.8 m) so bodies
    // sit in separate columns and a Keratin Bastion dome (radius 1.15 m)
    // does not swallow the neighbor.
    private static readonly Vector3[] Formation =
    [
        new(-3.4f, 0f, -0.7f),
        new(-6.5f, 0f, -2.35f),
        new(-9.6f, 0f, -4.0f),
        new(-12.6f, 0f, -5.65f),
    ];

    private static readonly Vector3 BossSpot = new(0.35f, 0f, 6.9f);
    private static readonly Vector3 CameraLook = new(-2.2f, 0.85f, 0.2f);
    private static readonly Vector3 CameraBack = new(0.48f, 0.24f, -0.84f);
    private const float CameraDistance = 22.5f;
    private const float FrameMargin = 0.60f;

    private SimBridge? _bridge;
    private Camera3D? _camera;
    private bool _active;
    private bool _worldReady;
    private bool _fpsPrinted;
    private double _fpsWait;
    private int _frames;
    private string _partyKey = "";
    private string _capture = "";
    private int _shotsLeft = 4;
    private bool _didBash;
    private bool _didBastion;
    private bool _didPart;
    private bool _openingSent;
    private double _hold;
    private bool _frozen;
    private bool _quiet;
    private Vector3 _kick;
    private readonly List<string> _recent = new();
    private readonly HashSet<string> _missing = new();
    private readonly List<Actor> _actors = new();
    private readonly List<PartAnchor> _parts = new();
    private Node3D? _bossRoot;
    private AnimationPlayer? _bossPlayer;
    private double _bossBusy;
    private string _bossClip = "";
    private bool _bossDead;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        _bridge.LogLine += OnLog;
        _bridge.StateChanged += OnState;
        _capture = OS.GetEnvironment("RESONANCE_ARENA_CAPTURE");
        if (_capture == "sequence")
        {
            try
            {
                File.Delete("/tmp/arena-shot");
            }
            catch (IOException)
            {
                // The shot file is only a capture latch.
            }
        }
    }

    public override void _ExitTree()
    {
        if (_bridge == null)
        {
            return;
        }

        _bridge.LogLine -= OnLog;
        _bridge.StateChanged -= OnState;
    }

    public void SetActive(bool active)
    {
        _active = active;
        Visible = active;
        ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        if (active)
        {
            EnsureWorld();
        }
    }

    public override void _Process(double delta)
    {
        if (!_active || !_worldReady || _bridge == null)
        {
            return;
        }

        _frames++;
        float step = (float)delta;
        _kick = _kick.Lerp(Vector3.Zero, 1f - Mathf.Exp(-5.5f * step));
        FrameCamera();

        foreach (Actor actor in _actors)
        {
            if (actor.Busy > 0)
            {
                actor.Busy -= delta;
            }

            if (actor.Flinch > 0)
            {
                actor.Flinch -= delta;
                if (actor.Body != null)
                {
                    float back = actor.Flinch > 0 ? (float)actor.Flinch * 0.35f : 0f;
                    actor.Body.Position = new Vector3(0f, actor.BodyRestY, -back);
                }
            }

            if (actor.Dome != null)
            {
                UpdateDome(actor, delta);
            }

            if (actor.TrailFor > 0)
            {
                actor.TrailFor -= delta;
                if (actor.Trail != null)
                {
                    ArenaVfx.SetEmitting(actor.Trail, actor.TrailFor > 0);
                }

                if (actor.Streak != null)
                {
                    actor.Streak.Visible = actor.TrailFor > 0;
                }
            }
        }

        _bossBusy -= delta;
        foreach (PartAnchor part in _parts)
        {
            if (part.HideIn > 0)
            {
                part.HideIn -= delta;
                if (part.HideIn <= 0)
                {
                    SetMeshes(part, false);
                }
            }

            if (part.Halo != null && part.Halo.Visible)
            {
                part.Halo.RotateY(step * 1.6f);
            }
        }

        if (!_fpsPrinted)
        {
            _fpsWait += delta;
            if (_fpsWait >= 2.0)
            {
                _fpsPrinted = true;
                GD.Print($"ARENA_FPS {Engine.GetFramesPerSecond().ToString("0", CultureInfo.InvariantCulture)}");
            }
        }

        PollCapture(delta);
    }

    private void EnsureWorld()
    {
        if (_worldReady)
        {
            Sync();
            return;
        }

        _worldReady = true;
        string method = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString();
        bool device = RenderingServer.GetRenderingDevice() != null;
        GD.Print($"ARENA_RENDERER {method} rendering_device {(device ? "yes" : "no")} gpu_particles {(ArenaVfx.GpuParticles ? "yes" : "no")}");

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color("07090d"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("243044"),
            AmbientLightEnergy = 0.55f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            GlowEnabled = true,
            GlowIntensity = 0.55f,
            GlowStrength = 0.75f,
            GlowBloom = 0.1f,
            GlowHdrThreshold = 0.85f,
            FogEnabled = true,
            FogLightColor = new Color("101624"),
            FogDensity = 0.018f,
        };
        AddChild(new WorldEnvironment { Environment = env });

        _camera = new Camera3D { Current = true };
        AddChild(_camera);
        FrameCamera();

        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-52f, 28f, 0f),
            LightColor = new Color("d5e2f0"),
            LightEnergy = 0.85f,
            ShadowEnabled = false,
        });
        AddChild(Light(new Vector3(-2.2f, 2.6f, -1.5f), "00e5ff", 3.2f, 14f));
        AddChild(Light(new Vector3(2.4f, 3.4f, 6.2f), "e13cff", 3.6f, 16f));
        AddChild(Light(new Vector3(3.8f, 4.2f, -5.5f), "c5d8ea", 2.2f, 18f));
        AddChild(Light(new Vector3(0f, 6.2f, 1.5f), "f2f6fb", 1.4f, 24f));

        var floor = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(30f, 0.16f, 24f) },
            Position = new Vector3(0f, -0.08f, 2f),
        };
        var floorMat = new StandardMaterial3D
        {
            AlbedoColor = new Color("12161c"),
            Metallic = 0.78f,
            Roughness = 0.38f,
        };
        floor.SetSurfaceOverrideMaterial(0, floorMat);
        AddChild(floor);
        AddChild(Strip(new Vector3(0f, 0.02f, -0.4f), new Vector3(12f, 0.03f, 0.07f), "00e5ff"));
        AddChild(Strip(new Vector3(0.3f, 0.02f, 5.2f), new Vector3(9f, 0.03f, 0.07f), "e13cff"));
        AddChild(ColumnMesh(new Vector3(-14.2f, 2.2f, -7.2f)));
        AddChild(ColumnMesh(new Vector3(11f, 2.2f, -7.2f)));
        AddChild(ColumnMesh(new Vector3(-9f, 2.2f, 11f)));
        AddChild(ColumnMesh(new Vector3(9f, 2.2f, 11f)));
        AddChild(Wall(new Vector3(0f, 2.6f, 13.2f), new Vector3(22f, 5.2f, 0.35f)));

        Sync();
    }

    private static OmniLight3D Light(Vector3 at, string html, float energy, float range) => new()
    {
        Position = at,
        LightColor = new Color(html),
        LightEnergy = energy,
        OmniRange = range,
        ShadowEnabled = false,
    };

    private static MeshInstance3D Strip(Vector3 at, Vector3 size, string html)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(html),
            EmissionEnabled = true,
            Emission = new Color(html),
            EmissionEnergyMultiplier = 4f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        return new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = at,
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D ColumnMesh(Vector3 at)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color("1a2030"),
            Metallic = 0.85f,
            Roughness = 0.32f,
        };
        return new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.45f, 4.4f, 0.45f) },
            Position = at,
            MaterialOverride = mat,
        };
    }

    private static MeshInstance3D Wall(Vector3 at, Vector3 size)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color("10141c"),
            Metallic = 0.7f,
            Roughness = 0.45f,
        };
        return new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = at,
            MaterialOverride = mat,
        };
    }

    private void OnLog(string line)
    {
        _recent.Add(line);
        if (_recent.Count > 16)
        {
            _recent.RemoveAt(0);
        }

        if (_quiet || !_worldReady)
        {
            return;
        }

        PresentLine(line);
    }

    private void OnState()
    {
        if (_active && _worldReady)
        {
            Sync();
        }
    }

    private void Sync()
    {
        if (_bridge == null)
        {
            return;
        }

        BattleSimulator sim = _bridge.Simulation;
        EnsureActors(sim);
        int tick = sim.Tick;
        if (_quiet)
        {
            for (int i = 0; i < _parts.Count && i < sim.Boss.Parts.Length; i++)
            {
                BossPartState part = sim.Boss.Parts[i];
                bool down = part.Hp <= 0 || !part.Active;
                if (down && !_parts[i].PlayedBreak)
                {
                    _parts[i].PlayedBreak = true;
                    _parts[i].Deferred = true;
                }
            }

            return;
        }

        if (_frozen)
        {
            UpdateIcons(sim, tick);
            return;
        }

        for (int i = 0; i < _parts.Count && i < sim.Boss.Parts.Length; i++)
        {
            BossPartState part = sim.Boss.Parts[i];
            PartAnchor anchor = _parts[i];
            bool down = part.Hp <= 0 || !part.Active;
            if (down && !anchor.PlayedBreak)
            {
                anchor.PlayedBreak = true;
                if (_quiet)
                {
                    anchor.Deferred = true;
                }
                else
                {
                    PlayBreak(anchor);
                }
            }
            else if (!down && anchor.PlayedBreak)
            {
                anchor.PlayedBreak = false;
                anchor.Deferred = false;
                anchor.HideIn = 0;
                SetMeshes(anchor, true);
            }

            bool stunned = part.Hp > 0 && tick < part.StunExpires;
            if (anchor.StunIcon != null)
            {
                anchor.StunIcon.Visible = stunned;
            }

            if (anchor.Halo != null)
            {
                anchor.Halo.Visible = stunned;
            }

            if (anchor.ChainIcon != null)
            {
                Texture2D? chain = HudSkin.Chain(part.Property);
                anchor.ChainIcon.Visible = chain != null;
                anchor.ChainIcon.Texture = chain;
            }
        }

        if (_bossBusy <= 0 && !_bossDead)
        {
            bool anyStun = false;
            for (int i = 0; i < sim.Boss.Parts.Length; i++)
            {
                if (sim.Boss.Parts[i].Hp > 0 && tick < sim.Boss.Parts[i].StunExpires)
                {
                    anyStun = true;
                    break;
                }
            }

            if (sim.Outcome == FightOutcome.Victory)
            {
                _bossDead = true;
                PlayBoss("death", false, 2.0);
            }
            else if (anyStun)
            {
                LoopBoss("stunned");
            }
            else if (sim.Boss.Casting)
            {
                LoopBoss("charge");
            }
            else
            {
                LoopBoss("idle");
            }
        }

        for (int i = 0; i < _actors.Count && i < sim.Heroes.Count; i++)
        {
            Actor actor = _actors[i];
            HeroState hero = sim.Heroes[i];
            if (!hero.IsAlive && !actor.Dead)
            {
                actor.Dead = true;
                actor.Busy = 2.2;
                Play(actor, "death", false);
            }
            else if (hero.IsAlive && actor.Busy <= 0 && !actor.Dead)
            {
                bool bastion = hero.PhysicalHitsMitigated && tick < hero.PhysDtExpires;
                if (bastion && actor.Player != null && actor.Player.CurrentAnimation == actor.Resolve("keratin_bastion"))
                {
                    // The clip ends held in the brace.
                }
                else
                {
                    Loop(actor, "ready");
                }
            }

            if (actor.ShieldIcon != null)
            {
                bool shield = (hero.Absorb > 0 && tick < hero.AbsorbExpires) ||
                              (hero.PhysicalHitsMitigated && tick < hero.PhysDtExpires);
                actor.ShieldIcon.Visible = shield;
            }

            if (actor.RegenIcon != null)
            {
                actor.RegenIcon.Visible = hero.RegenPerPulse > 0 && tick < hero.RegenExpires;
            }
        }
    }

    private void UpdateIcons(BattleSimulator sim, int tick)
    {
        for (int i = 0; i < _parts.Count && i < sim.Boss.Parts.Length; i++)
        {
            BossPartState part = sim.Boss.Parts[i];
            bool stunned = part.Hp > 0 && tick < part.StunExpires;
            Sprite3D? stunIcon = _parts[i].StunIcon;
            if (stunIcon != null)
            {
                stunIcon.Visible = stunned;
            }

            MeshInstance3D? halo = _parts[i].Halo;
            if (halo != null)
            {
                halo.Visible = stunned;
            }
        }
    }

    private void EnsureActors(BattleSimulator sim)
    {
        string key = sim.Boss.Name;
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            key += "|" + sim.Heroes[i].Name;
        }

        if (key == _partyKey && _actors.Count == sim.Heroes.Count && _bossRoot != null)
        {
            return;
        }

        _partyKey = key;
        foreach (Actor actor in _actors)
        {
            actor.Root.QueueFree();
        }

        _actors.Clear();
        _parts.Clear();
        _bossRoot?.QueueFree();
        _bossRoot = null;
        _bossPlayer = null;
        _bossDead = false;
        _bossBusy = 0;

        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            Vector3 spot = i < Formation.Length ? Formation[i] : new Vector3(i * 1.2f, 0f, -4f);
            _actors.Add(BuildHero(sim.Heroes[i], spot, i));
        }

        BuildBoss(sim);
    }

    private Actor BuildHero(HeroState hero, Vector3 spot, int index)
    {
        var root = new Node3D { Position = spot, Name = hero.Name };
        AddChild(root);
        root.LookAt(new Vector3(BossSpot.X, 1.2f, BossSpot.Z), Vector3.Up, true);

        bool korrith = hero.Name.StartsWith("Korrith", StringComparison.Ordinal);
        var actor = new Actor { Name = hero.Name, Root = root, Home = spot };
        if (korrith)
        {
            Node3D? model = LoadModel("res://assets/models/proof/korrith_vael_dun.glb");
            if (model == null)
            {
                GD.PrintErr("ARENA_IMPORT missing korrith_vael_dun.glb");
            }
            else
            {
                root.AddChild(model);
                actor.RealModel = true;
                actor.Player = FindPlayer(model);
                actor.Skeleton = model.FindChild("Skeleton3D", true, false) as Skeleton3D;
                LogClips("korrith", actor.Player);
                if (actor.Skeleton != null)
                {
                    actor.BastionBone = actor.Skeleton.FindBone("FX_Bastion");
                    actor.HandBone = actor.Skeleton.FindBone("RightHand_Prop");
                    AttachTrail(actor);
                }

                actor.Dome = ArenaVfx.Dome();
                root.AddChild(actor.Dome);
            }
        }

            if (!actor.RealModel)
            {
                Color color = RoleColor(hero.Name, index);
                var body = Placeholder(color);
                actor.Body = body;
                actor.BodyRestY = 0f;
                root.AddChild(body);
            }

            string plate = korrith && actor.RealModel ? hero.Name.Split(' ')[0] : hero.Name + "\nPLACEHOLDER";
            AddNameplate(root, plate, korrith && actor.RealModel ? 2.35f : 2.55f);
        actor.ShieldIcon = Icon(root, HudSkin.Status("shield"), new Vector3(-0.28f, 2.55f, 0f));
        actor.RegenIcon = Icon(root, HudSkin.Status("regen"), new Vector3(0.28f, 2.55f, 0f));
        actor.StunIcon = Icon(root, HudSkin.Status("stun"), new Vector3(0f, 2.85f, 0f));
        Loop(actor, "ready");
        return actor;
    }

    private void AttachTrail(Actor actor)
    {
        if (actor.Skeleton == null || actor.HandBone < 0)
        {
            return;
        }

        var hand = new BoneAttachment3D { BoneName = "RightHand_Prop" };
        actor.Skeleton.AddChild(hand);
        var sparks = ArenaVfx.Sparks(new Color(0.95f, 0.78f, 0.45f), 32, 2.4f, oneShot: false);
        if (sparks is Node3D sparkNode)
        {
            sparkNode.Position = new Vector3(0f, 0f, 0.35f);
        }

        hand.AddChild(sparks);
        actor.Trail = sparks;
        var streak = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.08f, 1.15f) },
            Position = new Vector3(0f, 0f, 0.55f),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = new Color(1f, 0.72f, 0.35f, 0.85f),
        };
        streak.MaterialOverride = mat;
        hand.AddChild(streak);
        actor.Streak = streak;
    }

    private void BuildBoss(BattleSimulator sim)
    {
        var root = new Node3D { Position = BossSpot, Name = "Carapace" };
        AddChild(root);
        root.LookAt(new Vector3(0f, 1.4f, -3.2f), Vector3.Up, true);
        _bossRoot = root;

        Node3D? loaded = LoadModel("res://assets/models/proof/carapace_engine_mk2.glb");
        Node3D model;
        if (loaded == null)
        {
            GD.PrintErr("ARENA_IMPORT missing carapace_engine_mk2.glb");
            model = new Node3D();
            root.AddChild(model);
        }
        else
        {
            model = loaded;
            root.AddChild(model);
            _bossPlayer = FindPlayer(model);
            LogClips("carapace", _bossPlayer);
        }

        var skeleton = model.FindChild("Skeleton3D", true, false) as Skeleton3D;
        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            BossPartState part = sim.Boss.Parts[i];
            var anchor = new PartAnchor { Name = part.Name };
            string meshToken = part.Name switch
            {
                "Core" => "Carapace_Core",
                "Weapon Arm" => "Carapace_WeaponArm",
                "Shield" => "Carapace_Shield",
                _ => "",
            };
            string bone = part.Name switch
            {
                "Core" => "Core",
                "Weapon Arm" => "WeaponArm",
                "Shield" => "Shield",
                _ => "",
            };
            string clip = part.Name switch
            {
                "Core" => "part_destroyed_core",
                "Weapon Arm" => "part_destroyed_weapon_arm",
                "Shield" => "part_destroyed_shield",
                _ => "",
            };
            anchor.Clip = clip;
            if (meshToken.Length > 0)
            {
                foreach (Node node in model.FindChildren("*", "", true, false))
                {
                    string name = node.Name.ToString();
                    if (name == meshToken || name.StartsWith(meshToken, StringComparison.Ordinal))
                    {
                        anchor.Meshes.Add(node);
                    }
                }
            }

            Node3D hang = root;
            if (skeleton != null && bone.Length > 0 && skeleton.FindBone(bone) >= 0)
            {
                var attachment = new BoneAttachment3D { BoneName = bone };
                skeleton.AddChild(attachment);
                hang = attachment;
            }

            anchor.Anchor = hang;
            anchor.StunIcon = Icon(hang, HudSkin.Status("stun"), new Vector3(0f, 1.1f, 0f));
            anchor.ChainIcon = Icon(hang, null, new Vector3(0.45f, 1.1f, 0f));
            anchor.Halo = HaloRing();
            hang.AddChild(anchor.Halo);
            _parts.Add(anchor);
            GD.Print($"ARENA_PART {part.Name} meshes {anchor.Meshes.Count} bone {(hang is BoneAttachment3D ? bone : "offset")}");
        }

        LoopBoss("idle");
    }

    private static MeshInstance3D HaloRing()
    {
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = new Color("ffc93c"),
            EmissionEnabled = true,
            Emission = new Color("ffc93c"),
            EmissionEnergyMultiplier = 3f,
        };
        return new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.28f, OuterRadius = 0.4f, Rings = 8, RingSegments = 14 },
            MaterialOverride = mat,
            Position = new Vector3(0f, 0.85f, 0f),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private void PresentLine(string line)
    {
        string text = StripTick(line);
        if (text.Contains("Keratin Bastion expired", StringComparison.Ordinal))
        {
            return;
        }

        if (text.Contains("Keratin Bastion.", StringComparison.Ordinal))
        {
            Actor? hero = FindActor(MatchHero(text));
            if (hero != null)
            {
                hero.Bastion = true;
                hero.BastionAge = 0;
                hero.Busy = 1.6;
                Play(hero, "keratin_bastion", false);
            }

            return;
        }

        if (text.StartsWith("Victory", StringComparison.Ordinal))
        {
            _bossDead = true;
            PlayBoss("death", false, 2.0);
            return;
        }

        if (text.StartsWith("Boss ", StringComparison.Ordinal) && text.Contains(" starts on ", StringComparison.Ordinal))
        {
            if (_bossBusy <= 0)
            {
                LoopBoss("charge");
            }

            return;
        }

        if (text.StartsWith("Boss ", StringComparison.Ordinal) && text.Contains(" → ", StringComparison.Ordinal))
        {
            PlayBoss("attack", false, 1.47);
            string heroName = AfterArrow(text);
            Actor? hero = FindActor(heroName);
            if (hero != null)
            {
                hero.Busy = 0.67;
                Play(hero, "hit_react", false);
                hero.Flinch = 0.25;
            }

            int damage = ParseDamage(text);
            SpawnFloat(HeroPoint(heroName), text.Contains(" miss", StringComparison.Ordinal) ? "Miss" : damage.ToString(CultureInfo.InvariantCulture), new Color("ffb080"));
            ImpactBurst.Spawn(this, HeroPoint(heroName), ImpactKind.Hit);
            if (damage >= 800)
            {
                Nudge(1.1f);
            }

            return;
        }

        if (text.Contains(" starts ", StringComparison.Ordinal))
        {
            string heroName = MatchHero(text);
            if (heroName.Length == 0)
            {
                return;
            }

            string ability = AbilityOf(text, heroName);
            Windup(heroName, ability);
            return;
        }

        if (text.Contains(" → ", StringComparison.Ordinal) && text.Contains("Dmg ", StringComparison.Ordinal))
        {
            ResolveHit(text);
        }
    }

    private void Windup(string heroName, string ability)
    {
        Actor? hero = FindActor(heroName);
        if (hero == null)
        {
            return;
        }

        if (ability.Contains("Shield Bash", StringComparison.Ordinal))
        {
            hero.Busy = 1.2;
            Play(hero, "shield_bash", false);
            return;
        }

        if (ability.Contains("Keratin Bastion", StringComparison.Ordinal))
        {
            hero.Bastion = true;
            hero.BastionAge = 0;
            hero.Busy = 1.6;
            Play(hero, "keratin_bastion", false);
            return;
        }

        if (ability is "Attack" or "Seismic Maul" || hero.RealModel)
        {
            hero.Busy = 1.33;
            hero.TrailFor = 0.55;
            Play(hero, "attack", false);
            return;
        }

        hero.Flinch = -0.2;
    }

    private void ResolveHit(string text)
    {
        string heroName = MatchHero(text);
        string ability = heroName.Length == 0 ? "" : AbilityOf(text, heroName);
        string partName = AfterArrow(text);
        int damage = ParseDamage(text);
        bool miss = text.Contains(" miss", StringComparison.Ordinal);
        Vector3 at = PartPoint(partName);
        SpawnFloat(at + new Vector3(0f, 0.6f, 0f), miss ? "Miss" : damage.ToString(CultureInfo.InvariantCulture), new Color("f4fbff"));

        if (ability.Contains("Shield Bash", StringComparison.Ordinal))
        {
            Actor? hero = FindActor(heroName);
            if (hero != null)
            {
                hero.Busy = 1.2;
                Play(hero, "shield_bash", false);
            }

            ImpactBurst.Spawn(this, at, ImpactKind.Bash);
            Nudge(1.6f);
            return;
        }

        if (ability is "Attack" or "Seismic Maul")
        {
            Actor? hero = FindActor(heroName);
            if (hero != null)
            {
                hero.Busy = 1.33;
                hero.TrailFor = 0.5;
                Play(hero, "attack", false);
                SpawnArc(hero, at);
            }

            ImpactBurst.Spawn(this, at, ImpactKind.Attack);
            if (damage >= 1200)
            {
                Nudge(1f);
            }

            return;
        }

        ImpactBurst.Spawn(this, at, ImpactKind.Hit);
        if (damage >= 1500)
        {
            Nudge(0.8f);
        }
    }

    private void SpawnArc(Actor hero, Vector3 to)
    {
        Vector3 from = hero.Root.GlobalPosition + new Vector3(0.4f, 1.5f, 0.2f);
        if (hero.Skeleton != null && hero.HandBone >= 0)
        {
            Transform3D pose = hero.Skeleton.GlobalTransform * hero.Skeleton.GetBoneGlobalPose(hero.HandBone);
            from = pose.Origin;
        }

        for (int i = 0; i < 6; i++)
        {
            float t = (i + 1) / 6f;
            Vector3 point = from.Lerp(to, t);
            point.Y += Mathf.Sin(t * Mathf.Pi) * 0.7f;
            var plate = new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(0.18f, 0.55f) },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            var mat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                AlbedoColor = new Color(1f, 0.7f, 0.3f, 0.8f),
            };
            plate.MaterialOverride = mat;
            AddChild(plate);
            plate.GlobalPosition = point;
            plate.LookAt(point + (to - from).Normalized(), Vector3.Up);
            var fade = new FadePlate { Life = 0.4f };
            plate.AddChild(fade);
            fade.Bind(plate, mat);
        }
    }

    private void PlayBreak(PartAnchor part)
    {
        if (part.Clip.Length > 0)
        {
            PlayBoss(part.Clip, false, 1.33);
        }

        part.HideIn = 1.33;
    }

    private void UpdateDome(Actor actor, double delta)
    {
        if (actor.Dome == null || _bridge == null)
        {
            return;
        }

        BattleSimulator sim = _bridge.Simulation;
        bool up = false;
        int tick = sim.Tick;
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            HeroState hero = sim.Heroes[i];
            if (hero.Name == actor.Name && hero.PhysicalHitsMitigated && tick < hero.PhysDtExpires)
            {
                up = true;
                break;
            }
        }

        actor.Bastion = up;
        actor.Dome.Visible = up || actor.BastionAge < 1.6 && actor.BusyClip == "keratin_bastion";
        if (!actor.Dome.Visible)
        {
            return;
        }

        actor.BastionAge += delta;
        float pulse = 1f;
        double age = actor.BastionAge;
        if (age < 1.6)
        {
            float u = (float)(age / 1.6);
            pulse = u < 0.29f ? Mathf.Lerp(0.08f, 1.25f, u / 0.29f)
                : u < 0.5f ? Mathf.Lerp(1.25f, 0.95f, (u - 0.29f) / 0.21f)
                : u < 0.75f ? Mathf.Lerp(0.95f, 1.12f, (u - 0.5f) / 0.25f)
                : Mathf.Lerp(1.12f, 1f, (u - 0.75f) / 0.25f);
        }

        if (actor.Skeleton != null && actor.BastionBone >= 0)
        {
            Vector3 scale = actor.Skeleton.GetBonePose(actor.BastionBone).Basis.Scale;
            float bone = (scale.X + scale.Y + scale.Z) / 3f;
            if (bone > 0.2f)
            {
                pulse = bone;
            }
        }

        actor.Dome.Scale = Vector3.One * Mathf.Clamp(pulse, 0.05f, 1.45f);
        if (actor.Dome.MaterialOverride is ShaderMaterial mat)
        {
            mat.SetShaderParameter("pulse", pulse);
        }
    }

    private void PollCapture(double delta)
    {
        if (_capture != "sequence" || _bridge == null || _shotsLeft <= 0)
        {
            return;
        }

        if (_hold > 0)
        {
            _hold -= delta;
            return;
        }

        if (!_openingSent)
        {
            if (_frames < 20)
            {
                return;
            }

            _openingSent = true;
            _frozen = false;
            ArenaVfx.HoldFrames = false;
            WriteShot("opening");
            _hold = 14;
            _recent.Clear();
            return;
        }

        BattleSimulator sim = _bridge.Simulation;
        if (sim.Outcome != FightOutcome.Ongoing && !_didPart)
        {
            WriteShot("ended");
            _shotsLeft = 0;
            return;
        }

        if (_bridge.Paused)
        {
            _bridge.TogglePause();
        }

        _quiet = true;
        _frozen = false;
        ArenaVfx.HoldFrames = false;
        for (int n = 0; n < 80 && sim.Outcome == FightOutcome.Ongoing; n++)
        {
            if (!_bridge.Step())
            {
                break;
            }

            sim = _bridge.Simulation;
            if (!_didBash && Saw("Shield Bash →"))
            {
                FinishShot(sim, "bash");
                _didBash = true;
                return;
            }

            if (!_didBastion && SawBastion())
            {
                FinishShot(sim, "bastion");
                _didBastion = true;
                return;
            }

            if (!_didPart && FirstDown(sim) >= 0)
            {
                FinishShot(sim, "part");
                _didPart = true;
                _shotsLeft = 0;
                return;
            }
        }

        _quiet = false;
    }

    private void FinishShot(BattleSimulator sim, string shot)
    {
        _quiet = false;
        _frozen = true;
        ArenaVfx.HoldFrames = true;
        if (_bridge != null && !_bridge.Paused)
        {
            _bridge.TogglePause();
        }

        if (shot == "bash")
        {
            int index = StunnedPart(sim);
            string part = index >= 0 ? sim.Boss.Parts[index].Name : "Shield";
            Actor? hero = FindActor("Korrith Vael-Dun");
            if (hero != null)
            {
                Freeze(hero, "shield_bash", 0.5);
            }

            Vector3 at = PartPoint(part);
            ImpactBurst.Spawn(this, at, ImpactKind.Bash);
            SpawnFloat(at + new Vector3(0f, 0.7f, 0f), "Bash", new Color("9af6ff"));
            if (index >= 0)
            {
                ShowStun(index);
            }

            Nudge(1.4f);
        }
        else if (shot == "bastion")
        {
            Actor? hero = FindActor("Korrith Vael-Dun");
            if (hero != null)
            {
                hero.Bastion = true;
                hero.BastionAge = 0.9;
                Freeze(hero, "keratin_bastion", 0.85);
                if (hero.Dome != null)
                {
                    hero.Dome.Visible = true;
                    hero.Dome.Scale = Vector3.One * 1.12f;
                    if (hero.Dome.MaterialOverride is ShaderMaterial mat)
                    {
                        mat.SetShaderParameter("pulse", 1.12f);
                    }
                }
            }
        }
        else if (shot == "part")
        {
            int index = FirstDown(sim);
            if (index >= 0)
            {
                PartAnchor part = _parts[index];
                part.PlayedBreak = true;
                part.Deferred = false;
                if (part.Clip.Length > 0 && _bossPlayer != null)
                {
                    PlayBoss(part.Clip, false, 1.33);
                    _bossPlayer.Seek(1.15, true);
                    _bossPlayer.Pause();
                }

                SetMeshes(part, false);
                part.HideIn = 0;
            }
        }

        WriteShot(shot);
        _hold = 14;
        _recent.Clear();
    }

    private void ShowStun(int index)
    {
        PartAnchor part = _parts[index];
        if (part.StunIcon != null)
        {
            part.StunIcon.Visible = true;
        }

        if (part.Halo != null)
        {
            part.Halo.Visible = true;
        }

        LoopBoss("stunned");
        if (_bossPlayer != null)
        {
            _bossPlayer.Pause();
        }
    }

    private void Freeze(Actor actor, string clip, double at)
    {
        if (!Play(actor, clip, false) || actor.Player == null)
        {
            return;
        }

        actor.Player.Seek(at, true);
        actor.Player.Pause();
        actor.Busy = 8;
        actor.BusyClip = clip;
    }

    private static int StunnedPart(BattleSimulator sim)
    {
        int best = -1;
        int until = -1;
        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            if (sim.Tick < sim.Boss.Parts[i].StunExpires && sim.Boss.Parts[i].StunExpires > until)
            {
                until = sim.Boss.Parts[i].StunExpires;
                best = i;
            }
        }

        return best;
    }

    private static int FirstDown(BattleSimulator sim)
    {
        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            if (sim.Boss.Parts[i].Hp <= 0 || !sim.Boss.Parts[i].Active)
            {
                return i;
            }
        }

        return -1;
    }

    private bool Saw(string token)
    {
        for (int i = 0; i < _recent.Count; i++)
        {
            if (_recent[i].Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool SawBastion()
    {
        for (int i = 0; i < _recent.Count; i++)
        {
            string line = _recent[i];
            if (line.Contains("Keratin Bastion.", StringComparison.Ordinal) &&
                !line.Contains("expired", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteShot(string name)
    {
        GD.Print($"ARENA_SHOT {name}");
        try
        {
            File.WriteAllText("/tmp/arena-shot", name);
        }
        catch (IOException)
        {
            // Capture can still read stdout.
        }
    }

    private void Nudge(float scale)
    {
        // A few centimetres. Large kicks shove a body past the frame margin.
        _kick += new Vector3(0.028f, 0.01f, -0.032f) * scale;
        const float cap = 0.07f;
        if (_kick.LengthSquared() > cap * cap)
        {
            _kick = _kick.Normalized() * cap;
        }
    }

    /// <summary>
    /// Pulls the camera back along a fixed 3/4 view and widens the vertical FOV
    /// until Korrith (2.12 m, shield and maul), the other three heroes, and the
    /// 4.4 m Carapace all sit inside the viewport with margin. Taller aspects
    /// lose horizontal coverage, so the lens opens (and the camera steps back
    /// if that would go fisheye) instead of cropping a side.
    /// </summary>
    private void FrameCamera()
    {
        if (_camera == null)
        {
            return;
        }

        Vector2 view = GetViewport().GetVisibleRect().Size;
        float aspect = view.Y > 2f ? view.X / view.Y : 1.78f;
        Vector3 back = CameraBack.Normalized();
        float distance = CameraDistance;
        float fov = 42f;
        Vector3 eye = CameraLook + back * distance;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            eye = CameraLook + back * distance;
            fov = FitFov(eye, aspect);
            if (fov <= 58f)
            {
                break;
            }

            distance *= 1.1f;
        }

        _camera.Fov = fov;
        _camera.Position = eye + _kick;
        _camera.LookAt(CameraLook, Vector3.Up);
        if (!_fpsPrinted && _frames == 30)
        {
            GD.Print($"ARENA_FRAME aspect {aspect.ToString("0.00", CultureInfo.InvariantCulture)} fov {fov.ToString("0.0", CultureInfo.InvariantCulture)} distance {distance.ToString("0.0", CultureInfo.InvariantCulture)} view {view.X.ToString("0", CultureInfo.InvariantCulture)}x{view.Y.ToString("0", CultureInfo.InvariantCulture)}");
        }
    }

    private float FitFov(Vector3 eye, float aspect)
    {
        float lo = 16f;
        float hi = 70f;
        for (int i = 0; i < 18; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (FramingFits(eye, aspect, mid))
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return hi;
    }

    private static bool FramingFits(Vector3 eye, float aspect, float fovDegrees)
    {
        Vector3 back = (eye - CameraLook).Normalized();
        Vector3 right = Vector3.Up.Cross(back);
        if (right.LengthSquared() < 0.0001f)
        {
            return false;
        }

        right = right.Normalized();
        Vector3 up = back.Cross(right);
        float tanV = Mathf.Tan(Mathf.DegToRad(fovDegrees) * 0.5f);
        float tanH = tanV * aspect;
        if (tanV < 0.001f || tanH < 0.001f)
        {
            return false;
        }

        foreach (Vector3 point in FramingPoints())
        {
            Vector3 delta = point - eye;
            float lx = delta.Dot(right);
            float ly = delta.Dot(up);
            float lz = delta.Dot(back);
            if (lz > -0.2f)
            {
                return false;
            }

            float ndcX = (lx / -lz) / tanH;
            float ndcY = (ly / -lz) / tanV;
            if (Mathf.Abs(ndcX) > FrameMargin || Mathf.Abs(ndcY) > FrameMargin)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Vector3> FramingPoints()
    {
        foreach (Vector3 spot in Formation)
        {
            // Korrith's mesh is 2.33 m tall and about 2.3 m wide with the shield and maul.
            yield return spot + new Vector3(0f, -0.2f, 0f);
            yield return spot + new Vector3(0f, 2.55f, 0f);
            yield return spot + new Vector3(-1.35f, 1.5f, 0f);
            yield return spot + new Vector3(1.35f, 1.7f, 0f);
        }

        // The proof mesh faces the party: local +Z is world -Z, local +X is world -X.
        // Bounds are wider than the 4.4 m body height (the mesh reaches 5.14 m).
        // The posed legs reach closer to the camera than the bind-pose box, so the
        // near ground points keep that lip above the bottom of the view.
        foreach (float lx in new[] { -3.48f, 3.08f })
        {
            foreach (float ly in new[] { 0f, 5.2f })
            {
                foreach (float lz in new[] { -2.11f, 4.91f })
                {
                    yield return BossSpot + new Vector3(-lx, ly, -lz);
                }
            }
        }

        yield return BossSpot + new Vector3(0f, 0f, -6.4f);
        yield return BossSpot + new Vector3(-2.4f, 0.4f, -6.4f);
        yield return BossSpot + new Vector3(2.2f, 0.4f, -6.4f);
    }

    private Vector3 PartPoint(string partName)
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            if (_parts[i].Name == partName && _parts[i].Anchor != null)
            {
                return _parts[i].Anchor.GlobalPosition;
            }
        }

        return _bossRoot != null ? _bossRoot.GlobalPosition + new Vector3(0f, 2.4f, 0f) : BossSpot + new Vector3(0f, 2.4f, 0f);
    }

    private Vector3 HeroPoint(string name)
    {
        Actor? actor = FindActor(name);
        return actor == null ? Vector3.Zero : actor.Root.GlobalPosition + new Vector3(0f, 1.3f, 0f);
    }

    private void SpawnFloat(Vector3 at, string text, Color color)
    {
        var floater = new DamageFloater { Position = at };
        AddChild(floater);
        floater.Show(text, color);
    }

    private Actor? FindActor(string name)
    {
        for (int i = 0; i < _actors.Count; i++)
        {
            if (_actors[i].Name == name)
            {
                return _actors[i];
            }
        }

        return null;
    }

    private string MatchHero(string text)
    {
        if (_bridge == null)
        {
            return "";
        }

        string best = "";
        foreach (HeroState hero in _bridge.Simulation.Heroes)
        {
            if (text.StartsWith(hero.Name, StringComparison.Ordinal) && hero.Name.Length > best.Length)
            {
                best = hero.Name;
            }
        }

        return best;
    }

    private static string AbilityOf(string text, string hero)
    {
        string rest = text[hero.Length..].TrimStart();
        if (rest.StartsWith("starts ", StringComparison.Ordinal))
        {
            rest = rest["starts ".Length..];
        }

        int end = rest.IndexOf(" → ", StringComparison.Ordinal);
        if (end < 0)
        {
            end = rest.IndexOf('.');
        }

        if (end < 0)
        {
            end = rest.Length;
        }

        return rest[..end].Trim();
    }

    private static string AfterArrow(string text)
    {
        int arrow = text.IndexOf("→ ", StringComparison.Ordinal);
        if (arrow < 0)
        {
            return "";
        }

        string rest = text[(arrow + 2)..];
        int dot = rest.IndexOf('.');
        return (dot < 0 ? rest : rest[..dot]).Trim();
    }

    private static string StripTick(string line)
    {
        if (!line.StartsWith("t=", StringComparison.Ordinal))
        {
            return line;
        }

        int cut = line.IndexOf("  ", StringComparison.Ordinal);
        return cut < 0 ? line : line[(cut + 2)..];
    }

    private static int ParseDamage(string text)
    {
        int at = text.IndexOf("Dmg ", StringComparison.Ordinal);
        if (at < 0)
        {
            return 0;
        }

        int value = 0;
        for (int i = at + 4; i < text.Length && text[i] is >= '0' and <= '9'; i++)
        {
            value = value * 10 + (text[i] - '0');
        }

        return value;
    }

    private static void SetMeshes(PartAnchor part, bool visible)
    {
        for (int i = 0; i < part.Meshes.Count; i++)
        {
            if (part.Meshes[i] is Node3D node)
            {
                node.Visible = visible;
            }
            else if (part.Meshes[i] is CanvasItem item)
            {
                item.Visible = visible;
            }
        }
    }

    private bool Play(Actor actor, string clip, bool loop)
    {
        actor.BusyClip = clip;
        if (actor.Player == null)
        {
            return false;
        }

        string resolved = actor.Resolve(clip);
        if (resolved.Length == 0)
        {
            if (_missing.Add(clip))
            {
                GD.Print($"ARENA_MISSING_CLIP {clip}");
            }

            return false;
        }

        SetLoop(actor.Player, resolved, loop);
        actor.Player.Play(resolved);
        return true;
    }

    private void Loop(Actor actor, string clip)
    {
        if (actor.Player == null)
        {
            return;
        }

        string resolved = actor.Resolve(clip);
        if (resolved.Length == 0)
        {
            resolved = actor.Resolve("idle");
        }

        if (resolved.Length == 0)
        {
            return;
        }

        if (actor.Player.CurrentAnimation == resolved && actor.Player.IsPlaying())
        {
            return;
        }

        SetLoop(actor.Player, resolved, true);
        actor.Player.Play(resolved);
    }

    private void PlayBoss(string clip, bool loop, double busy)
    {
        if (_bossPlayer == null)
        {
            return;
        }

        string resolved = Resolve(_bossPlayer, clip);
        if (resolved.Length == 0)
        {
            if (_missing.Add("boss:" + clip))
            {
                GD.Print($"ARENA_MISSING_CLIP boss {clip}");
            }

            return;
        }

        _bossClip = resolved;
        _bossBusy = busy;
        SetLoop(_bossPlayer, resolved, loop);
        _bossPlayer.Play(resolved);
    }

    private void LoopBoss(string clip)
    {
        if (_bossPlayer == null || _bossDead)
        {
            return;
        }

        string resolved = Resolve(_bossPlayer, clip);
        if (resolved.Length == 0)
        {
            return;
        }

        if (_bossPlayer.CurrentAnimation == resolved && _bossPlayer.IsPlaying())
        {
            return;
        }

        _bossClip = resolved;
        SetLoop(_bossPlayer, resolved, true);
        _bossPlayer.Play(resolved);
    }

    private static void SetLoop(AnimationPlayer player, string clip, bool loop)
    {
        Animation? anim = player.GetAnimation(clip);
        if (anim == null)
        {
            return;
        }

        try
        {
            anim.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        }
        catch (InvalidOperationException)
        {
            // Imported animations can refuse a loop-mode write. Playback still runs.
        }
    }

    private static string Resolve(AnimationPlayer player, string clip)
    {
        if (player.HasAnimation(clip))
        {
            return clip;
        }

        foreach (StringName name in player.GetAnimationList())
        {
            string text = name.ToString();
            if (text.EndsWith('/' + clip, StringComparison.Ordinal) || text.EndsWith(clip, StringComparison.Ordinal))
            {
                return text;
            }
        }

        return "";
    }

    private static void LogClips(string who, AnimationPlayer? player)
    {
        if (player == null)
        {
            GD.PrintErr($"ARENA_IMPORT {who} has no AnimationPlayer");
            return;
        }

        var names = new List<string>();
        foreach (StringName name in player.GetAnimationList())
        {
            names.Add(name.ToString());
        }

        GD.Print($"ARENA_CLIPS {who} {string.Join(",", names)}");
    }

    private static Node3D? LoadModel(string resPath)
    {
        if (Godot.FileAccess.FileExists(resPath + ".import"))
        {
            var packed = GD.Load<PackedScene>(resPath);
            if (packed != null)
            {
                GD.Print($"ARENA_IMPORT packed {resPath}");
                return packed.Instantiate<Node3D>();
            }
        }

        string abs = ProjectSettings.GlobalizePath(resPath);
        var doc = new GltfDocument();
        var state = new GltfState();
        Error err = doc.AppendFromFile(abs, state);
        if (err != Error.Ok)
        {
            GD.PrintErr($"ARENA_IMPORT {resPath} {err}");
            return null;
        }

        GD.Print($"ARENA_IMPORT gltf {resPath}");
        return doc.GenerateScene(state) as Node3D;
    }

    private static AnimationPlayer? FindPlayer(Node root)
    {
        foreach (Node node in root.FindChildren("*", "AnimationPlayer", true, false))
        {
            if (node is AnimationPlayer player && player.GetAnimationList().Length > 0)
            {
                return player;
            }
        }

        return null;
    }

    private static Node3D Placeholder(Color color)
    {
        var root = new Node3D();
        var mat = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 0.35f,
            Roughness = 0.45f,
        };
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = 0.38f, Height = 1.15f },
            Position = new Vector3(0f, 1.15f, 0f),
            MaterialOverride = mat,
        });
        root.AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.28f, Height = 0.56f },
            Position = new Vector3(0f, 2.0f, 0f),
            MaterialOverride = mat,
        });
        return root;
    }

    private static void AddNameplate(Node3D root, string name, float height)
    {
        root.AddChild(new Label3D
        {
            Text = name.Split(' ')[0],
            FontSize = 40,
            PixelSize = 0.0035f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            OutlineSize = 10,
            Position = new Vector3(0f, height, 0f),
            Modulate = new Color("d5dde8"),
            NoDepthTest = true,
        });
    }

    private static Sprite3D Icon(Node parent, Texture2D? texture, Vector3 at)
    {
        var sprite = new Sprite3D
        {
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.006f,
            Position = at,
            Visible = texture != null,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            Shaded = false,
        };
        parent.AddChild(sprite);
        return sprite;
    }

    private static Color RoleColor(string name, int index)
    {
        if (name.StartsWith("Korrith", StringComparison.Ordinal))
        {
            return new Color("c4a574");
        }

        if (name.StartsWith("Seraphine", StringComparison.Ordinal))
        {
            return new Color("f5b7b1");
        }

        return index % 2 == 0 ? new Color("7dcea0") : new Color("85c1e9");
    }

    private sealed class Actor
    {
        public string Name = "";
        public bool RealModel;
        public Node3D Root = null!;
        public Node3D? Body;
        public float BodyRestY = 1.15f;
        public AnimationPlayer? Player;
        public Skeleton3D? Skeleton;
        public int BastionBone = -1;
        public int HandBone = -1;
        public Node? Trail;
        public double TrailFor;
        public MeshInstance3D? Streak;
        public MeshInstance3D? Dome;
        public Sprite3D? ShieldIcon;
        public Sprite3D? RegenIcon;
        public Sprite3D? StunIcon;
        public double Busy;
        public string BusyClip = "";
        public bool Dead;
        public double Flinch;
        public bool Bastion;
        public double BastionAge;
        public Vector3 Home;
        private readonly Dictionary<string, string> _clips = new();

        public string Resolve(string clip)
        {
            if (_clips.TryGetValue(clip, out string? cached))
            {
                return cached;
            }

            if (Player == null)
            {
                return "";
            }

            string resolved = ArenaView.Resolve(Player, clip);
            _clips[clip] = resolved;
            return resolved;
        }
    }

    private sealed class PartAnchor
    {
        public string Name = "";
        public string Clip = "";
        public Node3D Anchor = null!;
        public List<Node> Meshes = new();
        public Sprite3D? StunIcon;
        public Sprite3D? ChainIcon;
        public MeshInstance3D? Halo;
        public bool PlayedBreak;
        public bool Deferred;
        public double HideIn;
    }
}

public partial class FadePlate : Node
{
    public float Life = 0.4f;
    private float _age;
    private StandardMaterial3D? _mat;
    private MeshInstance3D? _mesh;

    public void Bind(MeshInstance3D mesh, StandardMaterial3D mat)
    {
        _mesh = mesh;
        _mat = mat;
    }

    public override void _Process(double delta)
    {
        if (ArenaVfx.HoldFrames && _age > 0.12f)
        {
            return;
        }

        _age += (float)delta;
        if (_mat != null)
        {
            Color color = _mat.AlbedoColor;
            color.A = Mathf.Clamp(1f - _age / Life, 0f, 1f);
            _mat.AlbedoColor = color;
        }

        if (!ArenaVfx.HoldFrames && _age >= Life && _mesh != null)
        {
            _mesh.QueueFree();
        }
    }
}
