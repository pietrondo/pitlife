using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.UI;

public sealed class SpawnPanel
{
    public bool IsOpen { get; private set; }
    public string? SelectedSpeciesKey { get; private set; }
    public string? SelectedCategory { get; private set; }

    private static readonly string[] CategoryOrder =
        ["Plants", "AquaticPlants", "LandHerbivores", "LandCarnivores", "LandOmnivores", "Birds", "Fish", "MarineMammals"];
    private Dictionary<string, string[]> _speciesByCategory = BuildSpeciesByCategory();

    public const int PanelWidth = 200;
    public const int ToggleButtonSize = 44;
    private const int HeaderHeight = 32;
    private const int SectionSpacing = 6;
    private const int ButtonHeight = 24;
    private const int ButtonSpacing = 2;
    private const int Margin = 10;
    private const int SearchBoxHeight = 24;
    private const int ScrollBarWidth = 6;

    private readonly List<UiButton> _categoryButtons = new();
    private readonly List<UiButton> _speciesButtons = new();
    private Rectangle _panelBounds;
    private Rectangle _toggleBounds;
    private Rectangle _speciesScrollArea;
    private int _viewportHeight;
    private Texture2D? _iconTexture;
    private int _scrollOffset;
    private int _maxScroll;
    private readonly UiTextInput _searchInput = new() { Placeholder = "Filter...", MaxLength = 20 };

    public SpawnPanel() { RebuildCategoryButtons(); }

    internal static IReadOnlyList<string> SpeciesForCategory(string category) =>
        BuildSpeciesByCategory().TryGetValue(category, out var species) ? species : Array.Empty<string>();

    public void RefreshSpeciesCatalog()
    {
        _speciesByCategory = BuildSpeciesByCategory();
        SelectedSpeciesKey = null;
        _searchInput.Clear();
        _lastSearchText = "";
        RebuildSpeciesButtons();
    }

    public void SetIconTexture(Texture2D? icon) => _iconTexture = icon;

    public void Open() => IsOpen = true;
    public void Close()
    {
        IsOpen = false;
        SelectedSpeciesKey = null;
        SelectedCategory = null;
        _scrollOffset = 0;
        _searchInput.Clear();
        _lastSearchText = "";
    }
    public void Toggle() => IsOpen = !IsOpen;
    public void DeselectSpecies() => SelectedSpeciesKey = null;

    public void SetViewportHeight(int h) => _viewportHeight = h;

    public bool HandleClick(MouseState mouse, MouseState previousMouse)
    {
        if (!WasClicked(mouse, previousMouse)) return false;
        var pos = mouse.Position;

        if (_toggleBounds.Contains(pos))
        {
            Toggle();
            Logger.Debug($"SpawnPanel: toggled, IsOpen={IsOpen}");
            return true;
        }

        if (!IsOpen) return false;

        foreach (var btn in _categoryButtons)
        {
            if (btn.Bounds.Contains(pos))
            {
                var clickedKey = btn.Tag as string; // Internal English key
                var newCategory = clickedKey == SelectedCategory ? null : clickedKey;
                if (newCategory != SelectedCategory)
                {
                    Logger.Debug($"SpawnPanel: category changed from '{SelectedCategory}' to '{newCategory}'");
                }
                SelectedCategory = newCategory;
                SelectedSpeciesKey = null;
                _scrollOffset = 0;
                _searchInput.Clear();
                _lastSearchText = "";
                RebuildSpeciesButtons();
                return true;
            }
        }
        foreach (var btn in _speciesButtons)
        {
            if (btn.Bounds.Contains(pos))
            {
                SelectedSpeciesKey = btn.Tag as string; // Store the internal key, not localized name
                Logger.Debug($"SpawnPanel: species selected '{SelectedSpeciesKey}'");
                return true;
            }
        }
        return false;
    }

    private string _lastSearchText = "";

    public bool Update(MouseState mouse, MouseState previousMouse,
        KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        UpdateLayout();
        HandleScrollWheel(mouse, previousMouse);
        _searchInput.Update(keyboard, previousKeyboard, mouse, previousMouse);
        if (_searchInput.Text != _lastSearchText)
        {
            _lastSearchText = _searchInput.Text;
            RebuildSpeciesButtons();
        }
        return HandleClick(mouse, previousMouse);
    }

    private void HandleScrollWheel(MouseState mouse, MouseState previousMouse)
    {
        if (!IsOpen || _speciesButtons.Count == 0) return;
        if (!_panelBounds.Contains(mouse.Position) && !_speciesScrollArea.Contains(mouse.Position))
            return;
        int delta = mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        if (delta == 0) return;
        _scrollOffset = Math.Clamp(_scrollOffset - delta / 120 * (ButtonHeight + ButtonSpacing),
            0, _maxScroll);
        LayoutSpeciesButtons();
    }

