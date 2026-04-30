using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.Connectors.Targets.Aprimo.Models.Aprimo;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public static class AprimoClassificationResolver
    {
        public static AprimoClassification ResolveRequired(
            IEnumerable<AprimoClassification> all,
            string fieldName,
            string value)
        {
            if (all == null) throw new ArgumentNullException(nameof(all));
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Field name is required.", nameof(fieldName));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", nameof(value));

            var candidates = all
                .Where(c => HasLabel(c, value))
                .ToList();

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No classification found with label '{value}' (field '{fieldName}').");
            }

            if (candidates.Count == 1)
                return candidates[0];

            // Disambiguate by parent
            var parentMatched = candidates
                .Where(c => ParentMatchesField(c, fieldName))
                .ToList();

            if (parentMatched.Count == 1)
                return parentMatched[0];

            if (parentMatched.Count > 1)
            {
                // Still ambiguous even after parent match: fail loud
                var ids = string.Join(", ", parentMatched.Select(c => c.Id).Where(id => !string.IsNullOrWhiteSpace(id)));
                throw new InvalidOperationException(
                    $"Ambiguous classification label '{value}' for field '{fieldName}'. " +
                    $"Multiple candidates match parent. Candidate IDs: [{ids}]");
            }

            // No parent match: fail loud (safer than guessing)
            var allIds = string.Join(", ", candidates.Select(c => c.Id).Where(id => !string.IsNullOrWhiteSpace(id)));
            throw new InvalidOperationException(
                $"Ambiguous classification label '{value}' for field '{fieldName}'. " +
                $"None matched parent name/label. Candidate IDs: [{allIds}]");
        }

        private static bool HasLabel(AprimoClassification c, string value)
        {
            if (c?.Labels == null) return false;

            return c.Labels.Any(l =>
                !string.IsNullOrWhiteSpace(l?.Value) &&
                string.Equals(l.Value.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool ParentMatchesField(AprimoClassification c, string fieldName)
        {
            var parent = c?.Embedded?.Parent;
            if (parent == null) return false;

            // Compare against parent name (best if present)
            if (!string.IsNullOrWhiteSpace(parent.Name) &&
                parent.Name.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Fallback: compare against any parent label value
            if (parent.Labels != null && parent.Labels.Any(l =>
                !string.IsNullOrWhiteSpace(l?.Value) &&
                l.Value.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0))
                return true;

            return false;
        }
    }

}
