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

    public CreatureRenderer(Ecosystem ecosystem)
    {
        _ecosystem = ecosystem;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    public void SetPlantTexture(Texture2D t) => _plantTexture = t;
    public void SetHerbivoreTexture(Texture2D t) => _herbivoreTexture = t;
    public void SetCarnivoreTexture(Texture2D t) => _carnivoreTexture = t;
    public void SetOmnivoreTexture(Texture2D t) => _omnivoreTexture = t;
    public void RegisterSpeciesTexture(string species, Texture2D t) => _speciesTextures[species] = t;

    public void Draw(SpriteBatch spriteBatch, Camera camera)
    {
        Rectangle visible = camera.VisibleArea;
        var creatures = _ecosystem.Creatures;

        for (int i = 0; i < creatures.Count; i++)
        {
            var c = creatures[i];
            if (!c.IsAlive) continue;

            float px = c.Position.X;
            float py = c.Position.Y;

            if (px < visible.X - 20 || px > visible.X + visible.Width + 20 ||
                py < visible.Y - 20 || py > visible.Y + visible.Height + 20)
                continue;

            float size = 10f * c.Genome.Size;
            int s = (int)size;
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
                spriteBatch.Draw(tex, dest, null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }
            else
            {
                Color bodyColor = c.Genome.Color;
                spriteBatch.Draw(_pixelTexture, dest, bodyColor * 0.9f);

                Vector2 dir = c.Facing;
                if (dir.LengthSquared() > 0.01f)
                {
                    dir.Normalize();
                    float eyeOffset = size * 0.3f;
                    int eyeSize = Math.Max(2, s / 4);
                    Rectangle eye1 = new(
                        (int)(px + dir.X * eyeOffset - dir.Y * eyeOffset / 2 - eyeSize / 2),
                        (int)(py + dir.Y * eyeOffset + dir.X * eyeOffset / 2 - eyeSize / 2),
                        eyeSize, eyeSize
                    );
                    Rectangle eye2 = new(
                        (int)(px + dir.X * eyeOffset + dir.Y * eyeOffset / 2 - eyeSize / 2),
                        (int)(py + dir.Y * eyeOffset - dir.X * eyeOffset / 2 - eyeSize / 2),
                        eyeSize, eyeSize
                    );
                    spriteBatch.Draw(_pixelTexture, eye1, Color.White);
                    spriteBatch.Draw(_pixelTexture, eye2, Color.White);
                }
            }
        }
    }
}
