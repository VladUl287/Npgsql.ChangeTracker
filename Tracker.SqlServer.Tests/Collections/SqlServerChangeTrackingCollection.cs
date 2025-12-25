namespace Tracker.SqlServer.Tests.Collections;

[CollectionDefinition("SqlServerChangeTrackingCollection", DisableParallelization = true)]
public class SqlServerChangeTrackingCollection : ICollectionFixture<object>
{
}

