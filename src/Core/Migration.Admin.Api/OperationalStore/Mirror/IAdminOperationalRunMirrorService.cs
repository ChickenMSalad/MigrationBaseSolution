using Migration.ControlPlane.Models;

namespace Migration.Admin.Api.OperationalStore;

public interface IAdminOperationalRunMirrorService
{
    Task MirrorRunAsync(
        MigrationProjectRecord project,
        MigrationRunControlRecord run,
        CancellationToken cancellationToken = default);
}


