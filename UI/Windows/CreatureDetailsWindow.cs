using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI.Windows;

public class CreatureDetailsWindow : UiWindow
{
    private readonly StringBuilder _creatureGenSb = new(128);
    private readonly StringBuilder _creatureLineageSb = new(128);

    public CreatureDetailsWindow(string title, string id) : base(title, id)
    {
    }

    public void DrawContent(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Creature? selectedCreature)
    {
        var content = ContentBounds;
        DrawCreature(spriteBatch, pixel, font, content, selectedCreature);
    }

    private void DrawCreature(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        Creature? creature)
    {
        if (creature == null || !creature.IsAlive)
        {
            DrawLine(spriteBatch, font, content.X, content.Y, I18n.T("creature.none"), UiTheme.MutedStone);
            DrawLine(spriteBatch, font, content.X, content.Y + 24, I18n.T("creature.selectHint"), UiTheme.MutedStone);
            return;
        }

        DrawLine(spriteBatch, font, content.X, content.Y,
            I18n.Format("creature.heading", I18n.Species(creature.Species), I18n.CreatureTypeName(creature.CreatureType)), UiTheme.MossSignal);
        DrawLine(spriteBatch, font, content.X, content.Y + 28, I18n.Format("creature.energy", creature.Energy, creature.MaxEnergy), UiTheme.WarmParchment);
        DrawProgress(spriteBatch, pixel, new Rectangle(content.X, content.Y + 48, content.Width, 14), creature.Energy / creature.MaxEnergy);
        DrawLine(spriteBatch, font, content.X, content.Y + 78, I18n.Format("creature.age", creature.Age), UiTheme.WarmParchment);
        var statusText = creature.Gender == Gender.None
            ? I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")
            : $"{I18n.T(creature.Gender == Gender.Male ? "ui.gender.male" : "ui.gender.female")}  |  {I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")}";
        DrawLine(spriteBatch, font, content.X, content.Y + 100, statusText, UiTheme.MutedStone);
        DrawLine(spriteBatch, font, content.X, content.Y + 122, I18n.Format("creature.speed", creature.Genome.Speed), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 144, I18n.Format("creature.size", creature.Genome.Size), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 166, I18n.Format("creature.metabolism", creature.Genome.Metabolism), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 188, I18n.Format("creature.vision", creature.Genome.VisionRange), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 210, I18n.Format("creature.mutationRate", creature.Genome.MutationRate), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 236, I18n.T("creature.adaptations"), UiTheme.MossSignal);

        var col1X = content.X;
        var col2X = content.X + content.Width / 2;

        DrawLine(spriteBatch, font, col1X, content.Y + 258, I18n.Format("creature.adaptDesert", creature.Genome.DesertAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 258, I18n.Format("creature.adaptCold", creature.Genome.ColdAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col1X, content.Y + 280, I18n.Format("creature.adaptForest", creature.Genome.ForestAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 280, I18n.Format("creature.adaptWater", creature.Genome.WaterAdaptation), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 312, I18n.Format("creature.genome",
            creature.Genome.Color.R, creature.Genome.Color.G, creature.Genome.Color.B), UiTheme.MutedStone);

        // Lineage tree
        DrawLine(spriteBatch, font, content.X, content.Y + 340, "Lineage", UiTheme.MossSignal);
        var lineage = creature.Lineage;
        var genDepth = 0;
        foreach (var kv in lineage.AncestorDepths)
            genDepth = Math.Max(genDepth, kv.Value);
        _creatureLineageSb.Clear();
        if (lineage.ParentAId > 0)
        {
            _creatureLineageSb.Append("Parents: [").Append(lineage.ParentAId).Append(", ").Append(lineage.ParentBId).Append("]  |  ID: ").Append(lineage.IndividualId);
        }
        else
        {
            _creatureLineageSb.Append("ID: ").Append(lineage.IndividualId).Append("  |  Founder");
        }
        DrawLine(spriteBatch, font, content.X, content.Y + 362, _creatureLineageSb, UiTheme.WarmParchment);

        _creatureGenSb.Clear();
        _creatureGenSb.Append("Ancestors: ").Append(lineage.AncestorDepths.Count).Append("  |  MaxGen: ").Append(genDepth)
            .Append("  |  Inbreeding: ").Append(Math.Round(creature.InbreedingCoefficient, 3))
            .Append("  |  Fitness: ").Append(Math.Round(creature.GeneticFitness, 2));
        DrawLine(spriteBatch, font, content.X, content.Y + 382, _creatureGenSb, new Color(200, 180, 140));
    }

    private static void DrawProgress(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, float value)
    {
        UiPrimitives.Fill(spriteBatch, pixel, bounds, UiTheme.DeepGrove);
        var width = (int)((bounds.Width - 4) * MathHelper.Clamp(value, 0f, 1f));
        if (width > 0)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), UiTheme.MossSignal);
        UiPrimitives.Border(spriteBatch, pixel, bounds, 2, UiTheme.BarkEdge);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, StringBuilder text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }
}
