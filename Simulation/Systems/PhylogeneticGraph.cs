using System;
using System.Collections.Generic;
using System.Linq;

namespace PitLife.Simulation;

public sealed class PhylogeneticGraph
{
    private readonly Dictionary<string, PhylogeneticNode> _nodes = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, PhylogeneticNode> Nodes => _nodes;

    public void Register(PhylogeneticNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Species))
            throw new ArgumentException("Species name is required.");
        if (_nodes.ContainsKey(node.Species))
            throw new ArgumentException($"Species '{node.Species}' already registered.");

        foreach (var ancestor in GetAncestors(node.Parent))
        {
            if (ancestor == node.Species)
                throw new ArgumentException($"Adding '{node.Species}' would create a cycle via parent '{node.Parent}'.");
        }

        _nodes[node.Species] = node;
    }

    public bool CanEvolveFrom(string fromSpecies, string toSpecies, Genome genome)
    {
        if (!_nodes.TryGetValue(toSpecies, out var node))
            return true;

        if (node.Parent != null && node.Parent != fromSpecies)
            return false;

        if (node.MinimumGenomeThresholds != null)
        {
            foreach (var req in node.MinimumGenomeThresholds)
            {
                float value = req.Trait switch
                {
                    GenomeTrait.Speed => genome.Speed,
                    GenomeTrait.Size => genome.Size,
                    GenomeTrait.Metabolism => genome.Metabolism,
                    GenomeTrait.VisionRange => genome.VisionRange,
                    GenomeTrait.DesertAdaptation => genome.DesertAdaptation,
                    GenomeTrait.ColdAdaptation => genome.ColdAdaptation,
                    GenomeTrait.ForestAdaptation => genome.ForestAdaptation,
                    GenomeTrait.WaterAdaptation => genome.WaterAdaptation,
                    _ => 0f
                };
                if (value < req.MinimumValue)
                    return false;
            }
        }

        return true;
    }

    public PhylogeneticNode? GetNode(string species)
    {
        _nodes.TryGetValue(species, out var node);
        return node;
    }

    public List<string> GetAncestry(string species)
    {
        var chain = new List<string> { species };
        var visited = new HashSet<string>(StringComparer.Ordinal) { species };
        var current = species;

        while (_nodes.TryGetValue(current, out var node) && node.Parent != null)
        {
            if (!visited.Add(node.Parent)) break;
            chain.Add(node.Parent);
            current = node.Parent;
        }

        return chain;
    }

    private HashSet<string> GetAncestors(string? start)
    {
        var ancestors = new HashSet<string>(StringComparer.Ordinal);
        var current = start;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (current != null && visited.Add(current) && _nodes.TryGetValue(current, out var node))
        {
            if (node.Parent != null)
            {
                ancestors.Add(node.Parent);
                current = node.Parent;
            }
            else
            {
                break;
            }
        }

        return ancestors;
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        foreach (var (species, node) in _nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Species))
                errors.Add($"Node has empty species name.");

            if (node.Parent != null && !_nodes.ContainsKey(node.Parent))
                errors.Add($"Species '{species}' references unknown parent '{node.Parent}'.");
        }

        return errors;
    }
}

public sealed class PhylogeneticNode
{
    public string Species { get; set; } = "";
    public string? Parent { get; set; }
    public List<GenomeRequirement>? MinimumGenomeThresholds { get; set; }
    public float DivergenceTime { get; set; }
    public string? AncestorDescription { get; set; }
}

public sealed class GenomeRequirement
{
    public GenomeTrait Trait { get; set; }
    public float MinimumValue { get; set; }
}

public enum GenomeTrait
{
    Speed,
    Size,
    Metabolism,
    VisionRange,
    DesertAdaptation,
    ColdAdaptation,
    ForestAdaptation,
    WaterAdaptation
}
