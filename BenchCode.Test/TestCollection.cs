namespace BenchCode.Test;

[CollectionDefinition("AuthTests", DisableParallelization = false)]
public sealed class TestCollection : ICollectionFixture<Fixture>
{
}