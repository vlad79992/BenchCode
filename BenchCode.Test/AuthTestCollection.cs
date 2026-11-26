namespace BenchCode.Test;

[CollectionDefinition("AuthTests", DisableParallelization = false)]
public sealed class AuthTestCollection : ICollectionFixture<AuthFixture>
{
}