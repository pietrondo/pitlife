using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class LineageTests
{
    [Fact]
    public void Founder_HasNoParents()
    {
        var f = LineageRecord.Founder(42);
        Assert.Equal(42ul, f.IndividualId);
        Assert.Equal(0ul, f.ParentAId);
        Assert.Equal(0ul, f.ParentBId);
        Assert.Empty(f.AncestorDepths);
    }

    [Fact]
    public void CreateAsexualChild_InheritsParentAncestors()
    {
        var parent = LineageRecord.Founder(1);
        var child = LineageRecord.CreateAsexualChild(parent);
        Assert.Equal(1ul, child.ParentAId);
        Assert.Equal(0ul, child.ParentBId);
        Assert.True(child.AncestorDepths.ContainsKey(1));
        Assert.Equal(1, child.AncestorDepths[1]);
    }

    [Fact]
    public void CreateChild_CombinesBothParents()
    {
        var a = LineageRecord.Founder(10);
        var b = LineageRecord.Founder(20);
        var child = LineageRecord.CreateChild(a, b);
        Assert.Equal(10ul, child.ParentAId);
        Assert.Equal(20ul, child.ParentBId);
        Assert.True(child.AncestorDepths.ContainsKey(10));
        Assert.True(child.AncestorDepths.ContainsKey(20));
    }

    [Fact]
    public void CalculateRelatedness_Siblings_ReturnsPositive()
    {
        var parent = LineageRecord.Founder(1);
        var child1 = LineageRecord.CreateAsexualChild(parent).WithIndividualId(2);
        var child2 = LineageRecord.CreateAsexualChild(parent).WithIndividualId(3);
        float rel = LineageRecord.CalculateRelatedness(child1, child2);
        Assert.True(rel > 0f);
        Assert.True(rel <= 1f);
    }

    [Fact]
    public void CalculateRelatedness_Unrelated_ReturnsZero()
    {
        var a = LineageRecord.Founder(1).WithIndividualId(1);
        var b = LineageRecord.Founder(2).WithIndividualId(2);
        float rel = LineageRecord.CalculateRelatedness(a, b);
        Assert.Equal(0f, rel);
    }

    [Fact]
    public void WithIndividualId_PreservesAncestors()
    {
        var parent = LineageRecord.Founder(5);
        var child = LineageRecord.CreateAsexualChild(parent);
        var renamed = child.WithIndividualId(99);
        Assert.Equal(99ul, renamed.IndividualId);
        Assert.True(renamed.AncestorDepths.ContainsKey(5));
    }

    [Fact]
    public void Restore_RebuildsLineage()
    {
        var ancestors = new System.Collections.Generic.Dictionary<ulong, byte>
        {
            [10] = 1, [20] = 2
        };
        var restored = LineageRecord.Restore(1, 10, 20, ancestors);
        Assert.Equal(1ul, restored.IndividualId);
        Assert.Equal(10ul, restored.ParentAId);
        Assert.True(restored.AncestorDepths.ContainsKey(10));
    }

    [Fact]
    public void CalculateOffspringInbreeding_ReturnsBetweenZeroAndOne()
    {
        var parent = LineageRecord.Founder(1);
        var sib1 = LineageRecord.CreateAsexualChild(parent).WithIndividualId(2);
        var sib2 = LineageRecord.CreateAsexualChild(parent).WithIndividualId(3);
        float inb = LineageRecord.CalculateOffspringInbreeding(sib1, sib2);
        Assert.True(inb >= 0f && inb <= 1f);
    }
}
