namespace JabaSurvivol.Core.Tests;

public sealed class CoreAssemblyTests
{
    [Fact]
    public void Name_is_stable()
    {
        Assert.Equal("JabaSurvivol.Core", CoreAssembly.Name);
    }
}
