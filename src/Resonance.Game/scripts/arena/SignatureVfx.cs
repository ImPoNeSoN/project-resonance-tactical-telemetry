using Godot;

namespace Resonance.Game;

public enum SignatureKind
{
    CureCascade,
    PhaseSanctuary,
    HexLance,
    PyreLattice,
    CrescentSever,
    RavelExecution,
}

/// <summary>
/// One distinct read per signature clip. Cyan #00e5ff and magenta #e13cff on dark metal,
/// built from quads, rings, and lights the same way Shield Bash is.
/// </summary>
public partial class SignatureBurst : Node3D
{
    private static readonly Color Cyan = new(0f, 0.898f, 1f);
    private static readonly Color Magenta = new(0.882f, 0.235f, 1f);

    private SignatureKind _kind;
    private Vector3 _from;
    private bool _hasFrom;
    private float _age;
    private float _life = 1.35f;
    private bool _held;
    private OmniLight3D? _light;
    private readonly List<Node3D> _spun = new();
    private readonly List<ShaderMaterial> _mats = new();

    public static bool TryMatch(string ability, out SignatureKind kind)
    {
        if (ability.Contains("Cure Cascade", StringComparison.Ordinal))
        {
            kind = SignatureKind.CureCascade;
            return true;
        }

        if (ability.Contains("Phase Sanctuary", StringComparison.Ordinal))
        {
            kind = SignatureKind.PhaseSanctuary;
            return true;
        }

        if (ability.Contains("Hex Lance", StringComparison.Ordinal))
        {
            kind = SignatureKind.HexLance;
            return true;
        }

        if (ability.Contains("Pyre Lattice", StringComparison.Ordinal))
        {
            kind = SignatureKind.PyreLattice;
            return true;
        }

        if (ability.Contains("Crescent Sever", StringComparison.Ordinal))
        {
            kind = SignatureKind.CrescentSever;
            return true;
        }

        if (ability.Contains("Ravel Execution", StringComparison.Ordinal))
        {
            kind = SignatureKind.RavelExecution;
            return true;
        }

        kind = SignatureKind.CureCascade;
        return false;
    }

    public static bool IsChant(SignatureKind kind) =>
        kind is SignatureKind.CureCascade or SignatureKind.PhaseSanctuary or SignatureKind.HexLance or SignatureKind.PyreLattice;

    public static void Spawn(Node parent, SignatureKind kind, Vector3 at, Vector3 from, bool hasFrom)
    {
        var burst = new SignatureBurst
        {
            _kind = kind,
            _from = from,
            _hasFrom = hasFrom,
        };
        burst.Position = at;
        parent.AddChild(burst);
        burst.Compose();
    }

    private void Compose()
    {
        switch (_kind)
        {
            case SignatureKind.CureCascade:
                Column(Cyan, Magenta, 1.6f, 7);
                Ring(Cyan, 0.7f, new Vector3(-90f, 0f, 0f));
                _life = 1.4f;
                Light(Cyan, 4f, 4.5f);
                break;
            case SignatureKind.PhaseSanctuary:
                Dome(Magenta, 3.4f, 1.5f);
                Ring(Cyan, 3.2f, new Vector3(-90f, 0f, 0f));
                _life = 1.8f;
                Light(Magenta, 6f, 8f);
                break;
            case SignatureKind.HexLance:
                Beam(Cyan, Magenta);
                Flash(Cyan, 0.28f);
                _life = 0.85f;
                Light(Cyan, 7f, 5f);
                break;
            case SignatureKind.PyreLattice:
                Lattice(Magenta, Cyan);
                Ring(Cyan, 1.1f, new Vector3(-90f, 0f, 0f));
                _life = 1.5f;
                Light(Magenta, 6f, 6f);
                break;
            case SignatureKind.CrescentSever:
                Crescent(Cyan, 1.35f, 22f);
                Crescent(Magenta, 1.05f, -28f);
                Slash(Cyan, 48f);
                Slash(Magenta, -36f);
                _life = 0.9f;
                Light(Cyan, 7f, 6f);
                break;
            default:
                Spike(Magenta, Cyan);
                Ring(Cyan, 0.85f, new Vector3(-90f, 0f, 0f));
                _life = 1.05f;
                Light(Magenta, 8f, 5f);
                break;
        }
    }

