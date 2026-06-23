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
    public bool IsOpen => _state.IsOpen;
    public string? SelectedSpeciesKey => _state.SelectedSpeciesKey;
    public string? SelectedCategory => _state.SelectedCategory;
    public string? SelectedCataclysm { get => _state.SelectedCataclysm; set => _state.SelectedCataclysm = value; }
    public bool ShowCataclysms { get => _state.ShowCataclysms; set => _state.ShowCataclysms = value; }

    private readonly SpawnPanelState _state = new();
    private Dictionary<string, string[]> _speciesByCategory = SpeciesCatalogModel.Build();

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
    private int ScrollOffset { get => _state.ScrollOffset; set => _state.ScrollOffset = value; }
    private int MaxScroll { get => _state.MaxScroll; set => _state.MaxScroll = value; }
    private readonly UiTextInput _searchInput = new() { Placeholder = "Filter...", MaxLength = 20 };

    public SpawnPanel() { RebuildCategoryButtons(); }

    internal static IReadOnlyList<string> SpeciesForCategory(string category) =>
        SpawnPanelState.SpeciesForCategory(category);

    public void RefreshSpeciesCatalog()
    {
        _speciesByCategory = SpeciesCatalogModel.Build();
        _state.DeselectSpecies();
        _searchInput.Clear();
        _lastSearchText = "";
        RebuildSpeciesButtons();
    }

    public void SetIconTexture(Texture2D? icon) => _iconTexture = icon;

    public void Open() => _state.Open();
    public void Close()
    {
        _state.Close();
        _searchInput.Clear();
        _lastSearchText = "";
    }
    public void Toggle() => _state.Toggle();
    public void DeselectSpecies() => _state.DeselectSpecies();

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
                var oldCategory = _state.SelectedCategory;
                if (!_state.SelectCategory(clickedKey)) return false;
                Logger.Debug($"SpawnPanel: category changed from '{oldCategory}' to '{_state.SelectedCategory}'");
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
                _state.SelectSpecies(btn.Tag as string);
                Logger.Debug($"SpawnPanel: species selected '{_state.SelectedSpeciesKey}'");
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
        ScrollOffset = Math.Clamp(ScrollOffset - delta / 120 * (ButtonHeight + ButtonSpacing),
            0, MaxScroll);
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

        UiPrimitives.Fill(sb, pixel, _panelBounds, new Color(11, 23, 18, 235));
        UiPrimitives.Border(sb, pixel, _panelBounds, 2, new Color(107, 81, 55));

        sb.DrawString(font, I18n.T("spawn.title"),
            new Vector2(_panelBounds.X + 10, _panelBounds.Y + 6), UiTheme.MossSignal);

        // Tab: Spawn / Cataclysms
        var tabSpawn = new UiButton("Spawn");
        var tabCata = new UiButton("Cataclysms");
        tabSpawn.Bounds = new Rectangle(_panelBounds.X + 60, _panelBounds.Y + 6, 60, 20);
        tabCata.Bounds = new Rectangle(_panelBounds.X + 124, _panelBounds.Y + 6, 70, 20);
        tabSpawn.Draw(sb, pixel, font, mouse, !ShowCataclysms);
        tabCata.Draw(sb, pixel, font, mouse, ShowCataclysms);
        if (WasClicked(mouse, previousMouseState) && tabSpawn.Bounds.Contains(mouse.Position)) ShowCataclysms = false;
        if (WasClicked(mouse, previousMouseState) && tabCata.Bounds.Contains(mouse.Position)) ShowCataclysms = true;

        if (ShowCataclysms)
        {
            DrawCataclysmButtons(sb, pixel, font, mouse);
            return;
        }

        foreach (var btn in _categoryButtons)
        {
            bool isSelected = btn.Tag as string == _state.SelectedCategory;
            btn.Draw(sb, pixel, font, mouse, isSelected);
        }

        if (_state.SelectedCategory != null)
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
                bool isSel = sBtn.Tag as string == _state.SelectedSpeciesKey;
                sBtn.Draw(sb, pixel, font, mouse, isSel);
            }

            sb.End();
            sb.GraphicsDevice.ScissorRectangle = originalScissor;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = false });

            DrawScrollBar(sb, pixel);
        }

        if (_state.SelectedSpeciesKey != null)
        {
            string selectedName = I18n.Species(_state.SelectedSpeciesKey!);
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
        if (MaxScroll <= 0) return;
        int barX = _speciesScrollArea.Right - ScrollBarWidth;
        int barH = _speciesScrollArea.Height;
        UiPrimitives.Fill(sb, pixel, new Rectangle(barX, _speciesScrollArea.Y, ScrollBarWidth, barH),
            new Color(20, 20, 20, 180));
        float thumbRatio = (float)barH / (barH + MaxScroll);
        int thumbH = Math.Max(16, (int)(barH * thumbRatio));
        int thumbY = _speciesScrollArea.Y + (int)((float)ScrollOffset / MaxScroll * (barH - thumbH));
        UiPrimitives.Fill(sb, pixel, new Rectangle(barX, thumbY, ScrollBarWidth, thumbH),
            new Color(107, 81, 55, 200));
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

    private readonly UiButton[] _cataButtons = new[]
    {
        new UiButton(I18n.T("cata.asteroid")) { Tag = "Asteroid" },
        new UiButton(I18n.T("cata.iceage")) { Tag = "IceAge" },
        new UiButton(I18n.T("cata.supervolcano")) { Tag = "Supervolcano" },
        new UiButton(I18n.T("cata.earthquake")) { Tag = "Earthquake" },
        new UiButton(I18n.T("cata.drought")) { Tag = "Drought" },
        new UiButton(I18n.T("cata.flood")) { Tag = "Flood" }
    };
    private MouseState previousMouseState;

    private void DrawCataclysmButtons(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        int y = _panelBounds.Y + 34;
        foreach (var btn in _cataButtons)
        {
            btn.Bounds = new Rectangle(_panelBounds.X + 10, y, PanelWidth - 20, 22);
            bool sel = SelectedCataclysm == (string)btn.Tag!;
            btn.Draw(sb, pixel, font, mouse, sel);
            y += 26;
        }
        if (!string.IsNullOrEmpty(SelectedCataclysm))
            sb.DrawString(font, I18n.T("cata.placeHint"), new Vector2(_panelBounds.X + 10, _panelBounds.Bottom - 30), UiTheme.MossSignal);
    }

    public bool HandleCataclysmClick(MouseState mouse, MouseState prevMouse)
    {
        previousMouseState = mouse;
        if (!IsOpen || !ShowCataclysms) return false;
        foreach (var btn in _cataButtons)
        {
            if (btn.WasClicked(mouse, prevMouse))
            {
                SelectedCataclysm = (string)btn.Tag!;
                return true;
            }
        }
        return false;
    }

    private void RebuildCategoryButtons()
    {
        _categoryButtons.Clear();
        int y = 0 + HeaderHeight;
        foreach (var category in SpawnPanelState.CategoryOrder)
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
        if (_state.SelectedCategory == null) return;
        if (!_speciesByCategory.TryGetValue(_state.SelectedCategory, out var species)) return;

        string filter = _searchInput.Text.Trim();
        foreach (var s in species)
        {
            string displayName = I18n.Species(s);
            var def = SpeciesRegistry.Get(s);
            string prefix = "";
            if (def != null && def.IsAquatic) prefix = "~ ";
            else if (s is "Eagle" or "Owl") prefix = "^ ";
            if (filter.Length > 0 && !displayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !s.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            _speciesButtons.Add(new UiButton(prefix + displayName)
            {
                Tag = s
            });
        }
        ScrollOffset = 0;
        MaxScroll = 0;
        LayoutSpeciesButtons();
    }

    private void LayoutSpeciesButtons()
    {
        if (_speciesButtons.Count == 0) return;
        int speciesStartY = _panelBounds.Y + HeaderHeight +
            _categoryButtons.Count * (ButtonHeight + ButtonSpacing + SectionSpacing);
        int speciesAreaHeight = _speciesScrollArea.Height;
        int y = speciesStartY - ScrollOffset;
        foreach (var button in _speciesButtons)
        {
            button.Bounds = new Rectangle(_panelBounds.X + 20, y, PanelWidth - 30 - ScrollBarWidth, ButtonHeight);
            y += ButtonHeight + ButtonSpacing;
        }
        int totalHeight = _speciesButtons.Count * (ButtonHeight + ButtonSpacing) - ButtonSpacing;
        MaxScroll = Math.Max(0, totalHeight - speciesAreaHeight);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, MaxScroll);
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

    private static readonly HashSet<string> MarineMammalSpecies = new(StringComparer.Ordinal)
    {
        "Dolphin", "Whale", "Manatee", "Orca", "Seal", "SeaLion", "Otter", "Walrus"
    };
}