    private void UpdateLayout()
    {
        int toggleY = Margin + 10;
        _toggleBounds = new Rectangle(Margin, toggleY, ToggleButtonSize, ToggleButtonSize);
        if (IsOpen)
        {
            int panelY = toggleY + ToggleButtonSize + Margin;
            _panelBounds = new Rectangle(Margin, panelY, PanelWidth, ComputePanelHeight());
            LayoutCategoryButtons();
            int speciesStartY = _panelBounds.Y + HeaderHeight +
                _categoryButtons.Count * (ButtonHeight + ButtonSpacing + SectionSpacing);
            int maxSpeciesHeight = _viewportHeight - speciesStartY - Margin - 40;
            if (maxSpeciesHeight < 40) maxSpeciesHeight = 40;
            _speciesScrollArea = new Rectangle(_panelBounds.X + 8, speciesStartY,
                PanelWidth - 16, maxSpeciesHeight);
            int searchY = speciesStartY - SearchBoxHeight - 4;
            if (searchY < _panelBounds.Y + HeaderHeight + 10)
                searchY = _panelBounds.Y + HeaderHeight + 10;
            _searchInput.Bounds = new Rectangle(_panelBounds.X + 10, searchY,
                PanelWidth - 20 - ScrollBarWidth, SearchBoxHeight);
            LayoutSpeciesButtons();
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        UpdateLayout();
        DrawToggleButton(sb, pixel, font, mouse);

        if (!IsOpen) return;

        UiPrimitives.Fill(sb, pixel, _panelBounds, UiTheme.PanelBeige);
        UiPrimitives.Border(sb, pixel, _panelBounds, 2, UiTheme.ButtonShadow);

        sb.DrawString(font, I18n.T("spawn.title"),
            new Vector2(_panelBounds.X + 10, _panelBounds.Y + 6), UiTheme.MossSignal);

        foreach (var btn in _categoryButtons)
        {
            bool isSelected = btn.Tag as string == SelectedCategory;
            btn.Draw(sb, pixel, font, mouse, isSelected);
        }

        if (SelectedCategory != null)
        {
            _searchInput.Draw(sb, pixel, font, mouse);

            var originalScissor = sb.GraphicsDevice.ScissorRectangle;
            sb.End();
            var clipRect = new Rectangle(_speciesScrollArea.X, _speciesScrollArea.Y,
                _speciesScrollArea.Width, _speciesScrollArea.Height);
            sb.GraphicsDevice.ScissorRectangle = clipRect;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });

            foreach (var sBtn in _speciesButtons)
            {
                if (sBtn.Bounds.Bottom < _speciesScrollArea.Top || sBtn.Bounds.Top > _speciesScrollArea.Bottom)
                    continue;
                bool isSel = sBtn.Tag as string == SelectedSpeciesKey;
                sBtn.Draw(sb, pixel, font, mouse, isSel);
            }

            sb.End();
            sb.GraphicsDevice.ScissorRectangle = originalScissor;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = false });

            DrawScrollBar(sb, pixel);
        }

        if (SelectedSpeciesKey != null)
        {
            string selectedName = I18n.Species(SelectedSpeciesKey);
            string hint = I18n.T("spawn.selected") + ": " + selectedName;
            sb.DrawString(font, hint,
                new Vector2(_panelBounds.X + 10, _panelBounds.Bottom - 36),
                UiTheme.MossSignal);

            string clickHint = I18n.T("spawn.hint");
            sb.DrawString(font, clickHint,
                new Vector2(_panelBounds.X + 10, _panelBounds.Bottom - 22),
                UiTheme.WarmParchment);
        }
    }

    private void DrawScrollBar(SpriteBatch sb, Texture2D pixel)
    {
        if (_maxScroll <= 0) return;
        int barX = _speciesScrollArea.Right - ScrollBarWidth;
        int barH = _speciesScrollArea.Height;
        UiPrimitives.Fill(sb, pixel, new Rectangle(barX, _speciesScrollArea.Y, ScrollBarWidth, barH),
            new Color(20, 20, 20, 180));
        float thumbRatio = (float)barH / (barH + _maxScroll);
        int thumbH = Math.Max(12, (int)(barH * thumbRatio));
        int thumbY = _speciesScrollArea.Y + (int)((float)_scrollOffset / _maxScroll * (barH - thumbH));
        UiPrimitives.Fill(sb, pixel, new Rectangle(barX, thumbY, ScrollBarWidth, thumbH),
            new Color(107, 81, 55, 200));
    }

    private void DrawToggleButton(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        bool isHover = _toggleBounds.Contains(mouse.Position);
        Color bg = IsOpen ? new Color(78, 156, 181, 230) :
            (isHover ? UiTheme.ButtonFace : UiTheme.PanelBeige);
        UiPrimitives.Fill(sb, pixel, _toggleBounds, bg);
        UiPrimitives.Border(sb, pixel, _toggleBounds, 2, UiTheme.ButtonShadow);

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
        foreach (var category in CategoryOrder)
        {
            _categoryButtons.Add(new UiButton(I18n.T($"spawn.{category.ToLowerInvariant()}"))
            {
                Tag = category // Internal English key for selection logic
            });
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
        LayoutCategoryButtons();
    }

    private void LayoutCategoryButtons()
    {
        int y = _panelBounds.Y + HeaderHeight;
        foreach (var btn in _categoryButtons)
        {
            btn.Bounds = new Rectangle(_panelBounds.X + 10, y, PanelWidth - 20, ButtonHeight);
            y += ButtonHeight + ButtonSpacing + SectionSpacing;
        }
    }

    private void RebuildSpeciesButtons()
    {
        _speciesButtons.Clear();
        if (SelectedCategory == null) return;
        if (!_speciesByCategory.TryGetValue(SelectedCategory, out var species)) return;

        string filter = _searchInput.Text.Trim();
        foreach (var s in species)
        {
            string displayName = I18n.Species(s);
            if (filter.Length > 0 && !displayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !s.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            _speciesButtons.Add(new UiButton(displayName)
            {
                Tag = s
            });
        }
        _scrollOffset = 0;
        _maxScroll = 0;
        LayoutSpeciesButtons();
    }

    private void LayoutSpeciesButtons()
    {
        if (_speciesButtons.Count == 0) return;
        int speciesStartY = _panelBounds.Y + HeaderHeight +
            _categoryButtons.Count * (ButtonHeight + ButtonSpacing + SectionSpacing);
        int speciesAreaHeight = _speciesScrollArea.Height;
        int y = speciesStartY - _scrollOffset;
        foreach (var button in _speciesButtons)
        {
            button.Bounds = new Rectangle(_panelBounds.X + 20, y, PanelWidth - 30 - ScrollBarWidth, ButtonHeight);
            y += ButtonHeight + ButtonSpacing;
        }
        int totalHeight = _speciesButtons.Count * (ButtonHeight + ButtonSpacing) - ButtonSpacing;
        _maxScroll = Math.Max(0, totalHeight - speciesAreaHeight);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
    }

    private int ComputePanelHeight()
    {
        int h = HeaderHeight + 10;
        foreach (var _ in _categoryButtons)
            h += ButtonHeight + ButtonSpacing + SectionSpacing;
        h += SearchBoxHeight + 4;
        int speciesAreaHeight = 0;
        if (SelectedCategory != null && _speciesButtons.Count > 0)
            speciesAreaHeight = Math.Min(_speciesButtons.Count * (ButtonHeight + ButtonSpacing) + 8,
                _viewportHeight - Margin * 2 - ToggleButtonSize - HeaderHeight - 120);
        if (speciesAreaHeight < 30 && SelectedCategory != null) speciesAreaHeight = 30;
        h += speciesAreaHeight;
        h += 30;
        return Math.Min(h, _viewportHeight - Margin * 3 - ToggleButtonSize);
    }

    private static bool WasClicked(MouseState current, MouseState previous) =>
        current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;

    private static readonly HashSet<string> BirdSpecies = new(StringComparer.Ordinal)
    {
        "Eagle", "Owl"
    };

    private static readonly HashSet<string> MarineMammalSpecies = new(StringComparer.Ordinal)
    {
        "Dolphin", "Whale", "Manatee", "Orca", "Seal", "SeaLion", "Otter", "Walrus"
    };

    private static Dictionary<string, string[]> BuildSpeciesByCategory()
    {
        var categories = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string category in CategoryOrder)
            categories[category] = new List<string>();

        foreach (string species in SpeciesRegistry.All)
        {
            SpeciesDefinition? definition = SpeciesRegistry.Get(species);
            if (definition == null) continue;
        string category = definition.Kind switch
        {
            CreatureType.Plant when definition.IsAquatic => "AquaticPlants",
            CreatureType.Plant => "Plants",
            _ when BirdSpecies.Contains(species) => "Birds",
            _ when MarineMammalSpecies.Contains(species) => "MarineMammals",
            _ when definition.IsAquatic => "Fish",
            CreatureType.Herbivore => "LandHerbivores",
            CreatureType.Carnivore => "LandCarnivores",
            CreatureType.Omnivore => "LandOmnivores",
            _ => throw new InvalidOperationException($"Unsupported creature type: {definition.Kind}")
        };
            categories[category].Add(species);
        }

        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (string category in CategoryOrder)
            result[category] = categories[category].ToArray();
        return result;
    }
}
