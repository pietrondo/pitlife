using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;

namespace PitLife.UI;

public sealed class SpawnPanel
{
    public bool IsOpen { get; private set; }
    public string? SelectedSpecies { get; private set; }
    public string? SelectedCategory { get; private set; }

    private static readonly Dictionary<string, string[]> SpeciesByCategory = new()
    {
        ["Plants"] = ["Plant", "Flowers", "Mushroom", "GrassTuft", "Cactus", "Moss", "BerryBush", "Pine", "Toadstool"],
        ["Herbivores"] = ["Rabbit", "Deer", "Sheep", "Horse", "Goat", "Fish", "Lizard", "Turtle", "Salmon"],
        ["Carnivores"] = ["Fox", "Lynx", "Tiger", "Lion", "Leopard", "Crocodile", "Snake", "Eagle", "Wolf", "Shark", "Piranha"],
        ["Omnivores"] = ["Boar", "Raccoon", "Frog", "Beetle", "Butterfly", "Bear", "Jellyfish"]
    };

    private const int Width = 200;
    private const int HeaderHeight = 32;
    private const int SectionSpacing = 6;
    private const int ButtonHeight = 24;
    private const int ButtonSpacing = 2;

    private readonly List<UiButton> _categoryButtons = new();
    private readonly List<UiButton> _speciesButtons = new();
    private Rectangle _bounds;
    private int _viewportHeight;

    public SpawnPanel() { RebuildCategoryButtons(); }

    public void Open() => IsOpen = true;
    public void Close() { IsOpen = false; SelectedSpecies = null; SelectedCategory = null; }
    public void Toggle() => IsOpen = !IsOpen;

    public void SetViewportHeight(int h) => _viewportHeight = h;

    public bool HandleClick(MouseState mouse, MouseState previousMouse)
    {
        if (!IsOpen) return false;
        if (!WasClicked(mouse, previousMouse)) return false;
        var pos = mouse.Position;
        foreach (var btn in _categoryButtons)
        {
            if (btn.Bounds.Contains(pos))
            {
                SelectedCategory = btn.Text == SelectedCategory ? null : btn.Text;
                SelectedSpecies = null;
                RebuildSpeciesButtons();
                return true;
            }
        }
        foreach (var btn in _speciesButtons)
        {
            if (btn.Bounds.Contains(pos))
            {
                SelectedSpecies = btn.Text;
                return true;
            }
        }
        return false;
    }

    public bool Update(MouseState mouse, MouseState previousMouse)
    {
        if (!IsOpen) return false;
        _bounds = new Rectangle(10, 10, Width, ComputeHeight());
        return HandleClick(mouse, previousMouse);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        if (!IsOpen) return;
        _bounds = new Rectangle(10, 10, Width, ComputeHeight());

        UiPrimitives.Fill(sb, pixel, _bounds, new Color(11, 23, 18, 235));
        UiPrimitives.Border(sb, pixel, _bounds, 2, new Color(107, 81, 55));

        sb.DrawString(font, I18n.T("spawn.title"), new Vector2(_bounds.X + 10, _bounds.Y + 6), UiTheme.MossSignal);

        int yOffset = HeaderHeight;
        foreach (var btn in _categoryButtons)
        {
            bool isSelected = btn.Text == SelectedCategory;
            btn.Draw(sb, pixel, font, mouse, isSelected);
            yOffset += ButtonHeight + ButtonSpacing;
            if (isSelected)
            {
                foreach (var sBtn in _speciesButtons)
                {
                    bool isSel = sBtn.Text == SelectedSpecies;
                    sBtn.Draw(sb, pixel, font, mouse, isSel);
                    yOffset += ButtonHeight + ButtonSpacing;
                }
                yOffset += SectionSpacing;
            }
        }
    }

    private void RebuildCategoryButtons()
    {
        _categoryButtons.Clear();
        int y = 10 + HeaderHeight;
        foreach (var category in SpeciesByCategory.Keys)
        {
            _categoryButtons.Add(new UiButton(I18n.T($"spawn.{category.ToLowerInvariant()}"))
            {
                Bounds = new Rectangle(20, y, Width - 20, ButtonHeight)
            });
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
    }

    private void RebuildSpeciesButtons()
    {
        _speciesButtons.Clear();
        if (SelectedCategory == null) return;
        if (!SpeciesByCategory.TryGetValue(SelectedCategory, out var species)) return;

        int y = 10 + HeaderHeight + ButtonHeight + ButtonSpacing + SectionSpacing;
        foreach (var c in _categoryButtons)
        {
            if (c.Text == SelectedCategory) break;
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
        foreach (var s in species)
        {
            _speciesButtons.Add(new UiButton(I18n.Species(s))
            {
                Bounds = new Rectangle(30, y, Width - 30, ButtonHeight)
            });
            y += ButtonHeight + ButtonSpacing;
        }
    }

    private int ComputeHeight()
    {
        int h = HeaderHeight + 10;
        foreach (var _ in _categoryButtons)
            h += ButtonHeight + ButtonSpacing + SectionSpacing;
        if (SelectedCategory != null && _speciesButtons.Count > 0)
            h += _speciesButtons.Count * (ButtonHeight + ButtonSpacing);
        return h;
    }

    private static bool WasClicked(MouseState current, MouseState previous) =>
        current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;
}
