using Microsoft.CodeAnalysis;

namespace GodotTscnSourceGenerator
{
    [Generator]
    public class TscnTypesGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("Player.g.cs", "// YEA HELLO");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
