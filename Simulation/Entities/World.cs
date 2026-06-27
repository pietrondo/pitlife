using System;

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

    private World(int width, int height, int seed, WorldGenOptions? options)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        ContinentMask = new float[width * height];
        ElevationField = new float[width * height];
        RiverMask = new bool[width * height];
        if (options is null)
            new WorldGenerator(this, seed).Generate();
        else
            new WorldGenerator(this, seed).Generate(options);
    }

    public World(int width, int height, int seed)
        : this(width, height, seed, (WorldGenOptions?)null)
    {
    }

    public World(WorldGenOptions options, int seed)
        : this(options.MapWidth, options.MapHeight, seed, options)
    {
    }

    public Tile GetTile(int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Tiles[x, y];
    }

    public Tile GetTileAtPosition(float worldX, float worldY)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        return GetTile(tx, ty);
    }

    public void RegenerateGrass(float dt)
    {
        for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                Tiles[x, y].RegenerateGrass(dt);
    }

    public void ProcessRecovery(float dt)
    {
        for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
            {
                Tile tile = Tiles[x, y];
                tile.RecoverFromCataclysm(dt);

                if (tile.GrassAmount < tile.MaxGrass * 0.3f && tile.OriginalBiome == null)
                    continue;

                SpreadGrassToNeighbors(x, y, dt);
            }
    }

    private void SpreadGrassToNeighbors(int x, int y, float dt)
    {
        Tile tile = Tiles[x, y];
        if (tile.GrassAmount < tile.MaxGrass * 0.5f) return;

        var spread = 0.003f * tile.SoilNutrients * dt;
        TrySpread(tile, x - 1, y, spread);
        TrySpread(tile, x + 1, y, spread);
        TrySpread(tile, x, y - 1, spread);
        TrySpread(tile, x, y + 1, spread);
    }

    private void TrySpread(Tile from, int nx, int ny, float amount)
    {
        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) return;
        Tile neighbor = Tiles[nx, ny];
        if (neighbor.Biome == BiomeType.DeepOcean || neighbor.Biome == BiomeType.ShallowWater) return;
        if (neighbor.GrassAmount < neighbor.MaxGrass)
        {
            neighbor.GrassAmount = Math.Min(neighbor.MaxGrass, neighbor.GrassAmount + amount);
        }
    }

    public bool IsRiverAt(float worldX, float worldY)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;
        return RiverMask[ty * Width + tx];
    }

    public float GetElevation(float worldX, float worldY)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return 0f;
        return ElevationField[ty * Width + tx];
    }
}
