using System;
using System.Collections.Generic;

namespace PitLife.Simulation;

public sealed class LineageRecord
{
    public const int MaximumTrackedGenerations = 6;

    private readonly Dictionary<ulong, byte> _ancestorDepths;

    public ulong IndividualId { get; }
    public ulong ParentAId { get; }
    public ulong ParentBId { get; }
    public IReadOnlyDictionary<ulong, byte> AncestorDepths => _ancestorDepths;

    private LineageRecord(
        ulong individualId,
        ulong parentAId,
        ulong parentBId,
        Dictionary<ulong, byte> ancestorDepths)
    {
        IndividualId = individualId;
        ParentAId = parentAId;
        ParentBId = parentBId;
        _ancestorDepths = ancestorDepths;
    }

    public static LineageRecord Founder(ulong individualId = 0) =>
        new(individualId, 0, 0, new Dictionary<ulong, byte>());

    public LineageRecord WithIndividualId(ulong individualId) =>
        new(individualId, ParentAId, ParentBId, new Dictionary<ulong, byte>(_ancestorDepths));

    public static LineageRecord CreateChild(LineageRecord firstParent, LineageRecord secondParent)
    {
        var ancestors = new Dictionary<ulong, byte>();
        AddParentAndAncestors(firstParent, ancestors);
        AddParentAndAncestors(secondParent, ancestors);
        return new LineageRecord(0, firstParent.IndividualId, secondParent.IndividualId, ancestors);
    }

    public static LineageRecord CreateAsexualChild(LineageRecord parent)
    {
        var ancestors = new Dictionary<ulong, byte>();
        AddParentAndAncestors(parent, ancestors);
        return new LineageRecord(0, parent.IndividualId, 0, ancestors);
    }

    public static float CalculateOffspringInbreeding(LineageRecord firstParent, LineageRecord secondParent) =>
        CalculateRelatedness(firstParent, secondParent);

    public static float CalculateRelatedness(LineageRecord first, LineageRecord second)
    {
        Dictionary<ulong, byte> firstAncestors = WithSelf(first);
        Dictionary<ulong, byte> secondAncestors = WithSelf(second);
        double coefficient = 0;
        foreach ((var ancestor, var firstDepth) in firstAncestors)
        {
            if (ancestor == 0 || !secondAncestors.TryGetValue(ancestor, out var secondDepth))
                continue;
            coefficient += Math.Pow(0.5, firstDepth + secondDepth + 1);
        }
        return (float)Math.Clamp(coefficient, 0, 1);
    }

    public static LineageRecord Restore(
        ulong individualId,
        ulong parentAId,
        ulong parentBId,
        IEnumerable<KeyValuePair<ulong, byte>> ancestors)
    {
        var depths = new Dictionary<ulong, byte>();
        foreach ((var ancestor, var depth) in ancestors)
        {
            if (ancestor != 0 && depth <= MaximumTrackedGenerations)
                AddMinimumDepth(depths, ancestor, depth);
        }
        return new LineageRecord(individualId, parentAId, parentBId, depths);
    }

    private static void AddParentAndAncestors(
        LineageRecord parent,
        Dictionary<ulong, byte> destination)
    {
        if (parent.IndividualId != 0)
            AddMinimumDepth(destination, parent.IndividualId, 1);
        foreach ((var ancestor, var depth) in parent._ancestorDepths)
        {
            var childDepth = depth + 1;
            if (childDepth <= MaximumTrackedGenerations)
                AddMinimumDepth(destination, ancestor, (byte)childDepth);
        }
    }

    private static Dictionary<ulong, byte> WithSelf(LineageRecord lineage)
    {
        var result = new Dictionary<ulong, byte>(lineage._ancestorDepths);
        if (lineage.IndividualId != 0)
            result[lineage.IndividualId] = 0;
        return result;
    }

    private static void AddMinimumDepth(Dictionary<ulong, byte> depths, ulong id, byte depth)
    {
        if (!depths.TryGetValue(id, out var existing) || depth < existing)
            depths[id] = depth;
    }
}
