using System.Collections.Immutable;

namespace GodotTscnSourceGenerator.Models
{
    public class SubResource
    {
        public string Id { get; }
        public string Type { get; }
        public ImmutableArray<Animation> Animations { get; }
        public SubResource(string id, string type, ImmutableArray<Animation> animations)
        {
            Id = id;
            Type = type;
            Animations = animations;
        }
    }
}
