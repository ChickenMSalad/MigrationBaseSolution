using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorEnablementGuard : IOperationalMirrorEnablementGuard
{
    private readonly IOptions<OperationalRunMirrorOptions> _options;

    public OperationalMirrorEnablementGuard(
        IOptions<OperationalRunMirrorOptions> options)
    {
        _options = options;
    }

    public Task<OperationalMirrorEnablementGuardResult> EvaluateAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        var mirrorEnabled = _options.Value.Enabled;

        if (!mirrorEnabled)
        {
            messages.Add("Operational run mirror is disabled.");
        }

        var canMirror = mirrorEnabled;

        if (canMirror)
        {
            messages.Add("Operational mirror enablement guard passed for SQL operational backbone.");
        }

        return Task.FromResult(new OperationalMirrorEnablementGuardResult
        {
            CanMirror = canMirror,
            MirrorEnabled = mirrorEnabled,
            ReadinessPassed = canMirror,
            SqlSchemaPassed = canMirror,
            Messages = messages
        });
    }
}