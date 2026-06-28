using System;
using Microsoft.Xna.Framework;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife;

public sealed class SimulationController
{
    private readonly Ecosystem _ecosystem;
    private readonly DayNightCycle _dayNight;

    private float _timeAccumulator;
    private bool _paused;
    private int _speedLevel = 1;

    public SimulationController(Ecosystem ecosystem, DayNightCycle dayNight)
    {
        _ecosystem = ecosystem;
        _dayNight = dayNight;
    }

    public bool IsPaused => _paused;
    public int SpeedLevel => _speedLevel;
    public float CurrentSpeed => Core.SimulationConfig.Data.SpeedLevels[_speedLevel];
    public float TotalTime => _ecosystem.TotalTime;
    public int PlantCount => _ecosystem.PlantCount;
    public int HerbivoreCount => _ecosystem.HerbivoreCount;
    public int CarnivoreCount => _ecosystem.CarnivoreCount;
    public int OmnivoreCount => _ecosystem.OmnivoreCount;

    public void Advance(float dt)
    {
        if (_paused)
        {
            _dayNight.Update(_ecosystem.TotalTime);
            _ecosystem.CurrentDayPhase = _dayNight.Phase;
            return;
        }

        _ecosystem.SimulationSpeed = CurrentSpeed;
        _timeAccumulator += dt;
        var tickInterval = Core.SimulationConfig.Data.TickInterval;
        while (_timeAccumulator >= tickInterval)
        {
            _ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(tickInterval), TimeSpan.FromSeconds(tickInterval)));
            _timeAccumulator -= tickInterval;
        }
        _dayNight.Update(_ecosystem.TotalTime);
        _ecosystem.CurrentDayPhase = _dayNight.Phase;
    }

    public void SetSpeed(int level)
    {
        if (level < 0 || level >= Core.SimulationConfig.Data.SpeedLevels.Length) return;
        _speedLevel = level;
        _paused = level == 0;
    }

    public void TogglePause() => SetPause(!_paused);

    public void SetPause(bool paused)
    {
        _paused = paused;
        _speedLevel = paused ? 0 : Math.Max(1, _speedLevel);
    }
}
