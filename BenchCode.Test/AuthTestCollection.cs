namespace BenchCode.Test;

[CollectionDefinition("AuthTests", DisableParallelization = true)]
public sealed class AuthTestCollection : ICollectionFixture<AuthFixture> { }