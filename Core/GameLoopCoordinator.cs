using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;

namespace PitLife.Core;

public class GameLoopCoordinator
{
    private readonly Game1 _game;

    public GameLoopCoordinator(Game1 game)
    {
        _game = game;
    }

    public void Update(GameTime gameTime)
    {
        var input = _game._inputManager;
        input.Update();

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_game._showLoadingTimer > 0)
        {
            _game._showLoadingTimer -= dt;
            if (_game._pendingWorldGen && _game._showLoadingTimer <= 0.8f)
            {
                _game._orchestrator.GenerateNewWorld(_game._pendingSeed, _game._pendingOptions);
                _game._pendingWorldGen = false;
                _game._screen = Game1.GameScreen.Playing;
                _game._paused = false;
                _game._controller.SetPause(false);
            }
            return;
        }

        UpdateFPS(gameTime);

        var kbd = input.CurrentKbd;
        var mouse = input.CurrentMouse;
        var prevKbd = input.PrevKbd;
        var prevMouse = input.PrevMouse;
        var escapePressed = input.IsKeyJustPressed(Keys.Escape);
        var gamepadBack = input.IsGamepadBackPressed();

        if (_game.GraphicsDevice != null)
        {
            _game._camera.ViewportWidth = _game.GraphicsDevice?.Viewport.Width ?? 0;
            _game._camera.ViewportHeight = _game.GraphicsDevice?.Viewport.Height ?? 0;
        }

        if (UpdateMainMenu(gameTime, dt, kbd, mouse, gamepadBack)) return;

        if (HandleKeyboardInput(gameTime, dt, kbd, mouse, escapePressed)) return;

        if (_game._speciesEditor.IsOpen)
        {
            if (escapePressed)
                _game._speciesEditor.Close();
            else
                _game._speciesEditor.Update(mouse, prevMouse, kbd, prevKbd,
                    _game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);

            return;
        }

        if (escapePressed && _game._inGameUi.CloseTopWindow())
        {
            return;
        }

        if (escapePressed || gamepadBack)
        {
            _game._mainMenu.CloseWorldGenPanel();
            _game._mainMenu.GameInProgress = true;
            _game._screen = Game1.GameScreen.MainMenu;
            _game._paused = true;
            return;
        }

        var pointerOverUi = _game._inGameUi.Update(
            mouse,
            prevMouse,
            kbd,
            prevKbd,
            _game.GraphicsDevice?.Viewport.Width ?? 0,
            _game.GraphicsDevice?.Viewport.Height ?? 0);

        var worldMouse = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
        var hx = (int)(worldMouse.X / _game._ecosystem.World.TileSize);
        var hy = (int)(worldMouse.Y / _game._ecosystem.World.TileSize);
        _game._inGameUi.HoverTile = new Point(
            Math.Clamp(hx, 0, _game._ecosystem.World.Width - 1),
            Math.Clamp(hy, 0, _game._ecosystem.World.Height - 1));

        if (mouse.LeftButton == ButtonState.Pressed &&
            _game._minimap.HandleClick(mouse, _game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0))
        {
            return;
        }

        if (_game._inGameUi.WantsToGoToMainMenu)
        {
            _game._inGameUi.WantsToGoToMainMenu = false;
            _game._mainMenu.CloseWorldGenPanel();
            _game._mainMenu.GameInProgress = true;
            _game._screen = Game1.GameScreen.MainMenu;
            _game._paused = true;
            return;
        }

        _game._spawnPanel.SetViewportHeight(_game.GraphicsDevice?.Viewport.Height ?? 0);
        var cataWasOpen = _game._cataclysmPanel.IsOpen;
        var spawnWasOpen = _game._spawnPanel.IsOpen;
        var spawnPanelConsumed = _game._spawnPanel.Update(mouse, prevMouse, kbd, prevKbd);
        var cataConsumed = _game._cataclysmPanel.Update(mouse, prevMouse);
        spawnPanelConsumed = spawnPanelConsumed || _game._spawnPanel.HandleCataclysmClick(mouse, prevMouse);

