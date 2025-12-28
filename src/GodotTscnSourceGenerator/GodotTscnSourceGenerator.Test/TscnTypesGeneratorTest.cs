using NUnit.Framework;

namespace GodotTscnSourceGenerator.Test;

public class TscnTypesGeneratorTest
{
    [TestFixture]
    public class FixTypeName: TscnTypesGeneratorTest
    {
        [TestCase("GPUParticles2D", ExpectedResult = "GpuParticles2D")]
        [TestCase("HUD", ExpectedResult = "HUD")]
        public string GivenInput_OutputsCorrect(string input)
        {
            return TscnTypesGenerator.FixTypeName(input);
        }
    }
}