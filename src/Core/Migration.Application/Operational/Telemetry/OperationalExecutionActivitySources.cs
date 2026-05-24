using System.Diagnostics;

namespace Migration.Application.Operational.Telemetry;

public static class OperationalExecutionActivitySources
{
    public const string Name = "Migration.Operational.Execution";
    public const string Version = "8.3.0";

    public const string SqlQueueWorkItemExecution = "migration.operational.sql_queue.work_item.execute";
    public const string ServiceBusWorkItemExecution = "migration.operational.servicebus.work_item.execute";
    public const string ServiceBusDispatch = "migration.operational.servicebus.dispatch";
    public const string OperationalRunEvaluation = "migration.operational.run.evaluate";

    public static readonly ActivitySource Source = new(Name, Version);
}
