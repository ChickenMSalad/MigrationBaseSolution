namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalMirrorWriteVerificationService
{
    Task<OperationalMirrorWriteVerificationResult> VerifyAsync(
        CancellationToken cancellationToken = default);
}


