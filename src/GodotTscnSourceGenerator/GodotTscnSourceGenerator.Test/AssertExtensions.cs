using GodotTscnSourceGenerator.Models;
using NUnit.Framework.Constraints;
using System.Diagnostics.CodeAnalysis;

namespace GodotTscnSourceGenerator.Test;

internal static class AssertExtensions
{
    public static EqualConstraint UsingNodeComparer(this EqualConstraint constraint)
        => constraint.Using<Node?>(CompareNodes);
    public static SomeItemsConstraint UsingNodeComparer(this SomeItemsConstraint constraint)
        => constraint.Using<Node?>(NodeComparer.Default);
    static bool CompareNodes(Node? node, Node? other)
    {
        if (node is null && other is null || node is null ^ other is null)
        {
            return false;
        }
        if (node!.GetType() != other!.GetType())
        {
            return false;
        }
        return node.Name == other.Name && node.Type == other.Type;
    }

    internal class NodeComparer : IEqualityComparer<Node?>
    {
        internal static NodeComparer Default = new NodeComparer();
        public bool Equals(Node? x, Node? y) => CompareNodes(x, y);

        public int GetHashCode([DisallowNull] Node? obj) => HashCode.Combine(obj.Name, obj.Type);
    }
}
