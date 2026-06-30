using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Moq;
using PitLife;
using PitLife.Core;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;
using PitLife.Rendering;
using Xunit;

namespace PitLife.Tests;

public class SimulationOrchestratorTests : IDisposable
{
    private readonly string _settingsPath = "settings.json";
    private readonly string _backupPath = "settings.json.bak";
    private bool _hadBackup = false;

    public SimulationOrchestratorTests()
    {
        // Backup existing settings.json
        if (File.Exists(_settingsPath))
        {
            File.Copy(_settingsPath, _backupPath, true);
            _hadBackup = true;
            File.Delete(_settingsPath);
        }
    }

    public void Dispose()
    {
        // Clean up settings.json and restore backup
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }

        if (_hadBackup && File.Exists(_backupPath))
        {
            File.Copy(_backupPath, _settingsPath, true);
            File.Delete(_backupPath);
        }
    }

    private static void SetReadonlyField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field!.SetValue(target, value);
    }

    [Fact]
    public void LoadSettings_SetsLanguageFromSettingsJson()
    {
        var settings = new { language = "en" };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));

        using var game = new Game1();
        var orchestrator = new SimulationOrchestrator(game);

        I18n.SetLanguage("it");
        orchestrator.LoadSettings();

        Assert.Equal("en", I18n.CurrentLanguage);
    }

    [Fact]
    public void SaveSettings_WritesSettingsJson()
    {
        using var game = new Game1();
        var orchestrator = new SimulationOrchestrator(game);

        I18n.SetLanguage("en");
        orchestrator.SaveSettings();

        Assert.True(File.Exists(_settingsPath));
        var json = File.ReadAllText(_settingsPath);
        var doc = JsonDocument.Parse(json);
        Assert.Equal("en", doc.RootElement.GetProperty("language").GetString());
    }

    [Fact]
    public void ResetWorldSessionState_ConfiguresGameProperties()
    {
        using var game = new Game1();
        
        var mockEcosystem = new Mock<Ecosystem>(20, 20, 42) { CallBase = true };
        game._ecosystem = mockEcosystem.Object;

        var mockCamera = new Mock<Camera>(800, 600) { CallBase = true };
        game._camera = mockCamera.Object;

        var mockSpawnPanel = new Mock<SpawnPanel>() { CallBase = true };
        SetReadonlyField(game, "_spawnPanel", mockSpawnPanel.Object);

        var mockInGameUi = new Mock<InGameUi>() { CallBase = true };
        SetReadonlyField(game, "_inGameUi", mockInGameUi.Object);

        var mockDayNight = new Mock<DayNightCycle>() { CallBase = true };
        SetReadonlyField(game, "_dayNight", mockDayNight.Object);

        var orchestrator = new SimulationOrchestrator(game);
        orchestrator.ResetWorldSessionState();

        Assert.Equal(1f, game._camera.Zoom);
        Assert.Null(game._selectedCreature);
        Assert.False(game._spawnPanel.IsOpen);
        Assert.Equal(game._ecosystem.PlantCount, game._displayPlants);
    }
}

