namespace TeensyRom.Cli.Tests.Integration;

/// <summary>
/// Every scenario shares the one API and the static <c>AnsiConsole.Console</c>, so they run one at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RealApiCollection : ICollectionFixture<SharedApiFixture>
{
    public const string Name = "RealApi";
}
