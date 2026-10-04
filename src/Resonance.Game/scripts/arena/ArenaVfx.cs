using Godot;

namespace Resonance.Game;

/// <summary>
/// Shield Bash, Keratin Bastion, and Attack reads built from shaders and particles.
/// No imported VFX meshes. Forward+ uses GPUParticles3D. The Compatibility renderer
/// cannot draw those, so the same burst is a CpuParticles3D there.
/// </summary>
public static class ArenaVfx
{
    public static bool HoldFrames { get; set; }

    public static bool GpuParticles { get; } =
        ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString() != "gl_compatibility";

    private static Shader? _ringShader;
    private static Shader? _domeShader;
    private static Shader? _flashShader;

    public static Shader RingShader()
    {
        _ringShader ??= new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled, blend_add, depth_draw_never, shadows_disabled;
                uniform vec4 tint : source_color = vec4(0.0, 0.898, 1.0, 1.0);
                uniform float fade : hint_range(0.0, 1.0) = 1.0;
                void fragment() {
                    vec2 p = UV * 2.0 - 1.0;
                    float r = length(p);
                    float ring = smoothstep(0.18, 0.02, abs(r - 0.72));
                    float core = smoothstep(0.28, 0.0, r) * 0.35;
                    ALBEDO = tint.rgb * (ring + core);
                    ALPHA = (ring + core) * fade;
                }
                """,
        };
        return _ringShader;
    }

    public static Shader FlashShader()
    {
        _flashShader ??= new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled, blend_add, depth_draw_never, shadows_disabled;
                uniform vec4 tint : source_color = vec4(0.7, 0.95, 1.0, 1.0);
                uniform float fade : hint_range(0.0, 1.0) = 1.0;
                void fragment() {
                    float fres = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 1.4);
                    ALBEDO = tint.rgb * (0.55 + fres);
                    ALPHA = (0.35 + fres) * fade;
                }
                """,
        };
        return _flashShader;
    }

    public static Shader DomeShader()
    {
        _domeShader ??= new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, shadows_disabled;
                uniform vec4 tint : source_color = vec4(0.0, 0.898, 1.0, 1.0);
                uniform float pulse : hint_range(0.0, 2.0) = 1.0;
                void fragment() {
                    vec3 n = normalize(NORMAL);
                    float fres = pow(1.0 - abs(dot(n, normalize(VIEW))), 1.5);
                    vec2 uv = UV * vec2(9.0, 5.0);
                    vec2 cell = fract(uv) - 0.5;
                    float edge = abs(max(abs(cell.x), abs(cell.y) * 0.55 + abs(cell.x) * 0.45) - 0.42);
                    float hex = smoothstep(0.08, 0.015, edge);
                    float alpha = clamp(fres * 0.55 + hex * 0.85, 0.0, 1.0) * 0.62 * clamp(pulse, 0.0, 1.4);
                    ALBEDO = tint.rgb * (0.35 + hex + fres);
                    EMISSION = ALBEDO * pulse;
                    ALPHA = alpha;
                }
                """,
        };
        return _domeShader;
    }

    public static MeshInstance3D Dome()
    {
        var mesh = new SphereMesh
        {
            Radius = 1.15f,
            Height = 2.3f,
            RadialSegments = 24,
            Rings = 16,
        };
        var mat = new ShaderMaterial { Shader = DomeShader() };
        mat.SetShaderParameter("tint", new Color(0f, 0.898f, 1f));
        mat.SetShaderParameter("pulse", 1f);
        var node = new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = mat,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Position = new Vector3(0f, 1.12f, 0f),
        };
        return node;
    }

    public static Node Sparks(Color color, int amount, float speed, bool oneShot)
    {
        var mesh = SparkQuad(color);
        if (GpuParticles)
        {
            var material = new ParticleProcessMaterial
            {
                Direction = Vector3.Up,
                Spread = 72f,
                InitialVelocityMin = speed * 0.35f,
                InitialVelocityMax = speed,
                Gravity = new Vector3(0f, -8f, 0f),
                ScaleMin = 0.35f,
                ScaleMax = 1f,
                Color = color,
            };
            return new GpuParticles3D
            {
                Amount = amount,
                Lifetime = 0.48f,
                OneShot = oneShot,
                Explosiveness = oneShot ? 0.94f : 0.05f,
                ProcessMaterial = material,
                DrawPass1 = mesh,
                LocalCoords = false,
                Emitting = false,
                VisibilityAabb = new Aabb(new Vector3(-3f, -3f, -3f), new Vector3(6f, 6f, 6f)),
            };
        }

        return new CpuParticles3D
        {
            Amount = amount,
            Lifetime = 0.48f,
            OneShot = oneShot,
            Explosiveness = oneShot ? 0.94f : 0.05f,
            Mesh = mesh,
            Direction = Vector3.Up,
            Spread = 72f,
            Gravity = new Vector3(0f, -8f, 0f),
            InitialVelocityMin = speed * 0.35f,
            InitialVelocityMax = speed,
            ScaleAmountMin = 0.35f,
            ScaleAmountMax = 1f,
            Color = color,
            LocalCoords = false,
            Emitting = false,
        };
    }

    public static void Restart(Node particles)
    {
        if (particles is GpuParticles3D gpu)
        {
            gpu.Restart();
            gpu.Emitting = true;
            return;
        }

        if (particles is CpuParticles3D cpu)
        {
            cpu.Restart();
            cpu.Emitting = true;
        }
    }

    public static void SetEmitting(Node particles, bool emitting)
    {
        if (particles is GpuParticles3D gpu)
        {
            gpu.Emitting = emitting;
        }
        else if (particles is CpuParticles3D cpu)
        {
            cpu.Emitting = emitting;
        }
    }

    private static QuadMesh SparkQuad(Color color)
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = color,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
        };
        return new QuadMesh
        {
            Size = new Vector2(0.16f, 0.16f),
            Material = material,
        };
    }
}

public enum ImpactKind
{
    Bash,
    Attack,
    Hit,
}

/// <summary>One impact: flash, optional shockwave ring, and a spark burst.</summary>
public partial class ImpactBurst : Node3D
{
    private float _age;
    private float _life = 0.7f;
    private ImpactKind _kind;
    private MeshInstance3D? _flash;
    private MeshInstance3D? _ring;
    private ShaderMaterial? _flashMat;
    private ShaderMaterial? _ringMat;
    private OmniLight3D? _light;
    private bool _held;

    public static ImpactBurst Spawn(Node parent, Vector3 at, ImpactKind kind)
    {
        var burst = new ImpactBurst { _kind = kind };
        burst.Position = at;
        parent.AddChild(burst);
        burst.Build();
        return burst;
    }

    private void Build()
    {
        Color tint = _kind switch
        {
            ImpactKind.Bash => new Color(0f, 0.898f, 1f),
            ImpactKind.Attack => new Color(1f, 0.82f, 0.55f),
            _ => new Color(1f, 0.72f, 0.45f),
        };
        float speed = _kind == ImpactKind.Bash ? 7.5f : 4.2f;
        int amount = _kind == ImpactKind.Bash ? 56 : 28;
        var sparks = ArenaVfx.Sparks(tint, amount, speed, oneShot: true);
        AddChild(sparks);
        ArenaVfx.Restart(sparks);

        _flash = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = _kind == ImpactKind.Bash ? 0.45f : 0.22f, Height = _kind == ImpactKind.Bash ? 0.9f : 0.44f },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _flashMat = new ShaderMaterial { Shader = ArenaVfx.FlashShader() };
        _flashMat.SetShaderParameter("tint", tint);
        _flash.MaterialOverride = _flashMat;
        AddChild(_flash);

        if (_kind == ImpactKind.Bash)
        {
            _ring = new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(1f, 1f) },
                RotationDegrees = new Vector3(-90f, 0f, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            _ringMat = new ShaderMaterial { Shader = ArenaVfx.RingShader() };
            _ringMat.SetShaderParameter("tint", tint);
            _ring.MaterialOverride = _ringMat;
            AddChild(_ring);
            _life = 0.95f;
        }

        _light = new OmniLight3D
        {
            LightColor = tint,
            LightEnergy = _kind == ImpactKind.Bash ? 8f : 3f,
            OmniRange = _kind == ImpactKind.Bash ? 6f : 3f,
            ShadowEnabled = false,
        };
        AddChild(_light);
    }

    public override void _Process(double delta)
    {
        if (ArenaVfx.HoldFrames && _age > 0.28f)
        {
            _held = true;
        }

        if (!_held)
        {
            _age += (float)delta;
        }

        float u = Mathf.Clamp(_age / _life, 0f, 1f);
        float fade = _held ? 0.85f : 1f - u;
        if (_flash != null)
        {
            float s = _kind == ImpactKind.Bash ? Mathf.Lerp(0.4f, 1.8f, Mathf.Min(u * 1.4f, 1f)) : Mathf.Lerp(0.3f, 1.1f, u);
            _flash.Scale = Vector3.One * s;
            _flashMat?.SetShaderParameter("fade", fade);
        }

        if (_ring != null)
        {
            float s = _held ? 2.4f : Mathf.Lerp(0.4f, 3.4f, u);
            _ring.Scale = new Vector3(s, s, 1f);
            _ringMat?.SetShaderParameter("fade", fade);
        }

        if (_light != null)
        {
            _light.LightEnergy = (_kind == ImpactKind.Bash ? 8f : 3f) * fade;
        }

        if (!_held && _age >= _life)
        {
            QueueFree();
        }
    }
}

public partial class DamageFloater : Node3D
{
    private Label3D _label = null!;
    private float _age;
    private Vector3 _start;

    public override void _Ready()
    {
        _label = new Label3D
        {
            FontSize = 56,
            PixelSize = 0.0045f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            OutlineSize = 10,
            OutlineModulate = new Color(0f, 0f, 0f, 0.85f),
            NoDepthTest = true,
            Modulate = Colors.White,
        };
        AddChild(_label);
    }

    public void Show(string text, Color color)
    {
        _start = Position;
        _age = 0f;
        _label.Text = text;
        _label.Modulate = color;
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        if (!(ArenaVfx.HoldFrames && _age > 0.35f))
        {
            _age += (float)delta;
        }

        Position = _start + Vector3.Up * (_age * 0.9f);
        var color = _label.Modulate;
        color.A = ArenaVfx.HoldFrames ? 1f : Mathf.Clamp(1f - _age / 0.9f, 0f, 1f);
        _label.Modulate = color;
        if (!ArenaVfx.HoldFrames && _age >= 0.9f)
        {
            QueueFree();
        }
    }
}
