namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Evaluates promotion readiness using deterministic key/value evidence supplied by deployment automation.
/// </summary>
public sealed class AzureDeploymentPromotionEvaluator : IAzureDeploymentPromotionEvaluator
{
    public AzureDeploymentPromotionDecision Evaluate(
        AzureDeploymentPromotionPolicy policy,
        IReadOnlyDictionary<string, string?> evidence)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(evidence);

        var decision = new AzureDeploymentPromotionDecision
        {
            RequiresManualApproval = policy.RequireManualApproval
        };

        foreach (var gate in policy.Gates)
        {
            if (string.IsNullOrWhiteSpace(gate.Name))
            {
                decision.Warnings.Add("A promotion gate is missing a name.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(gate.EvidenceKey))
            {
                if (gate.Required)
                {
                    decision.FailedGates.Add(gate.Name);
                }
                else
                {
                    decision.Warnings.Add($"Optional gate '{gate.Name}' has no evidence key.");
                }

                continue;
            }

            evidence.TryGetValue(gate.EvidenceKey, out var actualValue);
            var expectedValue = gate.ExpectedValue ?? string.Empty;

            if (string.Equals(actualValue ?? string.Empty, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                decision.PassedGates.Add(gate.Name);
                continue;
            }

            if (gate.Required)
            {
                decision.FailedGates.Add(gate.Name);
            }
            else
            {
                decision.Warnings.Add($"Optional gate '{gate.Name}' did not match expected evidence.");
            }
        }

        decision.IsAllowed = decision.FailedGates.Count == 0;
        return decision;
    }
}
