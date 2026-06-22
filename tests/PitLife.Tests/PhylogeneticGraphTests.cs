using System;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class PhylogeneticGraphTests
{
    [Fact]
    public void Register_AddsNode()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Wolf" });
        Assert.NotNull(graph.GetNode("Wolf"));
    }

    [Fact]
    public void Register_DuplicateThrows()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Wolf" });
        Assert.Throws<ArgumentException>(() =>
            graph.Register(new PhylogeneticNode { Species = "Wolf" }));
    }

    [Fact]
    public void Register_CycleThrows()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Fox", Parent = "Wolf" });
        Assert.Throws<ArgumentException>(() =>
            graph.Register(new PhylogeneticNode { Species = "Wolf", Parent = "Fox" }));
    }

    [Fact]
    public void CanEvolveFrom_InvalidParentReturnsFalse()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Cheetah", Parent = "Lion" });

        var genome = new Genome { Speed = 1.5f, Size = 1f };
        Assert.False(graph.CanEvolveFrom("Fox", "Cheetah", genome));
    }

    [Fact]
    public void CanEvolveFrom_ValidParentAndThresholdsReturnsTrue()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode
        {
            Species = "Cheetah",
            Parent = "Fox",
            MinimumGenomeThresholds = [new() { Trait = GenomeTrait.Speed, MinimumValue = 1.4f }]
        });

        var genome = new Genome { Speed = 1.5f, Size = 1f };
        Assert.True(graph.CanEvolveFrom("Fox", "Cheetah", genome));
    }

    [Fact]
    public void CanEvolveFrom_BelowThresholdReturnsFalse()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode
        {
            Species = "Cheetah",
            Parent = "Fox",
            MinimumGenomeThresholds = [new() { Trait = GenomeTrait.Speed, MinimumValue = 1.4f }]
        });

        var genome = new Genome { Speed = 1.2f, Size = 1f };
        Assert.False(graph.CanEvolveFrom("Fox", "Cheetah", genome));
    }

    [Fact]
    public void CanEvolveFrom_UnregisteredSpeciesReturnsTrue()
    {
        var graph = new PhylogeneticGraph();
        var genome = new Genome();
        Assert.True(graph.CanEvolveFrom("Fox", "Cheetah", genome));
    }

    [Fact]
    public void GetAncestry_ReturnsFullChain()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Fox" });
        graph.Register(new PhylogeneticNode { Species = "Wolf", Parent = "Fox" });
        graph.Register(new PhylogeneticNode { Species = "Dog", Parent = "Wolf" });

        var chain = graph.GetAncestry("Dog");
        Assert.Equal(3, chain.Count);
        Assert.Equal("Dog", chain[0]);
        Assert.Equal("Wolf", chain[1]);
        Assert.Equal("Fox", chain[2]);
    }

    [Fact]
    public void Validate_UnknownParentReturnsError()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Wolf", Parent = "Unknown" });

        var errors = graph.Validate();
        Assert.Single(errors);
        Assert.Contains("Unknown", errors[0]);
    }

    [Fact]
    public void Validate_ValidGraphReturnsNoErrors()
    {
        var graph = new PhylogeneticGraph();
        graph.Register(new PhylogeneticNode { Species = "Fox" });
        graph.Register(new PhylogeneticNode { Species = "Wolf", Parent = "Fox" });

        var errors = graph.Validate();
        Assert.Empty(errors);
    }
}