        if ((_game._cataclysmPanel.IsOpen && !cataWasOpen) || (_game._spawnPanel.IsOpen && !spawnWasOpen))
            _game._inGameUi.CloseAllWindows();

        if (_game._cataclysmPanel.IsOpen && !cataWasOpen)
            _game._spawnPanel.Close();
        if (_game._spawnPanel.IsOpen && !spawnWasOpen)
        {
            _game._cataclysmPanel.Close();
            _game._speciesEditor.Close();
        }

        // ── Mutual exclusion: only one mode active ────────────
        var spawnJustSelected = _game._spawnPanel.SelectedCataclysm != null && _game._spawnPanel.SelectedCataclysm != _game._prevSpawnCata;
        var speciesJustSelected = _game._spawnPanel.SelectedSpeciesKey != null && _game._spawnPanel.SelectedSpeciesKey != _game._prevSpawnSpecies;
        var panelJustSelected = _game._cataclysmPanel.SelectedType != null && _game._cataclysmPanel.SelectedType != _game._prevPanelCata;

        if (spawnJustSelected || speciesJustSelected)
        {
            _game._prevSpawnCata = _game._spawnPanel.SelectedCataclysm;
            _game._prevSpawnSpecies = _game._spawnPanel.SelectedSpeciesKey;
            if (spawnJustSelected) _game._cataSelectedFrame = _game._gameFrame;
            _game._cataclysmPanel.SelectedType = null;
            _game._inGameUi.SelectedCataclysm = null;
            _game._prevPanelCata = null;
        }
        if (panelJustSelected)
        {
            _game._prevPanelCata = _game._cataclysmPanel.SelectedType;
            _game._cataSelectedFrame = _game._gameFrame;
            _game._spawnPanel.SelectedCataclysm = null;
            _game._spawnPanel.DeselectSpecies();
            _game._inGameUi.SelectedCataclysm = null;
            _game._prevSpawnCata = null;
            _game._prevSpawnSpecies = null;
        }
        if (_game._cataclysmPanel.SelectedType == null && !_game._cataclysmPanel.IsOpen) _game._prevPanelCata = null;
        if (_game._spawnPanel.SelectedCataclysm == null) _game._prevSpawnCata = null;

        _game._controller.Advance(dt);
        _game._paused = _game._controller.IsPaused;
        _game._displayPlants = _game._controller.PlantCount;
        _game._displayHerbivores = _game._controller.HerbivoreCount;
        _game._displayCarnivores = _game._controller.CarnivoreCount;
        _game._displayOmnivores = _game._controller.OmnivoreCount;
        _game._displayTime = _game._controller.TotalTime;

        _game._weather.Update(_game._ecosystem.Climate, _game._camera, dt, _game._ecosystem.World.PixelWidth, _game._ecosystem.World.PixelHeight);
        _game._waterEffect.Update(dt);

        _game._inGameUi.RecordPopSnapshot(_game._displayPlants, _game._displayHerbivores, _game._displayCarnivores, _game._displayOmnivores,
            dt * _game._controller.CurrentSpeed);

