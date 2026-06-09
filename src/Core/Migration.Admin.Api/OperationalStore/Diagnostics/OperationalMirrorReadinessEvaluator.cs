using Migration.Application.Abstractions.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorReadinessEvaluator : IOperationalMirrorReadinessEvaluator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<OperationalRunMirrorOptions> _options;

    public OperationalMirrorReadinessEvaluator(
        IServiceProvider serviceProvider,
        IOptions<OperationalRunMirrorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    public OperationalMirrorReadinessStatus Evaluate()
    {
        var messages = new List<string>();

        var enabled = _options.Value.Enabled;

        var mirrorServiceRegistered =
            _serviceProvider.GetService<IAdminOperationalRunMirrorService>() is not null;

        var optionsValidatorRegistered =
            _serviceProvider.GetService<IValidateOptions<OperationalRunMirrorOptions>>() is not null;

        var operationalStoreRegistered =
            _serviceProvider.GetService<IOperationalStore>() is not null;

        if (!enabled)
        {
            messages.Add("Operational run mirror is disabled.");
        }

        if (!mirrorServiceRegistered)
        {
            messages.Add("Operational run mirror service is not registered.");
        }

        if (!optionsValidatorRegistered)
        {
            messages.Add("Operational run mirror options validator is not registered.");
        }

        if (!operationalStoreRegistered)
        {
            messages.Add("Operational store facade is not registered.");
        }

        var ready =
            enabled &&
            mirrorServiceRegistered &&
            optionsValidatorRegistered &&
            operationalStoreRegistered;

        if (ready)
        {
            messages.Add("Operational run mirror is ready.");
        }

        return new OperationalMirrorReadinessStatus
        {
            Ready = ready,
            Enabled = enabled,
            MirrorServiceRegistered = mirrorServiceRegistered,
            OptionsValidatorRegistered = optionsValidatorRegistered,
            OperationalStoreRegistered = operationalStoreRegistered,
            Messages = messages
        };
    }
}


