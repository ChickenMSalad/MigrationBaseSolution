using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Validation.Resumability;

public interface IRealMigrationResumabilityValidator
{
    Task<RealMigrationResumabilityValidationResult> ValidateAsync(
        RealMigrationResumabilityValidationContract contract,
        RealMigrationResumabilityEvidence evidence,
        CancellationToken cancellationToken = default);
}
