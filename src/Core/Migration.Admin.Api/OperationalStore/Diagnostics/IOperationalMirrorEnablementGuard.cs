namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalMirrorEnablementGuard
{
    Task<OperationalMirrorEnablementGuardResult> EvaluateAsync(
        CancellationToken cancellationToken = default);
}


