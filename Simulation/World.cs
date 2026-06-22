using System;
using System.Collections.Generic;
using System.Numerics;

namespace PitLife.Simulation;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; } = 32;
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;
    public Tile[,] Tiles { get; }
    public float[] ContinentMask { get; }
    public float[] ElevationField { get; }
    public bool[] RiverMask { get; }

    public World(int width, int height, int seed)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        ContinentMask = new float[width * height];
        ElevationField = new float[width * height];
        RiverMask = new bool[width * height];
        new WorldGenerator(this, seed).Generate();
    }

    public Tile GetTile(int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Tiles[x, y];
    }

    public Tile GetTileAtPosition(float worldX, float worldY)
    {
        int tx = (int)(worldX / TileSize);
        int ty = (int)(worldY / TileSize);
        return GetTile(tx, ty);
    }

    public void RegenerateGrass(float dt)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Tiles[x, y].RegenerateGrass(dt);
    }

    public bool IsRiverAt(float worldX, float worldY)
    {
        int tx = (int)(worldX / TileSize);
        int ty = (int)(worldY / TileSize);
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;
        return RiverMask[ty * Width + tx];
    }
}