    private void Column(Color a, Color b, float height, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float y = 0.15f + (i * height / count);
            var plate = Quad(i % 2 == 0 ? a : b, new Vector2(0.28f, 0.28f), billboard: true);
            plate.Position = new Vector3(Mathf.Sin(i * 1.3f) * 0.22f, y, Mathf.Cos(i * 1.1f) * 0.22f);
            plate.RotationDegrees = new Vector3(0f, i * 28f, 0f);
            AddChild(plate);
            _spun.Add(plate);
        }
    }

    private void Dome(Color tint, float radius, float height)
    {
        var mat = new ShaderMaterial { Shader = ArenaVfx.DomeShader() };
        mat.SetShaderParameter("tint", tint);
        mat.SetShaderParameter("pulse", 1.05f);
        _mats.Add(mat);
        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = height * 2f, RadialSegments = 24, Rings = 12 },
            MaterialOverride = mat,
            Position = new Vector3(0f, height * 0.45f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(mesh);
    }

    private void Beam(Color core, Color edge)
    {
        Vector3 hero = _hasFrom ? ToLocal(_from) : new Vector3(0f, 1.2f, -2f);
        Vector3 mid = hero * 0.5f;
        float length = Mathf.Max(0.4f, hero.Length());
        var outer = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.16f, 0.16f, 1f) },
            MaterialOverride = AddMat(edge),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var inner = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.05f, 0.05f, 1f) },
            MaterialOverride = AddMat(core),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(outer);
        AddChild(inner);
        Vector3 aim = hero.Normalized();
        Vector3 up = Mathf.Abs(aim.Dot(Vector3.Up)) > 0.92f ? Vector3.Right : Vector3.Up;
        foreach (MeshInstance3D beam in new[] { outer, inner })
        {
            beam.Position = mid;
            beam.Scale = new Vector3(1f, 1f, length);
            beam.LookAt(GlobalPosition, up);
        }
    }

    private void Lattice(Color face, Color edge)
    {
        var rig = new Node3D { Position = new Vector3(0f, 1.5f, 0f) };
        AddChild(rig);
        _spun.Add(rig);
        for (int i = 0; i < 3; i++)
        {
            var plate = Quad(i == 1 ? edge : face, new Vector2(1.35f, 1.35f), billboard: false);
            plate.RotationDegrees = new Vector3(90f, i * 60f, 0f);
            rig.AddChild(plate);
        }
    }

    private void Crescent(Color tint, float radius, float yaw)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = radius * 0.62f, OuterRadius = radius, Rings = 10, RingSegments = 22 },
            MaterialOverride = AddMat(tint),
            RotationDegrees = new Vector3(72f, yaw, 64f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(mesh);
        _spun.Add(mesh);
    }

    private void Slash(Color tint, float roll)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.14f, 2.1f, 0.14f) },
            MaterialOverride = AddMat(tint),
            RotationDegrees = new Vector3(18f, 12f, roll),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(mesh);
        _spun.Add(mesh);
    }

    private void Spike(Color body, Color tip)
    {
        var shaft = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.2f, Height = 2.4f, RadialSegments = 8 },
            MaterialOverride = AddMat(body),
            Position = new Vector3(0f, 1.3f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var core = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.06f, Height = 2.6f, RadialSegments = 6 },
            MaterialOverride = AddMat(tip),
            Position = new Vector3(0f, 1.35f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(shaft);
        AddChild(core);
    }

    private void Ring(Color tint, float size, Vector3 rotation)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = Vector2.One * size },
            RotationDegrees = rotation,
            MaterialOverride = new ShaderMaterial { Shader = ArenaVfx.RingShader() },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        ((ShaderMaterial)mesh.MaterialOverride).SetShaderParameter("tint", tint);
        _mats.Add((ShaderMaterial)mesh.MaterialOverride);
        AddChild(mesh);
        _spun.Add(mesh);
    }

    private void Flash(Color tint, float radius)
    {
        var mat = new ShaderMaterial { Shader = ArenaVfx.FlashShader() };
        mat.SetShaderParameter("tint", tint);
        _mats.Add(mat);
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private MeshInstance3D Quad(Color tint, Vector2 size, bool billboard)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = size },
            MaterialOverride = AddMat(tint, billboard),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        return mesh;
    }

    private static StandardMaterial3D AddMat(Color tint, bool billboard = false)
    {
        return new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = tint,
            EmissionEnabled = true,
            Emission = tint,
            EmissionEnergyMultiplier = 2.4f,
            BillboardMode = billboard ? BaseMaterial3D.BillboardModeEnum.Enabled : BaseMaterial3D.BillboardModeEnum.Disabled,
        };
    }

    private void Light(Color color, float energy, float range)
    {
        _light = new OmniLight3D
        {
            LightColor = color,
            LightEnergy = energy,
            OmniRange = range,
            ShadowEnabled = false,
        };
        AddChild(_light);
    }

    public override void _Process(double delta)
    {
        if (ArenaVfx.HoldFrames && _age > 0.22f)
        {
            _held = true;
        }

        if (!_held)
        {
            _age += (float)delta;
        }

        float u = Mathf.Clamp(_age / _life, 0f, 1f);
        float fade = _held ? 0.92f : Mathf.Clamp(1f - Mathf.Max(0f, u - 0.55f) / 0.45f, 0f, 1f);
        foreach (ShaderMaterial mat in _mats)
        {
            mat.SetShaderParameter("fade", fade);
            mat.SetShaderParameter("pulse", 0.8f + (0.35f * Mathf.Sin(_age * 6f)));
        }

        float spin = _held ? 0.6f : _age * 2.4f;
        for (int i = 0; i < _spun.Count; i++)
        {
            _spun[i].RotateY(0.015f + (i * 0.004f));
            if (_kind == SignatureKind.PyreLattice && _spun[i] is Node3D rig && rig.GetChildCount() > 0)
            {
                rig.RotationDegrees = new Vector3(0f, spin * 40f, 0f);
            }
        }

        if (_light != null)
        {
            _light.LightEnergy = (_kind == SignatureKind.RavelExecution ? 8f : 5f) * fade;
        }

        if (!_held && _age >= _life)
        {
            QueueFree();
        }
    }
}
