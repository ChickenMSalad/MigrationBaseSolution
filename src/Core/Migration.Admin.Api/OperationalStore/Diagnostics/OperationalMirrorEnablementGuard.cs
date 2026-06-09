using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorEnablementGuard : IOperationalMirrorEnablementGuard
{
    private readonly IOptions<OperationalRunMirrorOptions> _options;
    private readonly IOperationalMirrorReadinessEvaluator _readinessEvaluator;
    private readonly IOperationalSqlSchemaSmokeTestService _schemaSmokeTestService;

    public OperationalMirrorEnablementGuard(
        IOptions<OperationalRunMirrorOptions> options,
        IOperationalMirrorReadinessEvaluator readinessEvaluator,
        IOperationalSqlSchemaSmokeTestService schemaSmokeTestService)
    {
        _options = options;
        _readinessEvaluator = readinessEvaluator;
        _schemaSmokeTestService = schemaSmokeTestService;
    }

    public async Task<OperationalMirrorEnablementGuardResult> EvaluateAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        var mirrorEnabled = _options.Value.Enabled;

        if (!mirrorEnabled)
        {
            messages.Add("Operational run mirror is disabled.");
        }

        var readiness = _readinessEvaluator.Evaluate();

        if (!readiness.Ready)
        {
            foreach (var message in readiness.Messages)
            {
                messages.Add(message);
            }
        }

        var schemaSmokeTest = await _schemaSmokeTestService.ExecuteAsync(
            cancellationToken);

        if (!schemaSmokeTest.Success)
        {
            foreach (var message in schemaSmokeTest.Messages)
            {
                messages.Add(message);
            }
        }

        var canMirror =
            mirrorEnabled &&
            readiness.Ready &&
            schemaSmokeTest.Success;

        if (canMirror)
        {
            messages.Add("Operational mirror enablement guard passed.");
        }

        return new OperationalMirrorEnablementGuardResult
        {
            CanMirror = canMirror,
            MirrorEnabled = mirrorEnabled,
            ReadinessPassed = readiness.Ready,
            SqlSchemaPassed = schemaSmokeTest.Success,
            Messages = messages
        };
    }
}


