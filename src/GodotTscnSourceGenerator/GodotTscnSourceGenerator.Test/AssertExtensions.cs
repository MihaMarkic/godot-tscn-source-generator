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
    public static EqualConstraint UsingScriptComparer(this EqualConstraint constraint)
    => constraint.Using<Script?>(CompareScripts);
    public static EqualConstraint UsingSubResourceComparer(this EqualConstraint constraint)
=> constraint.Using<SubResource?>(CompareSubResources);
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
    static bool CompareSubResources(SubResource? x, SubResource? y)
    {
        if (x is null && y is null || x is null ^ y is null)
        {
            return false;
        }
        if (x!.GetType() != y!.GetType())
        {
            return false;
        }
        return x.Id == y.Id && x.Type == y.Type && CompareArrays(x.Animations, y.Animations, AnimationComparer.Default);
    }
    static bool CompareAnimations(Animation? x, Animation? y)
    {
        if (x is null && y is null || x is null ^ y is null)
        {
            return false;
        }
        if (x!.GetType() != y!.GetType())
        {
            return false;
        }
        return x.Name == y.Name;
    }
    static bool CompareScripts(Script? a, Script? b)
    {
        if (a is null && b is null || a is null ^ b is null)
        {
            return false;
        }
        return a!.Path == b!.Path;
    }
    static bool CompareArrays<T>(IList<T> x, IList<T> y, IEqualityComparer<T> comparer)
    {
        if (x.Count != y.Count)
        {
            return false;
        }
        for (int i = 0; i < x.Count; i++)
        {
            if (!comparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }
        return true;
    }

    internal class NodeComparer : IEqualityComparer<Node?>
    {
        internal static NodeComparer Default = new NodeComparer();
        public bool Equals(Node? x, Node? y) => CompareNodes(x, y);

        public int GetHashCode([DisallowNull] Node? obj) => HashCode.Combine(obj.Name, obj.Type);
    }
    internal class SubResourceComparer: IEqualityComparer<SubResource?>
    {
        internal static SubResourceComparer Default = new SubResourceComparer();

        public bool Equals(SubResource? x, SubResource? y) => CompareSubResources(x, y);

        public int GetHashCode([DisallowNull] SubResource? obj)
        {
            var hc = HashCode.Combine(obj.Id, obj.Type);
            foreach (var a in obj.Animations)
            {
                hc = HashCode.Combine(hc, a.GetHashCode());
            }
            return hc;
        }
    }
    internal class AnimationComparer: IEqualityComparer<Animation?> {
        internal static AnimationComparer Default = new AnimationComparer();

        public bool Equals(Animation? x, Animation? y) => CompareAnimations(x, y);

        public int GetHashCode([DisallowNull] Animation? obj) => obj.Name.GetHashCode();
    }
}