        HandleMouseClicks(mouse, pointerOverUi, spawnPanelConsumed, cataConsumed);
        _game._gameFrame++;
    }

    public void Draw(GameTime gameTime)
    {
        _game.GraphicsDevice.Clear(Color.Black);

        if (_game._screen == Game1.GameScreen.Playing)
        {
            DrawWorld(gameTime);
        }

        _game._spriteBatch.Begin();
        if (_game._showLoadingTimer > 0)
        {
            DrawLoadingScreen(_game._spriteBatch);
            _game._spriteBatch.End();
            return;
        }
        if (_game._screen == Game1.GameScreen.MainMenu)
        {
            _game._mainMenu.Draw(
                _game._spriteBatch,
                _game._uiPixel,
                _game._font,
                _game._logo,
                _game.GraphicsDevice?.Viewport.Width ?? 0,
                _game.GraphicsDevice?.Viewport.Height ?? 0);
            _game._helpScreen.Draw(
                _game._spriteBatch,
                _game._uiPixel,
                _game._font,
                _game.GraphicsDevice?.Viewport.Width ?? 0,
                _game.GraphicsDevice?.Viewport.Height ?? 0);
            _game._spriteBatch.End();
            return;
        }

        DrawHUD(_game._spriteBatch, _game._font);
        DrawDebugOverlay(_game._spriteBatch, _game._font);

        if (_game._logo != null)
        {
            const int logoSize = 96;
            _game._spriteBatch.Draw(_game._logo,
                new Rectangle(_game.GraphicsDevice?.Viewport.Width ?? 0 - logoSize - 10, 10, logoSize, logoSize),
                Color.White);
        }

        _game._inGameUi.Draw(
            _game._spriteBatch,
            _game._uiPixel,
            _game._font,
            _game._inputManager.CurrentMouse,
            _game._selectedCreature,
            _game._displayPlants,
            _game._displayHerbivores,
            _game._displayCarnivores,
            _game._displayOmnivores,
            _game._displayTime,
            _game._paused,
            _game._controller.CurrentSpeed,
            _game.GraphicsDevice?.Viewport.Height ?? 0,
            _game._ecosystem.Metrics);

        _game._camera.ViewportWidth = _game.GraphicsDevice?.Viewport.Width ?? 0;
        _game._camera.ViewportHeight = _game.GraphicsDevice?.Viewport.Height ?? 0;
        _game._minimap.Draw(_game._spriteBatch, _game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);
        _game._spawnPanel.Draw(_game._spriteBatch, _game._uiPixel, _game._font, _game._inputManager.CurrentMouse);
        _game._cataclysmPanel.Draw(_game._spriteBatch, _game._uiPixel, _game._font, _game._inputManager.CurrentMouse);
        _game._speciesEditor.Draw(
            _game._spriteBatch,
            _game._uiPixel,
            _game._font,
            _game._inputManager.CurrentMouse,
            _game.GraphicsDevice?.Viewport.Width ?? 0,
            _game.GraphicsDevice?.Viewport.Height ?? 0);
        _game._cyclopedia.Draw(_game._spriteBatch, _game._uiPixel, _game._font);
        _game._spriteBatch.End();
    }

    private bool UpdateMainMenu(GameTime gameTime, float dt, KeyboardState kbd, MouseState mouse, bool gamepadBack)
    {
        if (_game._screen != Game1.GameScreen.MainMenu) return false;

        if (!_game._mainMenu.IsWorldGenPanelOpen && !_game._paused)
        {
            _game._ecosystem.SimulationSpeed = 0.35f;
            _game._ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(dt), TimeSpan.FromSeconds(dt)));
            _game._dayNight.Update(_game._ecosystem.TotalTime);
        }

        var input = _game._inputManager;

        if (_game._helpScreen.IsActive)
        {
            _game._helpScreen.Update(
                mouse,
                input.PrevMouse,
                kbd,
                input.PrevKbd,
                _game.GraphicsDevice?.Viewport.Width ?? 0,
                _game.GraphicsDevice?.Viewport.Height ?? 0);
            if (gamepadBack)
                _game._helpScreen.Hide();
            return true;
        }

        _game._menuInputCooldown = Math.Max(0f, _game._menuInputCooldown - dt);
        var vw = _game.GraphicsDevice?.Viewport.Width ?? 1280;
        var vh = _game.GraphicsDevice?.Viewport.Height ?? 800;
        MenuAction action = _game._menuInputCooldown > 0f
            ? MenuAction.None
            : _game._mainMenu.Update(
                mouse,
                input.PrevMouse,
                kbd,
                input.PrevKbd,
                vw,
                vh,
                _game._graphics.IsFullScreen);

        switch (action)
        {
            case MenuAction.StartGame:
                _game._screen = Game1.GameScreen.Playing;
                _game._paused = false;
                _game._controller.SetPause(false);
                break;
            case MenuAction.NewWorld:
                _game._mainMenu.CloseWorldGenPanel();
                _game._mainMenu.GameInProgress = false;
                _game._pendingWorldGen = true;
                _game._pendingSeed = null;
                _game._pendingOptions = _game._mainMenu.CurrentOptions;
                _game._showLoadingTimer = 1.5f;
                break;
            case MenuAction.NewWorldWithSeed:
                _game._mainMenu.CloseWorldGenPanel();
                _game._mainMenu.GameInProgress = false;
                _game._pendingWorldGen = true;
                _game._pendingSeed = _game._mainMenu.Seed;
                _game._pendingOptions = _game._mainMenu.CurrentOptions;
                _game._showLoadingTimer = 1.5f;
                break;
            case MenuAction.SaveGame:
                SaveSystem.Save("savegame.json", _game._ecosystem);
                _game._menuInputCooldown = 0.5f;
                break;
            case MenuAction.LoadGame:
                try
                {
                    var saveData = SaveSystem.Load("savegame.json");
                    if (saveData != null)
                    {
                        _game._orchestrator.RestoreLoadedEcosystem(saveData);
                        _game._screen = Game1.GameScreen.Playing;
                        _game._paused = false;
                        _game._controller.SetPause(false);
                    }
                }
                catch (InvalidDataException ex)
                {
                    Logger.Error($"Failed to load save: {ex.Message}");
                }
                _game._menuInputCooldown = 0.5f;
                break;
            case MenuAction.ToggleFullscreen:
                _game._graphics.ToggleFullScreen();
                _game._menuInputCooldown = 0.5f;
                break;
            case MenuAction.ShowHelp:
                _game._helpScreen.Show();
                break;
            case MenuAction.Exit:
                _game.Exit();
                break;
        }

        if (gamepadBack)
            _game.Exit();

        return true;
    }

    private bool HandleKeyboardInput(GameTime gameTime, float dt, KeyboardState kbd, MouseState mouse, bool escapePressed)
    {
        var input = _game._inputManager;

        // Debug
        if (input.IsKeyJustPressed(Keys.F1))
            _game._showDebugOverlay = !_game._showDebugOverlay;

        // Species Cyclopedia
        if (input.IsKeyJustPressed(Keys.G))
            _game._cyclopedia.Toggle(_game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);

        // Manual Cataclysm
        if (input.IsKeyJustPressed(Keys.F7))
            _game._ecosystem.Cataclysms.TriggerManual(_game._ecosystem, _game._ecosystem.Random);

        // Species Editor
        if (input.IsKeyJustPressed(Keys.F6))
        {
            _game._speciesEditor.Toggle();
            if (_game._cyclopedia.IsOpen)
            {
                if (escapePressed)
                    _game._cyclopedia.Close();
                else
                    _game._cyclopedia.Update(mouse, input.PrevMouse, kbd, input.PrevKbd,
                        _game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);

                return true;
            }

            if (_game._speciesEditor.IsOpen)
            {
                _game._inGameUi.CloseAllWindows();
                if (_game._cataclysmPanel.IsOpen) _game._cataclysmPanel.Close();
                if (_game._spawnPanel.IsOpen) _game._spawnPanel.Close();
            }
        }

        // Cataclysm Panel
        if (input.IsKeyJustPressed(Keys.C))
            _game._cataclysmPanel.Toggle();

        // Spawn Panel
        if (input.IsKeyJustPressed(Keys.F4))
            _game._spawnPanel.Toggle();

        // ESC: close panel if open, otherwise cancel active mode
        if (escapePressed)
        {
            if (_game._cataclysmPanel.IsOpen)
                _game._cataclysmPanel.Toggle();
            else if (_game._spawnPanel.IsOpen)
                _game._spawnPanel.Toggle();
            if (_game._cataclysmPanel.SelectedType != null || _game._spawnPanel.SelectedCataclysm != null || _game._spawnPanel.SelectedSpeciesKey != null)
            {
                _game._spawnPanel.SelectedCataclysm = null;
                _game._spawnPanel.DeselectSpecies();
                _game._cataclysmPanel.SelectedType = null;
                _game._prevPanelCata = null;
                _game._prevSpawnCata = null;
                _game._prevSpawnSpecies = null;
                _game._cataSelectedFrame = 0;
            }
        }

        // Camera & Time Controls
        input.UpdateCamera(_game._camera, dt);
        if (input.IsKeyJustPressed(Keys.Up))
            _game._controller.SetSpeed(Math.Min(3, _game._controller.SpeedLevel + 1));
        if (input.IsKeyJustPressed(Keys.Down))
            _game._controller.SetSpeed(Math.Max(0, _game._controller.SpeedLevel - 1));
        if (input.IsKeyJustPressed(Keys.D1)) _game._controller.SetSpeed(1);
        if (input.IsKeyJustPressed(Keys.D2)) _game._controller.SetSpeed(2);
        if (input.IsKeyJustPressed(Keys.D3)) _game._controller.SetSpeed(3);
        if (input.IsKeyJustPressed(Keys.Space)) _game._controller.TogglePause();

        if (_game._inGameUi.SpeedUpRequested)
        {
            _game._controller.SetSpeed(Math.Min(3, _game._controller.SpeedLevel + 1));
            _game._inGameUi.SpeedUpRequested = false;
        }
        if (_game._inGameUi.SpeedDownRequested)
        {
            _game._controller.SetSpeed(Math.Max(0, _game._controller.SpeedLevel - 1));
            _game._inGameUi.SpeedDownRequested = false;
        }

        return false;
    }

    private void HandleMouseClicks(MouseState mouse, bool pointerOverUi, bool spawnPanelConsumed, bool cataConsumed)
    {
        var input = _game._inputManager;

        // Cataclysm placement when selected (require at least 1 frame delay after selection)
        var cataReady = _game._cataSelectedFrame > 0 && (_game._gameFrame - _game._cataSelectedFrame) >= 1;
        if (_game._cataclysmPanel.SelectedType != null && cataReady &&
            !pointerOverUi && !spawnPanelConsumed && !cataConsumed &&
            input.IsLeftClickJustPressed())
        {
            var catPos = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
            _game._ecosystem.Cataclysms.TriggerAt(_game._ecosystem, _game._ecosystem.Random, _game._cataclysmPanel.SelectedType!, catPos);
            _game._worldRenderer.Invalidate();
            _game._cataclysmPanel.SelectedType = null;
            _game._prevPanelCata = null;
            _game._cataSelectedFrame = 0;
        }
        else if (_game._inGameUi.SelectedCataclysm != null &&
            !pointerOverUi && !spawnPanelConsumed && !cataConsumed &&
            input.IsLeftClickJustPressed())
        {
            var catPos = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
            _game._ecosystem.Cataclysms.TriggerAt(_game._ecosystem, _game._ecosystem.Random, _game._inGameUi.SelectedCataclysm, catPos);
            _game._worldRenderer.Invalidate();
            _game._inGameUi.SelectedCataclysm = null;
        }
        else if (_game._spawnPanel.IsOpen && _game._spawnPanel.SelectedSpeciesKey != null &&
            !spawnPanelConsumed && !cataConsumed && !pointerOverUi &&
            input.IsLeftClickJustPressed())
        {
            var spawnPos = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
            var spawned = 0;
            for (var i = 0; i < 3; i++)
            {
                var offset = new Vector2((float)(_game._ecosystem.Random.NextDouble() - 0.5) * 40,
                    (float)(_game._ecosystem.Random.NextDouble() - 0.5) * 40);
                if (_game._ecosystem.SpawnByName(_game._spawnPanel.SelectedSpeciesKey, spawnPos + offset))
                    spawned++;
            }
            if (spawned > 0)
            {
                Logger.Event("SPAWN", $"Player spawned {spawned}x {_game._spawnPanel.SelectedSpeciesKey} at ({spawnPos.X:F0}, {spawnPos.Y:F0})");
                _game._spawnPanel.DeselectSpecies();
                _game._prevSpawnSpecies = null;
            }
            else
            {
                var tile = _game._ecosystem.World.GetTileAtPosition(spawnPos.X, spawnPos.Y);
                Logger.Warn($"Spawn failed for {_game._spawnPanel.SelectedSpeciesKey} at ({spawnPos.X:F0}, {spawnPos.Y:F0}) - biome={tile.Biome}. Try a different location.");
            }
        }
        else if (!pointerOverUi && !spawnPanelConsumed && !cataConsumed && cataReady &&
            _game._spawnPanel.SelectedCataclysm != null &&
            input.IsLeftClickJustPressed())
        {
            var catPos = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
            _game._ecosystem.Cataclysms.TriggerAt(_game._ecosystem, _game._ecosystem.Random, _game._spawnPanel.SelectedCataclysm, catPos);
            _game._worldRenderer.Invalidate();
            _game._spawnPanel.SelectedCataclysm = null;
            _game._prevSpawnCata = null;
            _game._cataSelectedFrame = 0;
        }
        else if (!pointerOverUi && !spawnPanelConsumed && !cataConsumed &&
                 input.IsLeftClickJustPressed())
        {
            if (_game._cataclysmPanel.IsOpen) _game._cataclysmPanel.Close();
            if (_game._spawnPanel.IsOpen) _game._spawnPanel.Close();
            if (_game._speciesEditor.IsOpen) _game._speciesEditor.Close();
            var worldPos = _game._camera.ScreenToWorld(mouse.X, mouse.Y);
            _game._selectedCreature = Game1.FindClosestCreature(_game._ecosystem.Creatures, worldPos);
            if (_game._selectedCreature != null)
            {
                _game._inGameUi.OpenCreatureWindow(_game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);
            }
            else
            {
                var tileX = (int)(worldPos.X / _game._ecosystem.World.TileSize);
                var tileY = (int)(worldPos.Y / _game._ecosystem.World.TileSize);
                tileX = Math.Clamp(tileX, 0, _game._ecosystem.World.Width - 1);
                tileY = Math.Clamp(tileY, 0, _game._ecosystem.World.Height - 1);
                _game._inGameUi.SelectedTile = new Point(tileX, tileY);
                _game._inGameUi.OpenTerrainWindow(_game.GraphicsDevice?.Viewport.Width ?? 0, _game.GraphicsDevice?.Viewport.Height ?? 0);
            }
        }
    }

    private void UpdateFPS(GameTime gameTime)
    {
        _game._frameCount++;
        _game._fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_game._fpsTimer >= 0.5)
        {
            _game._currentFPS = _game._frameCount / (float)_game._fpsTimer;
            _game._frametimeMS = (float)(_game._fpsTimer / _game._frameCount * 1000.0);
            _game._frameCount = 0;
            _game._fpsTimer = 0;
        }
    }

    private void DrawWorld(GameTime gameTime)
    {
        var shake = _game._ecosystem.Cataclysms.ScreenShake;
        Vector2 savedPos = _game._camera.Position;
        if (shake != Vector2.Zero)
            _game._camera.Position = new Vector2(savedPos.X + shake.X, savedPos.Y + shake.Y);

        _game._spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _game._camera.TransformMatrix);
        _game._worldRenderer.Draw(_game._spriteBatch, _game._camera);
        _game._spriteBatch.End();

        _game._spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _game._camera.TransformMatrix,
            blendState: BlendState.Additive);
        Color seasonTint = _game._ecosystem.Climate.CurrentSeason switch
        {
            Season.Spring => new Color(40, 120, 40, 6),
            Season.Summer => new Color(120, 100, 20, 8),
            Season.Autumn => new Color(100, 70, 20, 8),
            Season.Winter => new Color(60, 80, 160, 10),
            _ => Color.Transparent
        };
        var tempAlpha = Math.Clamp((_game._ecosystem.Climate.TemperatureModifier + 0.15f) / 0.3f, 0f, 1f);
        Color tempBlend = Color.Lerp(new Color(40, 80, 200, 4), new Color(200, 80, 40, 6), tempAlpha);
        _game._spriteBatch.Draw(_game._uiPixel, _game._camera.VisibleArea, seasonTint);
        _game._spriteBatch.Draw(_game._uiPixel, _game._camera.VisibleArea, tempBlend);
        _game._spriteBatch.End();

        var isSnow = _game._ecosystem.Climate.CurrentSeason == Season.Winter || _game._ecosystem.Climate.TemperatureModifier < -0.05f;
        _game._spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _game._camera.TransformMatrix);
        _game._weather.Draw(_game._spriteBatch, _game._uiPixel, isSnow);
        _game._waterEffect.Draw(_game._spriteBatch, _game._uiPixel, _game._ecosystem.World, _game._camera);
        _game._spriteBatch.End();

        _game._spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _game._camera.TransformMatrix);
        _game._creatureRenderer.Draw(_game._spriteBatch, _game._camera, _game._dayNight.GetOverlayColor(), _game._font);
        if (_game._selectedCreature != null && _game._selectedCreature.IsAlive)
        {
            var center = _game._selectedCreature.Position;
            _game._spriteBatch.DrawString(_game._font, "X", center - new Vector2(8, 14), Color.Yellow);
        }
        _game._ecosystem.Cataclysms.Draw(_game._spriteBatch, _game._uiPixel, _game._camera.VisibleArea);
        DrawFruits(_game._spriteBatch, _game._camera.VisibleArea);
        _game._spriteBatch.End();

        _game._camera.Position = savedPos;
    }

    private void DrawFruits(SpriteBatch sb, Rectangle visibleArea)
    {
        var left = visibleArea.X - 2;
        var top = visibleArea.Y - 2;
        var right = visibleArea.Right + 2;
        var bottom = visibleArea.Bottom + 2;

        foreach (var fruit in _game._ecosystem.Fruits.Fruits)
        {
            if (!fruit.IsAlive) continue;
            var px = (int)fruit.Position.X;
            var py = (int)fruit.Position.Y;

            if (px < left || px > right || py < top || py > bottom) continue;

            var color = fruit.GetColor();
            sb.Draw(_game._uiPixel, new Rectangle(px - 1, py - 1, 2, 2), color);
        }
    }

    private void DrawHUD(SpriteBatch sb, SpriteFont font)
    {
        _game._sb.Clear();
        var years = _game._displayTime / 480f + 1;
        _game._sb.Append($"Year {years:F1} | P:{_game._displayPlants} H:{_game._displayHerbivores} C:{_game._displayCarnivores} O:{_game._displayOmnivores} | ");
        if (_game._paused) _game._sb.Append(I18n.T("hud.paused"));
        else _game._sb.Append($"{_game._controller.CurrentSpeed}x");
        sb.DrawString(font, _game._sb, new Vector2(10, 10), Color.White);

        sb.DrawString(font, I18n.T("hud.controls"),
            new Vector2(10, 32), new Color(160, 160, 160));
        
        _game._sb.Clear();
        var phaseKey = _game._dayNight.Phase switch
        {
            DayPhase.Dawn => "dayphase.dawn",
            DayPhase.Day => "dayphase.day",
            DayPhase.Dusk => "dayphase.dusk",
            DayPhase.Night => "dayphase.night",
            _ => "dayphase.day"
        };
        _game._sb.Append(I18n.T(phaseKey));
        sb.DrawString(font, _game._sb, new Vector2(10, 54), GetPhaseColor(_game._dayNight.Phase));
        
        _game._sb.Clear();
        var seasonKey = _game._ecosystem.Climate.CurrentSeason switch
        {
            Season.Spring => "season.Spring",
            Season.Summer => "season.Summer",
            Season.Autumn => "season.Autumn",
            Season.Winter => "season.Winter",
            _ => "season.Spring"
        };
        _game._sb.Append(I18n.T(seasonKey));
        sb.DrawString(font, _game._sb, new Vector2(120, 54), GetSeasonColor(_game._ecosystem.Climate.CurrentSeason));
        
        _game._sb.Clear();
        _game._sb.Append($"Seed: {_game._ecosystem.Seed}");
        sb.DrawString(font, _game._sb, new Vector2(10, 76), UiTheme.WarmParchment);
    }

    private static Color GetPhaseColor(DayPhase phase) => phase switch
    {
        DayPhase.Dawn => new Color(255, 180, 100),
        DayPhase.Day => Color.White,
        DayPhase.Dusk => new Color(255, 140, 80),
        DayPhase.Night => new Color(120, 140, 220),
        _ => Color.White
    };

    private static readonly Dictionary<Season, Color> _seasonColors = new()
    {
        { Season.Spring, new Color(140, 220, 100) },
        { Season.Summer, new Color(255, 200, 60) },
        { Season.Autumn, new Color(220, 140, 40) },
        { Season.Winter, new Color(160, 200, 240) }
    };

    private static Color GetSeasonColor(Season season) =>
        _seasonColors.TryGetValue(season, out var color) ? color : Color.White;

    private void DrawDebugOverlay(SpriteBatch sb, SpriteFont font)
    {
        var m = _game._ecosystem.Metrics;
        m.FPS = _game._currentFPS;
        var y = _game.GraphicsDevice?.Viewport.Height ?? 0 - 100;
        var x = 8;
        var lineH = 14;

        if (_game._ecosystem.Disease.HasOutbreak)
        {
            _game._sb.Clear();
            _game._sb.Append($"Disease: {_game._ecosystem.Disease.ActiveDiseaseName}");
            sb.DrawString(font, _game._sb, new Vector2(x, y), new Color(220, 60, 60));
            y += lineH;
        }
        if (_game._ecosystem.Cataclysms.IsActive)
        {
            _game._sb.Clear();
            _game._sb.Append($"{_game._ecosystem.Cataclysms.ActiveEvent} ({_game._ecosystem.Cataclysms.Timer:F0}s)");
            sb.DrawString(font, _game._sb, new Vector2(x, y), new Color(255, 140, 0));
            y += lineH;
        }

        if (!_game._showDebugOverlay) return;

        _game._sb.Clear();
        _game._sb.Append($"FPS:{_game._currentFPS:F0} B:{m.TotalBirths} D:{m.TotalDeaths} Starve:{m.StarvationDeaths} Old:{m.OldAgeDeaths} Pred:{m.PredationDeaths} Comb:{m.CombatDeaths}");
        sb.DrawString(font, _game._sb, new Vector2(x, y), UiTheme.MutedStone);
        y += lineH;
        
        _game._sb.Clear();
        _game._sb.Append($"Sp:{m.SpeciesCount} Het:{m.MeanHeterozygosity:F2} Inb:{m.MeanInbreeding:F2} Sub:{m.TotalSubspecies} Trophic:L1={m.TrophicLevel1}/L2={m.TrophicLevel2}/L3+={m.TrophicLevel3Plus}");
        sb.DrawString(font, _game._sb, new Vector2(x, y), UiTheme.MutedStone);
        y += lineH;
        
        _game._sb.Clear();
        _game._sb.Append($"O2:{_game._ecosystem.Atmosphere.Oxygen:F0}% CO2:{_game._ecosystem.Atmosphere.CO2:F0}%");
        sb.DrawString(font, _game._sb, new Vector2(x, y), UiTheme.MutedStone);
        if (m.LastDeathSpecies.Length > 0)
        {
            y += lineH;
            _game._sb.Clear();
            _game._sb.Append($"{m.LastDeathSpecies}: {m.LastDeathCause}");
            sb.DrawString(font, _game._sb, new Vector2(x, y), new Color(180, 120, 100));
        }
    }

    private void DrawLoadingScreen(SpriteBatch sb)
    {
        var vw = _game.GraphicsDevice?.Viewport.Width ?? 0;
        var vh = _game.GraphicsDevice?.Viewport.Height ?? 0;
        _game.GraphicsDevice?.Clear(new Color(11, 23, 18));

        var text = "LOADING...";
        var size = _game._font.MeasureString(text);
        sb.DrawString(_game._font, text, new Vector2((vw - size.X) / 2, vh / 2 - 40), UiTheme.MossSignal);

        var barW = 300;
        var barH = 16;
        var barX = (vw - barW) / 2;
        var barY = vh / 2;
        UiPrimitives.Fill(sb, _game._uiPixel, new Rectangle(barX, barY, barW, barH), new Color(20, 40, 30));
        var progress = 1f - (_game._showLoadingTimer / 1.5f);
        var fillW = (int)(barW * progress);
        UiPrimitives.Fill(sb, _game._uiPixel, new Rectangle(barX + 2, barY + 2, fillW - 4, barH - 4), UiTheme.MossSignal);
        UiPrimitives.Border(sb, _game._uiPixel, new Rectangle(barX, barY, barW, barH), 2, UiTheme.BarkEdge);
    }
}
