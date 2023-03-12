namespace GodotTscnSourceGenerator.Models
{
    public class ExtResource
    {
        public string Uid { get; }
        public string Id { get; }
        public string Type { get; }
        public string Path { get; }
        public ExtResource(string uid, string id, string type, string path)
        {
            Uid = uid;
            Id = id;
            Type = type;
            Path = path;
        }
    }
}
