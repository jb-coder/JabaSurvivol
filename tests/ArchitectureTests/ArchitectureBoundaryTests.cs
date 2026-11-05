using JabaSurvivol.Core;

namespace JabaSurvivol.ArchitectureTests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void GameCore_does_not_reference_unity_engine()
    {
        var references = typeof(CoreAssembly).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty);

        Assert.DoesNotContain(
            references,
            name => name.StartsWith("UnityEngine", StringComparison.Ordinal));
    }
}
