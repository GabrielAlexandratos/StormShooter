using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StormShooter;

public enum WeatherType { None, Rain, HeavyRain, LightSnow }

public class WeatherSystem
{
    private struct Drop
    {
        public float X, Y, Phase;
        public float Vx, Vy;
        public float LenMult, Alpha;
        public float SplashY;
    }

    private struct Splash
    {
        public float X, Y;
        public float Life;
    }

    private const int VW = Settings.VirtualWidth;
    private const int VH = Settings.VirtualHeight;
    private const int MaxDrops = 520;
    private const int MaxSplashes = 180;
    private const float SplashDuration = 0.18f;

    private readonly Drop[] _drops = new Drop[MaxDrops];
    private readonly Splash[] _splashes = new Splash[MaxSplashes];
    private int _splashNext;
    private readonly Random _rng = new();
    private bool _initialized;
    private float _time;

    public WeatherType Type = WeatherType.None;

    public static WeatherType Random(Random rng) =>
        (WeatherType)rng.Next(Enum.GetValues<WeatherType>().Length);

    public Color AtmosphereTint => Type switch
    {
        WeatherType.Rain => Color.FromNonPremultiplied(15, 25, 45, 35),
        WeatherType.HeavyRain => Color.FromNonPremultiplied(10, 18, 38, 70),
        _ => Color.Transparent,
    };

    private static readonly (int Count, float VelX, float VelY, float Width, float Length, int Alpha, Color BaseColor)[] Configs =
    {
        (0, 0f, 0f, 0f, 0f, 0, Color.Transparent),
        (220, -160f, 1100f, 0.6f, 16f, 180, new Color(80, 110, 150)),
        (500, -320f, 2400f, 0.7f, 22f, 235, new Color(50, 80, 125)),
        (60, -8f, 28f, 2.0f, 2f, 90, new Color(255, 255, 255)),
    };

    private Drop SpawnDrop(int typeIndex, bool scatter, Vector2 cam)
    {
        var cfg = Configs[typeIndex];
        float spawnBand = VH * MathF.Abs(cfg.VelX / cfg.VelY);
        float speedMult = 0.75f + (float)_rng.NextDouble() * 0.8f;
        float lenMult = 0.5f + (float)_rng.NextDouble() * 1.2f;
        float alphaMult = 0.55f + (float)_rng.NextDouble() * 0.55f;
        float angleJitter = typeIndex == (int)WeatherType.HeavyRain ? 80f : 50f;

        float startX = cam.X + (float)_rng.NextDouble() * (VW + spawnBand);
        float startY, splashY;

        if (scatter)
        {
            startY = cam.Y + (float)_rng.NextDouble() * VH;
            float remaining = (cam.Y + VH) - startY;
            splashY = remaining > 5f
                ? startY + (float)_rng.NextDouble() * remaining
                : startY + 5f;
        }
        else
        {
            startY = cam.Y - (float)_rng.NextDouble() * VH * 0.15f;
            splashY = cam.Y + (float)_rng.NextDouble() * VH;
        }

        return new Drop
        {
            X = startX,
            Y = startY,
            Phase = (float)_rng.NextDouble() * MathF.Tau,
            Vx = cfg.VelX * speedMult + (float)(_rng.NextDouble() - 0.5) * angleJitter,
            Vy = cfg.VelY * speedMult,
            LenMult = lenMult,
            Alpha = MathHelper.Clamp(cfg.Alpha * alphaMult, 0f, 255f),
            SplashY = splashY,
        };
    }

    private void SpawnSplash(float x, float y)
    {
        _splashes[_splashNext] = new Splash { X = x, Y = y, Life = 1f };
        _splashNext = (_splashNext + 1) % MaxSplashes;
    }

    public void Update(float dt, Vector2 cameraPos)
    {
        if (Type == WeatherType.None) return;

        _time += dt;
        int typeIndex = (int)Type;
        var cfg = Configs[typeIndex];
        bool isRain = Type == WeatherType.Rain || Type == WeatherType.HeavyRain;

        if (!_initialized)
        {
            for (int i = 0; i < MaxDrops; i++)
                _drops[i] = SpawnDrop(typeIndex, scatter: true, cameraPos);
            _initialized = true;
        }

        for (int i = 0; i < cfg.Count; i++)
        {
            var d = _drops[i];

            if (Type == WeatherType.LightSnow)
            {
                d.Y += cfg.VelY * dt;
                d.X += cfg.VelX * dt + MathF.Sin(d.Phase + _time * 1.2f) * 5f * dt;
            }
            else
            {
                d.Y += d.Vy * dt;
                d.X += d.Vx * dt;
            }

            bool hitFloor = isRain && d.Y >= d.SplashY;
            bool offScreen = d.X < cameraPos.X - 30f || d.Y > cameraPos.Y + VH + 30f;

            if (hitFloor)
            {
                SpawnSplash(d.X, d.Y);
                _drops[i] = SpawnDrop(typeIndex, scatter: false, cameraPos);
            }
            else if (offScreen)
            {
                _drops[i] = SpawnDrop(typeIndex, scatter: false, cameraPos);
            }
            else
            {
                _drops[i] = d;
            }
        }

        float lifeDrain = dt / SplashDuration;
        for (int i = 0; i < MaxSplashes; i++)
            if (_splashes[i].Life > 0f)
                _splashes[i].Life = MathF.Max(0f, _splashes[i].Life - lifeDrain);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (Type == WeatherType.None) return;

        int typeIndex = (int)Type;
        var cfg = Configs[typeIndex];
        bool isSnow = Type == WeatherType.LightSnow;
        float baseW = cfg.Width;
        float baseL = cfg.Length;

        for (int i = 0; i < cfg.Count; i++)
        {
            var d = _drops[i];
            float angle = isSnow ? 0f : MathF.Atan2(d.Vy, d.Vx);
            float l = isSnow ? baseW : baseL * d.LenMult;
            sb.Draw(pixel, new Vector2(d.X, d.Y), null, new Color(cfg.BaseColor, (int)d.Alpha),
                angle, new Vector2(0.5f, 0.5f), new Vector2(l, baseW), SpriteEffects.None, 0f);
        }

        if (!isSnow)
        {
            float dot = 1.1f;
            for (int i = 0; i < MaxSplashes; i++)
            {
                var s = _splashes[i];
                if (s.Life <= 0f) continue;

                float t = 1f - s.Life;
                float spread = t * 5f;
                int alpha = (int)(s.Life * cfg.Alpha * 0.7f);
                var color = new Color(cfg.BaseColor, alpha);
                var origin = new Vector2(0.5f, 0.5f);

                sb.Draw(pixel, new Vector2(s.X - spread, s.Y), null, color, 0f, origin, dot, SpriteEffects.None, 0f);
                sb.Draw(pixel, new Vector2(s.X + spread, s.Y), null, color, 0f, origin, dot, SpriteEffects.None, 0f);
                sb.Draw(pixel, new Vector2(s.X, s.Y - spread), null, color, 0f, origin, dot, SpriteEffects.None, 0f);
                sb.Draw(pixel, new Vector2(s.X, s.Y + spread), null, color, 0f, origin, dot, SpriteEffects.None, 0f);
            }
        }
    }
}
