using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;

namespace PitLife.Core;

public class SimulationOrchestrator
{
    private readonly Game1 _game;

    public SimulationOrchestrator(Game1 game)
    {
        _game = game;
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists("settings.json"))
            {
                var json = File.ReadAllText("settings.json");
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var lang))
                    I18n.SetLanguage(lang.GetString() ?? "it");
            }
        }
        catch (Exception ex) { Logger.Error($"Failed to load settings: {ex.Message}"); }
    }

    public void SaveSettings()
    {
        try
        {
            var settings = new { language = I18n.CurrentLanguage, fullscreen = _game._graphics.IsFullScreen };
            File.WriteAllText("settings.json", JsonSerializer.Serialize(settings));
        }
        catch (Exception ex) { Logger.Error($"Failed to save settings: {ex.Message}"); }
    }

    public void GenerateNewWorld(int? seedOverride, WorldGenOptions? worldGenOptions = null)
    {
        var seed = seedOverride ?? new Random().Next();
        var wgOpts = worldGenOptions ?? WorldGenOptions.Pangea() with { MapWidth = 400, MapHeight = 300 };
        _game._ecosystem = new Ecosystem(wgOpts, seed);
        _game._ecosystem.Climate.Configure(wgOpts.PlanetRadiusKm, wgOpts.OrbitalAU, wgOpts.Eccentricity);
        _game._ecosystem.Initialize(60, 20, 15, 150);
        _game._worldRenderer?.Dispose();
        _game._creatureRenderer?.Dispose();
        _game._worldRenderer = new PixelWorldRenderer(_game._ecosystem.World);
        _game._creatureRenderer = new CreatureRenderer(_game._ecosystem);
        _game._minimap = new Minimap(_game._ecosystem, _game._camera);
        _game._controller = new SimulationController(_game._ecosystem, _game._dayNight);
        _game._waterEffect = new WaterEffect();
        ResetWorldSessionState();

        _game._worldRenderer.LoadContent(_game.GraphicsDevice);
        _game._creatureRenderer.LoadContent(_game.GraphicsDevice);
        _game._minimap.LoadContent(_game.GraphicsDevice);
        _game._creatureRenderer.LoadFromRegistry(_game.GraphicsDevice, AssetRegistry.Fallbacks);
        _game._creatureRenderer.LoadFromRegistry(_game.GraphicsDevice, AssetRegistry.SpeciesTextures);
        _game._creatureRenderer.LoadGenderedFromRegistry(_game.GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);
    }

    public void RestoreLoadedEcosystem(SaveSystem.SaveData data)
    {
        _game._ecosystem = new Ecosystem(data.WorldWidth, data.WorldHeight, data.Seed);
        _game._ecosystem.TotalTime = data.TotalTime;

        foreach (var cData in data.Creatures)
        {
            var def = SpeciesRegistry.Get(cData.Species);
            if (def == null) continue;

            var genome = new Genome
            {
                Speed = cData.Genome.Speed,
                Size = cData.Genome.Size,
                Metabolism = cData.Genome.Metabolism,
                VisionRange = cData.Genome.VisionRange,
                Color = new Color(cData.Genome.ColorR, cData.Genome.ColorG, cData.Genome.ColorB),
                MutationRate = cData.Genome.MutationRate,
                DesertAdaptation = cData.Genome.DesertAdaptation,
                ColdAdaptation = cData.Genome.ColdAdaptation,
                ForestAdaptation = cData.Genome.ForestAdaptation,
                WaterAdaptation = cData.Genome.WaterAdaptation,
                Genetics = cData.Genome.Genetics ?? default
            };

            Creature c = (Creature)Activator.CreateInstance(
                def.CreatureType,
                new Vector2(cData.PositionX, cData.PositionY),
                genome,
                def.Species)!;

            c.Energy = cData.Energy;
            c.Gender = cData.Gender;
            c.Facing = new Vector2(cData.FacingX, cData.FacingY);
            c.GrowFor(cData.Age);
            c.RestoreGeneticHistory(
                LineageRecord.Restore(
                    cData.IndividualId,
                    cData.ParentAId,
                    cData.ParentBId,
                    cData.AncestorDepths),
                cData.InbreedingCoefficient);

            _game._ecosystem.AddCreature(c);
        }

        _game._ecosystem.FlushPending();
        _game._ecosystem.UpdateStats();

        _game._worldRenderer?.Dispose();
        _game._creatureRenderer?.Dispose();
        _game._worldRenderer = new PixelWorldRenderer(_game._ecosystem.World);
        _game._creatureRenderer = new CreatureRenderer(_game._ecosystem);
        _game._minimap = new Minimap(_game._ecosystem, _game._camera);
        _game._controller = new SimulationController(_game._ecosystem, _game._dayNight);
        _game._waterEffect = new WaterEffect();
        ResetWorldSessionState();

        _game._worldRenderer.LoadContent(_game.GraphicsDevice);
        _game._creatureRenderer.LoadContent(_game.GraphicsDevice);
        _game._minimap.LoadContent(_game.GraphicsDevice);
        _game._creatureRenderer.LoadFromRegistry(_game.GraphicsDevice, AssetRegistry.Fallbacks);
        _game._creatureRenderer.LoadFromRegistry(_game.GraphicsDevice, AssetRegistry.SpeciesTextures);
        _game._creatureRenderer.LoadGenderedFromRegistry(_game.GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);
    }

    public void ResetWorldSessionState()
    {
        _game._camera.WorldWidth = _game._ecosystem.World.PixelWidth;
        _game._camera.WorldHeight = _game._ecosystem.World.PixelHeight;
        _game._camera.Zoom = 1f;
        _game._camera.Position = new Vector2(
            _game._ecosystem.World.PixelWidth / 2f,
            _game._ecosystem.World.PixelHeight / 2f);
        _game._selectedCreature = null;
        _game._spawnPanel.Close();
        _game._inGameUi.ResetForWorld(_game._ecosystem.World);
        _game._prevSpawnSpecies = null;
        _game._displayPlants = _game._ecosystem.PlantCount;
        _game._displayHerbivores = _game._ecosystem.HerbivoreCount;
        _game._displayCarnivores = _game._ecosystem.CarnivoreCount;
        _game._displayOmnivores = _game._ecosystem.OmnivoreCount;
        _game._displayTime = _game._ecosystem.TotalTime;
        _game._dayNight.Update(_game._ecosystem.TotalTime);
    }
}
