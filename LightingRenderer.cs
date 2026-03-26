using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public struct LightSource
{
    public Vector2 Position;
    public float Radius;
    public Color Tint;
    public float Lifetime;
    public float MaxLifetime;

    public LightSource(Vector2 pos, float radius, Color tint, float lifetime = -1f)
    {
        Position = pos;
        Radius = radius;
        Tint = tint;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
    }
}

public class LightingRenderer
{
    private RenderTarget2D _lightMap;
    private Texture2D _circleHard; // solid circle
    private Texture2D _glow; // soft edge circle

    private readonly GraphicsDevice _gd;

    private readonly List<LightSource> _lights = new();

    public float PlayerRadius { get; set; } = 60f;
    public float DimMultiplier { get; set; } = 2.5f; // outer fade zone
    public float DimBrightness { get; set; } = 0.35f;

    // Blending to prevent stacking brightness with multiple light sources, keeps the highest value only
    private static readonly BlendState MaxBlend = new BlendState
    {
        ColorBlendFunction = BlendFunction.Max,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Max,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };

    public LightingRenderer(GraphicsDevice gd, int virtualW, int virtualH)
    {
        _gd = gd;

        _lightMap = new RenderTarget2D(gd, virtualW, virtualH);
        _circleHard = BuildCircle(gd, 512);
        _glow = BuildGlow(gd, 512);
    }

    private static Texture2D BuildCircle(GraphicsDevice gd, int size)
    {
        // Hard edge circle
        float cx = size / 2f;
        float cy = size / 2f;
        float r = size / 2f - 1f;

        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            px[y * size + x] = dist <= r ? Color.White : Color.Transparent;
        }

        var tex = new Texture2D(gd, size, size);
        tex.SetData(px);
        return tex;
    }

    private static Texture2D BuildGlow(GraphicsDevice gd, int size)
    {
        // Soft edge circle for glow
        float cx = size / 2f;
        float cy = size / 2f;
        float r = size / 2f;

        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float t = MathHelper.Clamp(1f - (dist / r), 0f, 1f);

            t *= t;

            px[y * size + x] = new Color((float)255, 255, 255, (byte)(t * 255f));
        }

        var tex = new Texture2D(gd, size, size);
        tex.SetData(px);
        return tex;
    }

    public void AddLight(LightSource light) => _lights.Add(light);

    public void AddFlash(Vector2 pos, float radius, Color tint, float duration)
        => _lights.Add(new LightSource(pos, radius, tint, duration));

    public Texture2D BuildLightMap(SpriteBatch sb, Vector2 playerPos, float dt)
    {
        // update lights
        for (int i = _lights.Count - 1; i >= 0; i--)
        {
            var l = _lights[i];

            if (l.Lifetime < 0f)
                continue;

            l.Lifetime -= dt;

            if (l.Lifetime <= 0f)
            {
                _lights.RemoveAt(i);
                continue;
            }

            _lights[i] = l;
        }

        _gd.SetRenderTarget(_lightMap);
        _gd.Clear(Color.Black); // fully dark base screen
        
        sb.Begin(blendState: MaxBlend, samplerState: SamplerState.PointClamp);

        DrawCircle(
            sb,
            playerPos,
            PlayerRadius * DimMultiplier,
            new Color(DimBrightness, DimBrightness, DimBrightness, 1f)
        );
        
        // Full brightness zone around player
        DrawCircle(sb, playerPos, PlayerRadius, Color.White);

        // only white lights affect visibility
        foreach (var l in _lights)
        {
            if (l.Tint != Color.White)
                continue;

            // might use fade for something
            float a = l.Lifetime < 0f
                ? 1f
                : MathHelper.Clamp(l.Lifetime / l.MaxLifetime, 0f, 1f);

            DrawCircle(sb, l.Position, l.Radius, Color.White);
        }

        sb.End();

        // Coloured glow
        sb.Begin(blendState: BlendState.Additive, samplerState: SamplerState.PointClamp);

        foreach (var l in _lights)
        {
            if (l.Tint == Color.White)
                continue;

            float a = l.Lifetime < 0f
                ? 1f
                : MathHelper.Clamp(l.Lifetime / l.MaxLifetime, 0f, 1f);

            DrawGlow(sb, l.Position, l.Radius, l.Tint, a);
        }

        sb.End();

        return _lightMap;
    }

    private void DrawCircle(SpriteBatch sb, Vector2 pos, float radius, Color color)
    {
        var origin = new Vector2(_circleHard.Width / 2f, _circleHard.Height / 2f);
        float scale = (radius * 2f) / _circleHard.Width;

        sb.Draw(_circleHard, pos, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private void DrawGlow(SpriteBatch sb, Vector2 pos, float radius, Color tint, float alpha)
    {
        var origin = new Vector2(_glow.Width / 2f, _glow.Height / 2f);
        float scale = (radius * 2f) / _glow.Width;

        var color = new Color(tint.R, tint.G, tint.B, (int)(alpha * 255f));
        sb.Draw(_glow, pos, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public void Dispose()
    {
        _lightMap?.Dispose();
        _circleHard?.Dispose();
        _glow?.Dispose();
    }
}