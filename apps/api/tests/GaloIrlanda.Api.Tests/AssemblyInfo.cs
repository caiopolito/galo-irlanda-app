using GaloIrlanda.Api.Tests.Support;

// Um Postgres (Testcontainers) para toda a suíte; testes em série porque compartilham o banco.
[assembly: AssemblyFixture(typeof(ApiFactory))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
