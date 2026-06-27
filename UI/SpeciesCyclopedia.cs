using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI;

public sealed class SpeciesCyclopedia
{
    public bool IsOpen { get; private set; }

    private const int CardWidth = 320;
    private const int CardHeight = 130;
    private const int CardsPerRow = 2;
    private const int Gap = 12;
    private const int PanelPadding = 16;
    private const int TitleHeight = 36;

    private int _scrollOffset;
    private int _maxScroll;
    private int _panelX, _panelY;
    private int _viewportW, _viewportH;

    private CreatureType _activeFilter = CreatureType.Plant;
    private static readonly CreatureType[] Filters = [CreatureType.Plant, CreatureType.Herbivore, CreatureType.Carnivore, CreatureType.Omnivore];

    private string _searchText = "";
    private bool _searchActive;
    private readonly UiTextInput _searchInput = new();

    private string[] _filteredSpecies = [];
    private int _selectedIndex = -1;
    private SpeciesDefinition? _selectedDef;

    public void Toggle(int vpw, int vph)
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            _selectedIndex = -1;
            _selectedDef = null;
            _searchText = "";
            _scrollOffset = 0;
            RefreshFilteredList();
        }
    }

    public void Close() { IsOpen = false; }

    public void Update(MouseState mouse, MouseState prevMouse, KeyboardState kbd, KeyboardState prevKbd,
        int viewportWidth, int viewportHeight)
    {
        if (!IsOpen) return;

        _viewportW = viewportWidth;
        _viewportH = viewportHeight;
        _panelX = viewportWidth / 2 - 360;
        _panelY = 40;

        HandleSearchInput(mouse, prevMouse, kbd, prevKbd);
        HandleFilterTabs(mouse, prevMouse);

        // Scroll with mouse wheel
        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _scrollOffset = Math.Clamp(_scrollOffset - scrollDelta / 40, 0, Math.Max(0, _maxScroll));
        }

        // Card click
        HandleCardClick(mouse, prevMouse);

        // Detail view: back
        if (_selectedDef != null && Pressed(kbd, prevKbd, Keys.Escape))
        {
            _selectedDef = null;
            _selectedIndex = -1;
        }

        // Keyboard navigation
        if (_selectedDef == null)
        {
            if (Pressed(kbd, prevKbd, Keys.Down)) NavigateSelection(1);
            if (Pressed(kbd, prevKbd, Keys.Up)) NavigateSelection(-1);
            if (Pressed(kbd, prevKbd, Keys.Enter)) OpenSelectedDetail();
        }
    }

    /// <summary>
    /// Handles the search input field logic.
    /// </summary>
    private void HandleSearchInput(MouseState mouse, MouseState prevMouse, KeyboardState kbd, KeyboardState prevKbd)
    {
        if (Pressed(kbd, prevKbd, Keys.F) && kbd.IsKeyDown(Keys.LeftControl))
            _searchActive = true;
        if (_searchActive && Pressed(kbd, prevKbd, Keys.Escape))
            _searchActive = false;

        if (_searchActive)
        {
            _searchInput.Update(kbd, prevKbd, mouse, prevMouse);
            if (_searchInput.Text != _searchText)
            {
                _searchText = _searchInput.Text;
                RefreshFilteredList();
            }
        }
    }

    /// <summary>
    /// Handles clicks on the filter tabs.
    /// </summary>
    private void HandleFilterTabs(MouseState mouse, MouseState prevMouse)
    {
        int tabX = _panelX + PanelPadding;
        for (int i = 0; i < Filters.Length; i++)
        {
            var tabRect = new Rectangle(tabX, _panelY, 100, 28);
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
                && tabRect.Contains(mouse.Position))
            {
                _activeFilter = Filters[i];
                RefreshFilteredList();
                _selectedIndex = -1;
                _selectedDef = null;
                _scrollOffset = 0;
            }
            tabX += 108;
        }
    }

    private void NavigateSelection(int delta)
    {
        if (_filteredSpecies.Length == 0) return;
        _selectedIndex = (_selectedIndex + delta + _filteredSpecies.Length) % _filteredSpecies.Length;
    }

    private void OpenSelectedDetail()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _filteredSpecies.Length)
            _selectedDef = SpeciesRegistry.Get(_filteredSpecies[_selectedIndex]);
    }

    private void HandleCardClick(MouseState mouse, MouseState prevMouse)
    {
        if (mouse.LeftButton != ButtonState.Pressed || prevMouse.LeftButton != ButtonState.Released) return;

        int cardAreaStartY = _panelY + TitleHeight + PanelPadding - _scrollOffset;
        int cols = (_viewportW - _panelX * 2 - PanelPadding * 2) / (CardWidth + Gap);
        if (cols < 1) cols = 1;

        for (int i = 0; i < _filteredSpecies.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            int cx = _panelX + PanelPadding + col * (CardWidth + Gap);
            int cy = cardAreaStartY + row * (CardHeight + Gap);

            if (cy + CardHeight < _panelY + TitleHeight || cy > _viewportH - 20) continue;

            var cardRect = new Rectangle(cx, cy, CardWidth, CardHeight);
            if (cardRect.Contains(mouse.Position))
            {
                _selectedDef = SpeciesRegistry.Get(_filteredSpecies[i]);
                _selectedIndex = i;
                break;
            }
        }
    }

    private void RefreshFilteredList()
    {
        var species = SpeciesRegistry.OfType(_activeFilter);
        if (!string.IsNullOrEmpty(_searchText))
        {
            species = species.Where(s =>
                s.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }
        _filteredSpecies = species.OrderBy(s => s).ToArray();
        _maxScroll = Math.Max(0, (_filteredSpecies.Length / CardsPerRow + 1) * (CardHeight + Gap) - (_viewportH - _panelY - 120));
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        if (!IsOpen) return;

        // Dark overlay
        UiPrimitives.Fill(sb, pixel, new Rectangle(0, 0, _viewportW, _viewportH),
            UiTheme.DeepGrove with { A = 220 });

        // Panel background
        var panelRect = new Rectangle(_panelX, _panelY, _viewportW - _panelX * 2, _viewportH - _panelY - 20);
        UiPrimitives.Fill(sb, pixel, panelRect, UiTheme.ForestNight);
        UiPrimitives.Border(sb, pixel, panelRect, 3, UiTheme.BarkEdge);

        // Title
        DrawText(sb, font, _panelX + PanelPadding, _panelY + 8, "ENCYCLOPEDIA", UiTheme.MossSignal);

        // Search bar
        int searchX = _panelX + PanelPadding + 440;
        if (_searchActive)
        {
            _searchInput.Bounds = new Rectangle(searchX, _panelY + 6, 180, 24);
            _searchInput.Draw(sb, pixel, font, Mouse.GetState());
        }
        else
        {
            DrawText(sb, font, searchX, _panelY + 8, "Ctrl+F per cercare...", UiTheme.MutedStone);
        }

        // Filter tabs
        int tabX = _panelX + PanelPadding;
        int tabsY = _panelY + TitleHeight - 8;
        for (int i = 0; i < Filters.Length; i++)
        {
            var tabRect = new Rectangle(tabX, tabsY, 100, 28);
            bool active = _activeFilter == Filters[i];
            UiPrimitives.Fill(sb, pixel, tabRect, active ? UiTheme.MossSignal : UiTheme.DeepGrove);
            UiPrimitives.Border(sb, pixel, tabRect, 1, UiTheme.BarkEdge);
            string label = Filters[i] switch
            {
                CreatureType.Plant => "🌿 Piante",
                CreatureType.Herbivore => "🐇 Erbivori",
                CreatureType.Carnivore => "🐺 Carnivori",
                CreatureType.Omnivore => "🐻 Onnivori",
                _ => ""
            };
            var pos = new Vector2(tabX + 6, tabsY + 6);
            sb.DrawString(font, label, pos, active ? UiTheme.DeepGrove : UiTheme.WarmParchment);
            tabX += 108;
        }

        // Species count
        DrawText(sb, font, _panelX + PanelPadding + 440, tabsY + 6,
            $"{_filteredSpecies.Length} specie", UiTheme.MutedStone);

        // Draw detail or list
        if (_selectedDef != null)
        {
            DrawDetailView(sb, pixel, font);
        }
        else
        {
            DrawCardList(sb, pixel, font);
        }

        // Close hint
        DrawText(sb, font, _panelX + PanelPadding, _viewportH - 30,
            "ESC: chiudi | Click: dettagli | Rotella: scroll", UiTheme.MutedStone);
    }

    private void DrawCardList(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        int contentY = _panelY + TitleHeight + 30 + PanelPadding - _scrollOffset;
        int contentH = _viewportH - contentY + _scrollOffset - 60;
        int cols = Math.Max(1, (panelRect().Width - PanelPadding * 2) / (CardWidth + Gap));

        for (int i = 0; i < _filteredSpecies.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            int cx = _panelX + PanelPadding + col * (CardWidth + Gap);
            int cy = contentY + row * (CardHeight + Gap);

            if (cy + CardHeight < _panelY + TitleHeight + 30 || cy > _viewportH - 20) continue;

            var cardRect = new Rectangle(cx, cy, CardWidth, CardHeight);
            bool selected = i == _selectedIndex;
            UiPrimitives.Fill(sb, pixel, cardRect, selected ? new Color(30, 60, 40) : UiTheme.DeepGrove);
            UiPrimitives.Border(sb, pixel, cardRect, 2, selected ? UiTheme.MossSignal : UiTheme.BarkEdge);

            var def = SpeciesRegistry.Get(_filteredSpecies[i]);
            if (def == null) continue;

            string name = _filteredSpecies[i];
            string sizeStr = def.Kind == CreatureType.Plant ? "" : $"Size: {def.DefaultSize:F1}";
            string social = def.Kind != CreatureType.Plant ? def.SocialBehavior.ToString() : "";
            string biomeStr = def.ValidBiomes.Count > 6 ? $"{def.ValidBiomes.Count} biomes" : string.Join(", ", def.ValidBiomes.Take(3));
            string traits = "";
            if (def.IsAquatic) traits += "Aquatic ";
            if (def.Hibernates) traits += "Hibernates ";
            if (def.PlantReproduction.HasValue) traits += def.PlantReproduction.Value.ToString();

            DrawText(sb, font, cx + 8, cy + 6, name, selected ? UiTheme.MossSignal : UiTheme.WarmParchment);
            DrawText(sb, font, cx + 8, cy + 26, $"{def.Kind} | {sizeStr}", UiTheme.MutedStone);
            DrawText(sb, font, cx + 8, cy + 46, social, UiTheme.LakeBlue);
            DrawText(sb, font, cx + 8, cy + 66, biomeStr, UiTheme.MutedStone);
            DrawText(sb, font, cx + 8, cy + 86, traits, new Color(200, 180, 120));
            if (def.Kind != CreatureType.Plant)
            {
                DrawText(sb, font, cx + 8, cy + 106, $"Maturity: {def.MaturityAge:F0}s", UiTheme.MutedStone);
            }
        }
    }

    private void DrawDetailView(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        if (_selectedDef == null) return;

        int contentX = _panelX + PanelPadding;
        int contentY = _panelY + TitleHeight + 36;
        int y = contentY;

        DrawText(sb, font, contentX, y, $"SPECIE: {_selectedDef.Species}", UiTheme.MossSignal);
        y += 28;
        DrawText(sb, font, contentX, y, $"Tipo: {_selectedDef.Kind}", UiTheme.WarmParchment);
        y += 20;
        DrawText(sb, font, contentX, y, $"Classe: {_selectedDef.CreatureType.Name}", UiTheme.WarmParchment);
        y += 24;
        DrawText(sb, font, contentX, y, "--- HABITAT ---", UiTheme.MossSignal);
        y += 22;
        DrawText(sb, font, contentX, y, $"Acquatico: {(_selectedDef.IsAquatic ? "Si" : "No")}", UiTheme.WarmParchment);
        y += 20;
        DrawText(sb, font, contentX, y, $"Biomi: {string.Join(", ", _selectedDef.ValidBiomes)}", UiTheme.WarmParchment);
        y += 24;
        DrawText(sb, font, contentX, y, "--- CARATTERISTICHE ---", UiTheme.MossSignal);
        y += 22;

        if (_selectedDef.Kind != CreatureType.Plant)
        {
            DrawText(sb, font, contentX, y, $"Taglia: {_selectedDef.DefaultSize:F1}", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Sociale: {_selectedDef.SocialBehavior}", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Matura a: {_selectedDef.MaturityAge:F0}s", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Iberna: {(_selectedDef.Hibernates ? "Si" : "No")}", UiTheme.WarmParchment);
        }
        else
        {
            DrawText(sb, font, contentX, y, $"Riproduzione: {_selectedDef.PlantReproduction}", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Impollinazione: {_selectedDef.Pollination}", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Temp min: {_selectedDef.MinTemperature:F0}°C", UiTheme.WarmParchment);
            y += 20;
            DrawText(sb, font, contentX, y, $"Temp max: {_selectedDef.MaxTemperature:F0}°C", UiTheme.WarmParchment);
        }

        y += 24;
        DrawText(sb, font, contentX, y, "ESC: torna alla lista", UiTheme.MutedStone);
    }

    private Rectangle panelRect() => new(_panelX, _panelY, _viewportW - _panelX * 2, _viewportH - _panelY - 20);

    private static void DrawText(SpriteBatch sb, SpriteFont font, int x, int y, string text, Color color)
    {
        sb.DrawString(font, text, new Vector2(x, y), color);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);
}
