using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

public class CreatureRenderer
{
    private readonly Ecosystem _ecosystem;
    private Texture2D? _pixelTexture;
    private Texture2D? _plantTexture;
    private readonly Dictionary<string, Texture2D> _speciesTextures = new();
    private readonly Dictionary<string, (Texture2D Male, Texture2D Female)> _genderedSpeciesTextures = new();
    private readonly HashSet<Creature> _reportedRenderFailures = new();
    private Texture2D? _herbivoreTexture;
    private Texture2D? _carnivoreTexture;
    private Texture2D? _omnivoreTexture;

    public CreatureRenderer(Ecosystem ecosystem) => _ecosystem = ecosystem;

    public void LoadContent(GraphicsDevice gd)
    {
        _pixelTexture = new Texture2D(gd, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    public void SetPlantTexture(Texture2D? t) => _plantTexture = t;
    public void SetHerbivoreTexture(Texture2D? t) => _herbivoreTexture = t;
    public void SetCarnivoreTexture(Texture2D? t) => _carnivoreTexture = t;
    public void SetOmnivoreTexture(Texture2D? t) => _omnivoreTexture = t;
    public void RegisterSpeciesTexture(string species, Texture2D? t)
    {
        if (t != null)
            _speciesTextures[species] = t;
    }

    public void RegisterGenderedSpeciesTexture(string species, Texture2D? male, Texture2D? female)
    {
        if (male != null && female != null)
            _genderedSpeciesTextures[species] = (male, female);
    }

    public void LoadFromRegistry(GraphicsDevice gd, IEnumerable<SpeciesAsset> assets)
    {
        foreach (var asset in assets)
        {
            var tex = LoadTexture(gd, asset.Path);
            switch (asset.Species)
            {
                case AssetRegistry.FallbackPlant: _plantTexture = tex; break;
                case AssetRegistry.FallbackHerbivore: _herbivoreTexture = tex; break;
                case AssetRegistry.FallbackCarnivore: _carnivoreTexture = tex; break;
                case AssetRegistry.FallbackOmnivore: _omnivoreTexture = tex; break;
                default: RegisterSpeciesTexture(asset.Species, tex); break;
            }
        }
    }

    public void LoadGenderedFromRegistry(GraphicsDevice gd, IEnumerable<GenderedSpeciesAsset> assets)
    {
        foreach (var a in assets)
        {
            var male = LoadTexture(gd, a.MalePath);
            var female = LoadTexture(gd, a.FemalePath);
            RegisterGenderedSpeciesTexture(a.Species, male, female);
        }
    }

    private static Texture2D? LoadTexture(GraphicsDevice gd, string path)
    {
        try { if (File.Exists(path)) return Texture2D.FromFile(gd, path); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load texture '{path}': {ex.Message}");
        }
        return null;
    }

    public void Draw(SpriteBatch sb, Camera camera, Color? dayNightOverlay = null)
    {
        if (_pixelTexture == null) return;

        Rectangle visible = camera.VisibleArea;
        var creatures = _ecosystem.Creatures;

        Color ApplyOverlay(Color original)
        {
            if (dayNightOverlay.HasValue && dayNightOverlay.Value.A > 0)
            {
                float alpha = dayNightOverlay.Value.A / 255f;
                return Color.Lerp(original, new Color(dayNightOverlay.Value.R, dayNightOverlay.Value.G, dayNightOverlay.Value.B, (byte)255), alpha);
            }
            return original;
        }

        for (int i = 0; i < creatures.Count; i++)
        {
            Creature? c = null;
            try
            {
                c = creatures[i];
                if (c == null || !c.IsAlive) continue;

                float px = c.Position.X;
                float py = c.Position.Y;

                if (float.IsNaN(px) || float.IsNaN(py)) continue;

                if (px < visible.X - 64 || px > visible.X + visible.Width + 64 ||
                    py < visible.Y - 64 || py > visible.Y + visible.Height + 64)
                    continue;

                float size = c.CreatureType == CreatureType.Plant
                    ? 18f * c.Genome.Size
                    : 28f * c.Genome.Size;
                if (c.IsBaby) size *= 0.6f;
                int s = Math.Max(6, (int)size);
                Rectangle dest = new((int)(px - s / 2), (int)(py - s / 2), s, s);

                Texture2D? tex;
                if (_genderedSpeciesTextures.TryGetValue(c.Species, out var gendered))
                    tex = c.Gender == Gender.Male ? gendered.Male : gendered.Female;
                else if (_speciesTextures.TryGetValue(c.Species, out var st))
                    tex = st;
                else
                    tex = c.CreatureType switch
                    {
                        CreatureType.Plant => _plantTexture,
                        CreatureType.Herbivore => _herbivoreTexture,
                        CreatureType.Carnivore => _carnivoreTexture,
                        CreatureType.Omnivore => _omnivoreTexture,
                        _ => null
                    };

                if (tex != null)
                {
                    Color tint = c.CreatureType == CreatureType.Plant
                        ? new Color(c.Genome.Color.R, (byte)Math.Min(255, c.Genome.Color.G * 1.3f), c.Genome.Color.B)
                        : c.Genome.Color;
                    tint = ApplyOverlay(tint);
                    sb.Draw(tex, dest, null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                }
                else
                {
                    Color bodyColor = ApplyOverlay(c.Genome.Color);
                    sb.Draw(_pixelTexture, dest, bodyColor * 0.9f);

                    Vector2 dir = c.Facing;
                    if (dir.LengthSquared() > 0.01f)
                    {
                        dir.Normalize();
                        float eyeOffset = size * 0.3f;
                        int eyeSize = Math.Max(2, s / 4);
                        sb.Draw(_pixelTexture, new Rectangle(
                            (int)(px + dir.X * eyeOffset - dir.Y * eyeOffset / 2 - eyeSize / 2),
                            (int)(py + dir.Y * eyeOffset + dir.X * eyeOffset / 2 - eyeSize / 2),
                            eyeSize, eyeSize), Color.White);
                        sb.Draw(_pixelTexture, new Rectangle(
                            (int)(px + dir.X * eyeOffset + dir.Y * eyeOffset / 2 - eyeSize / 2),
                            (int)(py + dir.Y * eyeOffset - dir.X * eyeOffset / 2 - eyeSize / 2),
                            eyeSize, eyeSize), Color.White);
                    }
                }

                // Gender icon
                if (c.Gender != Gender.None && _pixelTexture != null)
                {
                    Color genderColor = GetGenderIndicatorColor(c.Gender)!.Value;
                    genderColor = ApplyOverlay(genderColor);
                    int dot = Math.Max(2, s / 6);
                    int yOff = s / 2 + 2;
                    sb.Draw(_pixelTexture, new Rectangle((int)px - dot / 2, (int)py + yOff, dot, dot), genderColor);
                }
            }
            catch (Exception ex)
            {
                if (c != null && _reportedRenderFailures.Add(c))
                    Console.Error.WriteLine($"Creature render failed for {c.Species}: {ex.Message}");
            }
        }
    }

    internal static Color? GetGenderIndicatorColor(Gender gender) => gender switch
    {
        Gender.Male => Color.Red,
        Gender.Female => new Color(80, 120, 255),
        _ => null
    };
}
