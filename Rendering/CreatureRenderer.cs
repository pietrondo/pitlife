using System;
using System.Collections.Generic;
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
    private Texture2D? _herbivoreTexture;
    private Texture2D? _carnivoreTexture;
    private Texture2D? _omnivoreTexture;

    public CreatureRenderer(Ecosystem ecosystem) => _ecosystem = ecosystem;

    public void LoadContent(GraphicsDevice gd)
    {
        _pixelTexture = new Texture2D(gd, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    public void SetPlantTexture(Texture2D t) => _plantTexture = t;
    public void SetHerbivoreTexture(Texture2D t) => _herbivoreTexture = t;
    public void SetCarnivoreTexture(Texture2D t) => _carnivoreTexture = t;
    public void SetOmnivoreTexture(Texture2D t) => _omnivoreTexture = t;
    public void RegisterSpeciesTexture(string species, Texture2D t)
    {
        if (t != null)
            _speciesTextures[species] = t;
    }

    public void Draw(SpriteBatch sb, Camera camera)
    {
        if (_pixelTexture == null) return;

        Rectangle visible = camera.VisibleArea;
        var creatures = _ecosystem.Creatures;

        for (int i = 0; i < creatures.Count; i++)
        {
            try
            {
                var c = creatures[i];
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
                int s = Math.Max(6, (int)size);
                Rectangle dest = new((int)(px - s / 2), (int)(py - s / 2), s, s);

                Texture2D? tex = _speciesTextures.TryGetValue(c.Species, out var st) ? st : c.CreatureType switch
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
                    sb.Draw(tex, dest, null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                }
                else
                {
                    Color bodyColor = c.Genome.Color;
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
            }
            catch
            {
                // skip problematic creature
            }
        }
    }
}
