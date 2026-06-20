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

    public const int PanelWidth = 200;
    public const int ToggleButtonSize = 44;
    private const int HeaderHeight = 32;
    private const int SectionSpacing = 6;
    private const int ButtonHeight = 24;
    private const int ButtonSpacing = 2;
    private const int Margin = 10;

    private readonly List<UiButton> _categoryButtons = new();
    private readonly List<UiButton> _speciesButtons = new();
    private Rectangle _panelBounds;
    private Rectangle _toggleBounds;
    private int _viewportHeight;
    private Texture2D? _iconTexture;

    public SpawnPanel() { RebuildCategoryButtons(); }

    public void SetIconTexture(Texture2D? icon) => _iconTexture = icon;

    public void Open() => IsOpen = true;
    public void Close()
    {
        IsOpen = false;
        SelectedSpecies = null;
        SelectedCategory = null;
    }
    public void Toggle() => IsOpen = !IsOpen;

    public void SetViewportHeight(int h) => _viewportHeight = h;

    public bool HandleClick(MouseState mouse, MouseState previousMouse)
    {
        if (!WasClicked(mouse, previousMouse)) return false;
        var pos = mouse.Position;

        if (_toggleBounds.Contains(pos))
        {
            Toggle();
            return true;
        }

        if (!IsOpen) return false;

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
        UpdateLayout();
        return HandleClick(mouse, previousMouse);
    }

    private void UpdateLayout()
    {
        int toggleY = _viewportHeight - Margin - ToggleButtonSize - 52;
        _toggleBounds = new Rectangle(Margin, toggleY, ToggleButtonSize, ToggleButtonSize);
        if (IsOpen)
        {
            int panelY = toggleY - Margin - ComputePanelHeight();
            _panelBounds = new Rectangle(Margin, panelY, PanelWidth, ComputePanelHeight());
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        UpdateLayout();
        DrawToggleButton(sb, pixel, font, mouse);

        if (!IsOpen) return;

        UiPrimitives.Fill(sb, pixel, _panelBounds, new Color(11, 23, 18, 235));
        UiPrimitives.Border(sb, pixel, _panelBounds, 2, new Color(107, 81, 55));

        sb.DrawString(font, I18n.T("spawn.title"),
            new Vector2(_panelBounds.X + 10, _panelBounds.Y + 6), UiTheme.MossSignal);

        foreach (var btn in _categoryButtons)
        {
            bool isSelected = btn.Text == SelectedCategory;
            btn.Draw(sb, pixel, font, mouse, isSelected);
            if (isSelected)
            {
                foreach (var sBtn in _speciesButtons)
                {
                    bool isSel = sBtn.Text == SelectedSpecies;
                    sBtn.Draw(sb, pixel, font, mouse, isSel);
                }
            }
        }

        if (SelectedSpecies != null)
        {
            string hint = I18n.T("spawn.hint");
            sb.DrawString(font, hint,
                new Vector2(_panelBounds.X + 10, _panelBounds.Bottom - 22),
                UiTheme.WarmParchment);
        }
    }

    private void DrawToggleButton(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        bool isHover = _toggleBounds.Contains(mouse.Position);
        Color bg = IsOpen ? new Color(78, 156, 181, 230) :
            (isHover ? new Color(11, 23, 18, 240) : new Color(11, 23, 18, 200));
        UiPrimitives.Fill(sb, pixel, _toggleBounds, bg);
        UiPrimitives.Border(sb, pixel, _toggleBounds, 2, IsOpen ? UiTheme.MossSignal : new Color(107, 81, 55));

        if (_iconTexture != null)
        {
            int pad = 6;
            var iconRect = new Rectangle(_toggleBounds.X + pad, _toggleBounds.Y + pad,
                _toggleBounds.Width - pad * 2, _toggleBounds.Height - pad * 2);
            sb.Draw(_iconTexture, iconRect, Color.White);
        }
        else
        {
            sb.DrawString(font, "+", new Vector2(_toggleBounds.X + 16, _toggleBounds.Y + 12),
                IsOpen ? Color.White : UiTheme.MossSignal);
        }
    }

    private void RebuildCategoryButtons()
    {
        _categoryButtons.Clear();
        int y = 0 + HeaderHeight;
        foreach (var category in SpeciesByCategory.Keys)
        {
            _categoryButtons.Add(new UiButton(I18n.T($"spawn.{category.ToLowerInvariant()}")));
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
        LayoutCategoryButtons();
    }

    private void LayoutCategoryButtons()
    {
        int y = HeaderHeight;
        foreach (var btn in _categoryButtons)
        {
            btn.Bounds = new Rectangle(Margin + 10, y, PanelWidth - 20, ButtonHeight);
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
    }

    private void RebuildSpeciesButtons()
    {
        _speciesButtons.Clear();
        if (SelectedCategory == null) return;
        if (!SpeciesByCategory.TryGetValue(SelectedCategory, out var species)) return;

        int y = HeaderHeight;
        foreach (var c in _categoryButtons)
        {
            if (c.Text == SelectedCategory) break;
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }

        foreach (var s in species)
        {
            _speciesButtons.Add(new UiButton(I18n.Species(s))
            {
                Bounds = new Rectangle(Margin + 20, y, PanelWidth - 30, ButtonHeight)
            });
            y += ButtonHeight + ButtonSpacing;
        }
    }

    private int ComputePanelHeight()
    {
        int h = HeaderHeight + 10;
        foreach (var _ in _categoryButtons)
            h += ButtonHeight + ButtonSpacing + SectionSpacing;
        if (SelectedCategory != null && _speciesButtons.Count > 0)
            h += _speciesButtons.Count * (ButtonHeight + ButtonSpacing);
        h += 26;
        return h;
    }

    private static bool WasClicked(MouseState current, MouseState previous) =>
        current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;
}
