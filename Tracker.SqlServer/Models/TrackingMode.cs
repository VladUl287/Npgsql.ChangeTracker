namespace Tracker.SqlServer.Models;

public enum TrackingMode
{
    /// <summary>
    /// <c>sys.dm_db_index_usage_stats</c> returns counts of different types of index operations and the time each type of operation was last performed.
    /// <a href="https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-db-index-usage-stats-transact-sql">Source ID Documentation</a>.
    /// </summary>
    DbIndexUsageStats,
    /// <summary>
    /// Change Tracking lightweight solution that provides an efficient change tracking mechanism.
    /// <a href="https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-tracking-sql-server">Source ID Documentation</a>.
    /// </summary>
    ChangeTracking
}