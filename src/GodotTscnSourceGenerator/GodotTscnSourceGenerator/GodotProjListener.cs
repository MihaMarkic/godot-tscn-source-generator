using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using GodotTscnSourceGenerator.Models;
using Righthand.GodotTscnParser.Engine.Grammar;
using System.Linq;

namespace GodotTscnSourceGenerator
{
    public class GodotProjListener : GodotProjBaseListener
    {
        public HashSet<InputAction> InputActions { get; } = new();

        public override void ExitSection([NotNull] GodotProjParser.SectionContext context)
        {
            if (string.Equals(context.sectionName().GetText(), "input", System.StringComparison.OrdinalIgnoreCase))
            {
                var lines = context.sectionLine();
                var query = from l in lines
                            let cp = l.complexPair()
                            where cp is not null
                            select cp.complexPairName().GetText();
                foreach (var name in query)
                {
                    InputActions.Add(new InputAction(name));
                }
            }
            base.ExitSection(context);
        }
    }
}
