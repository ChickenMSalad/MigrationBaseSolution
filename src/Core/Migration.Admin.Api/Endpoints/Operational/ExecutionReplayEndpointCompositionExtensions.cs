using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Endpoints.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational;

public static class ExecutionReplayEndpointCompositionExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapExecutionDiagnosticExportEndpoints();
        endpoints.MapExecutionReplayAnalysisEndpoints();
        endpoints.MapExecutionReplayPreparationEndpoints();
        endpoints.MapExecutionReplayMaterializationEndpoints();
        endpoints.MapExecutionReplayLineageEndpoints();
        endpoints.MapExecutionReplayApprovalEndpoints();
        endpoints.MapExecutionReplayPolicyEndpoints();
        endpoints.MapExecutionReplayPolicyOverrideEndpoints();
        endpoints.MapExecutionReplayAdmissionStatusEndpoints();
        endpoints.MapExecutionReplayAdmissionEndpoints();

        return endpoints;
    }
}

