using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using Microsoft.Xna.Framework.Input;
using PitLife.Simulation;
using PitLife.Localization;

namespace PitLife.UI;

public sealed class SpeciesCyclopedia
{
    private readonly StringBuilder _sb = new StringBuilder(128);
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

    private readonly System.Collections.Generic.List<string> _filteredSpecies = new();
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

        DrawSearchInput(mouse, prevMouse, kbd, prevKbd);
        HandleFilterTabs(mouse, prevMouse);
        HandleScroll(mouse, prevMouse);
        HandleCardClick(mouse, prevMouse);
        HandleKeyboardNavigation(kbd, prevKbd);
    }

    private void DrawSearchInput(MouseState mouse, MouseState prevMouse, KeyboardState kbd, KeyboardState prevKbd)
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

    private void HandleFilterTabs(MouseState mouse, MouseState prevMouse)
    {
        var tabX = _panelX + PanelPadding;
        for (var i = 0; i < Filters.Length; i++)
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

    private void HandleScroll(MouseState mouse, MouseState prevMouse)
    {
        var scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _scrollOffset = Math.Clamp(_scrollOffset - scrollDelta / 40, 0, Math.Max(0, _maxScroll));
        }
    }

    private void HandleKeyboardNavigation(KeyboardState kbd, KeyboardState prevKbd)
    {
        if (_selectedDef != null && Pressed(kbd, prevKbd, Keys.Escape))
        {
            _selectedDef = null;
            _selectedIndex = -1;
        }

        if (_selectedDef == null)
        {
            if (Pressed(kbd, prevKbd, Keys.Down)) NavigateSelection(1);
            if (Pressed(kbd, prevKbd, Keys.Up)) NavigateSelection(-1);
            if (Pressed(kbd, prevKbd, Keys.Enter)) OpenSelectedDetail();
        }
    }

    private void NavigateSelection(int delta)
    {
        if (_filteredSpecies.Count == 0) return;
        _selectedIndex = (_selectedIndex + delta + _filteredSpecies.Count) % _filteredSpecies.Count;
    }

    private void OpenSelectedDetail()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _filteredSpecies.Count)
            _selectedDef = SpeciesRegistry.Get(_filteredSpecies[_selectedIndex]);
    }

    private void HandleCardClick(MouseState mouse, MouseState prevMouse)
    {
        if (mouse.LeftButton != ButtonState.Pressed || prevMouse.LeftButton != ButtonState.Released) return;

        var cardAreaStartY = _panelY + TitleHeight + PanelPadding - _scrollOffset;
        var cols = (_viewportW - _panelX * 2 - PanelPadding * 2) / (CardWidth + Gap);
        if (cols < 1) cols = 1;

        for (var i = 0; i < _filteredSpecies.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;
            var cx = _panelX + PanelPadding + col * (CardWidth + Gap);
            var cy = cardAreaStartY + row * (CardHeight + Gap);

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
        _filteredSpecies.Clear();
        foreach (var s in SpeciesRegistry.OfType(_activeFilter))
        {
            if (string.IsNullOrEmpty(_searchText) || s.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            {
                _filteredSpecies.Add(s);
            }
        }
        _filteredSpecies.Sort(StringComparer.Ordinal);

        _maxScroll = Math.Max(0, (_filteredSpecies.Count / CardsPerRow + 1) * (CardHeight + Gap) - (_viewportH - _panelY - 120));
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
        var searchX = _panelX + PanelPadding + 440;
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
        var tabX = _panelX + PanelPadding;
        var tabsY = _panelY + TitleHeight - 8;
        for (var i = 0; i < Filters.Length; i++)
        {
            var tabRect = new Rectangle(tabX, tabsY, 100, 28);
            var active = _activeFilter == Filters[i];
            UiPrimitives.Fill(sb, pixel, tabRect, active ? UiTheme.MossSignal : UiTheme.DeepGrove);
            UiPrimitives.Border(sb, pixel, tabRect, 1, UiTheme.BarkEdge);
            var label = Filters[i] switch
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
            $"{_filteredSpecies.Count} specie", UiTheme.MutedStone);

        // Draw detail or list
        if (_selectedDef != null)
        {
            DrawDetailView(sb, pixel, font);
        }
        else if (_filteredSpecies.Count > 0)
        {
            DrawCardList(sb, pixel, font);
        }
        else
        {
            var msg = I18n.T("cyclopedia.empty");
            var size = font.MeasureString(msg);
            var px = _panelX + PanelPadding + 440;
            var py = _panelY + TitleHeight + 30 + PanelPadding;
            DrawText(sb, font, px, py, msg, UiTheme.MutedStone);
        }

        // Close hint
        DrawText(sb, font, _panelX + PanelPadding, _viewportH - 30,
            "ESC: chiudi | Click: dettagli | Rotella: scroll", UiTheme.MutedStone);
    }

    private void DrawCardList(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        var contentY = _panelY + TitleHeight + 30 + PanelPadding - _scrollOffset;
        var contentH = _viewportH - contentY + _scrollOffset - 60;

        if (_filteredSpecies.Count == 0)
        {
            var msg = "Nessuna specie trovata.";
            var msgSize = font.MeasureString(msg);
            var centerPos = new Vector2(_panelX + panelRect().Width / 2f - msgSize.X / 2f, _panelY + TitleHeight + 100);
            sb.DrawString(font, msg, centerPos, UiTheme.MutedStone);
        }

        var cols = Math.Max(1, (panelRect().Width - PanelPadding * 2) / (CardWidth + Gap));

        for (var i = 0; i < _filteredSpecies.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;
            var cx = _panelX + PanelPadding + col * (CardWidth + Gap);
            var cy = contentY + row * (CardHeight + Gap);

            if (cy + CardHeight < _panelY + TitleHeight + 30 || cy > _viewportH - 20) continue;

            var cardRect = new Rectangle(cx, cy, CardWidth, CardHeight);
            var selected = i == _selectedIndex;
            UiPrimitives.Fill(sb, pixel, cardRect, selected ? new Color(30, 60, 40) : UiTheme.DeepGrove);
            UiPrimitives.Border(sb, pixel, cardRect, 2, selected ? UiTheme.MossSignal : UiTheme.BarkEdge);

            var def = SpeciesRegistry.Get(_filteredSpecies[i]);
            if (def == null) continue;

            var name = _filteredSpecies[i];

            DrawText(sb, font, cx + 8, cy + 6, name, selected ? UiTheme.MossSignal : UiTheme.WarmParchment);

            _sb.Clear().Append(def.Kind.ToString());
            if (def.Kind != CreatureType.Plant)
            {
                _sb.Append($" | Size: {def.DefaultSize:F1}");
            }
            DrawText(sb, font, cx + 8, cy + 26, _sb, UiTheme.MutedStone);

            var social = def.Kind != CreatureType.Plant ? def.SocialBehavior.ToString() : string.Empty;
            DrawText(sb, font, cx + 8, cy + 46, social, UiTheme.LakeBlue);

            _sb.Clear();
            if (def.ValidBiomes.Count > 6)
            {
                _sb.Append(def.ValidBiomes.Count).Append(" biomes");
            }
            else
            {
                int b = 0;
                foreach (var biome in def.ValidBiomes)
                {
                    if (b >= 3) break;
                    if (b > 0) _sb.Append(", ");
                    _sb.Append(biome);
                    b++;
                }
            }
            DrawText(sb, font, cx + 8, cy + 66, _sb, UiTheme.MutedStone);

            _sb.Clear();
            if (def.IsAquatic) _sb.Append("Aquatic ");
            if (def.Hibernates) _sb.Append("Hibernates ");
            if (def.PlantReproduction.HasValue) _sb.Append(def.PlantReproduction.Value.ToString());
            DrawText(sb, font, cx + 8, cy + 86, _sb, new Color(200, 180, 120));

            if (def.Kind != CreatureType.Plant)
            {
                _sb.Clear().Append("Maturity: ").AppendFormat("{0:F0}", def.MaturityAge).Append("s");
                DrawText(sb, font, cx + 8, cy + 106, _sb, UiTheme.MutedStone);
            }
        }
    }

    private void DrawDetailView(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        if (_selectedDef == null) return;

        var contentX = _panelX + PanelPadding;
        var contentY = _panelY + TitleHeight + 36;
        var y = contentY;

        _sb.Clear().Append("SPECIE: ").Append(_selectedDef.Species);
        DrawText(sb, font, contentX, y, _sb, UiTheme.MossSignal);
        y += 28;
        _sb.Clear().Append("Tipo: ").Append(_selectedDef.Kind);
        DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        y += 20;
        _sb.Clear().Append("Classe: ").Append(_selectedDef.CreatureType.Name);
        DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        y += 24;
        DrawText(sb, font, contentX, y, "--- HABITAT ---", UiTheme.MossSignal);
        y += 22;
        _sb.Clear().Append("Acquatico: ").Append(_selectedDef.IsAquatic ? "Si" : "No");
        DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        y += 20;

        _sb.Clear().Append("Biomi: ");
        int biomeIdx = 0;
        foreach (var biome in _selectedDef.ValidBiomes)
        {
            if (biomeIdx > 0) _sb.Append(", ");
            _sb.Append(biome);
            biomeIdx++;
        }
        DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        y += 24;
        DrawText(sb, font, contentX, y, "--- CARATTERISTICHE ---", UiTheme.MossSignal);
        y += 22;

        if (_selectedDef.Kind != CreatureType.Plant)
        {
            _sb.Clear().Append("Taglia: ").AppendFormat("{0:F1}", _selectedDef.DefaultSize);
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Sociale: ").Append(_selectedDef.SocialBehavior);
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Matura a: ").AppendFormat("{0:F0}", _selectedDef.MaturityAge).Append("s");
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Iberna: ").Append(_selectedDef.Hibernates ? "Si" : "No");
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        }
        else
        {
            _sb.Clear().Append("Riproduzione: ").Append(_selectedDef.PlantReproduction);
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Impollinazione: ").Append(_selectedDef.Pollination);
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Temp min: ").AppendFormat("{0:F0}", _selectedDef.MinTemperature).Append("°C");
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
            y += 20;
            _sb.Clear().Append("Temp max: ").AppendFormat("{0:F0}", _selectedDef.MaxTemperature).Append("°C");
            DrawText(sb, font, contentX, y, _sb, UiTheme.WarmParchment);
        }

        y += 24;
        DrawText(sb, font, contentX, y, "ESC: torna alla lista", UiTheme.MutedStone);
    }

    private Rectangle panelRect() => new(_panelX, _panelY, _viewportW - _panelX * 2, _viewportH - _panelY - 20);

    private static void DrawText(SpriteBatch sb, SpriteFont font, int x, int y, string text, Color color)
    {
        sb.DrawString(font, text, new Vector2(x, y), color);
    }

    private static void DrawText(SpriteBatch sb, SpriteFont font, int x, int y, StringBuilder text, Color color)
    {
        sb.DrawString(font, text, new Vector2(x, y), color);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);
}
