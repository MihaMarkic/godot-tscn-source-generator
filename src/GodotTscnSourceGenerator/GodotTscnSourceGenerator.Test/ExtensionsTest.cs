using NUnit.Framework;

namespace GodotTscnSourceGenerator.Test;

public class ExtensionsTest
{
    [TestFixture]
    public class ToPascalCase: ExtensionsTest
    {
        [TestCase("sprite_frames", ExpectedResult = "SpriteFrames")]
        public string? GivenSample_FormatsCorrectly(string input)
        {
            return input.ToPascalCase();
        }
    }
}
