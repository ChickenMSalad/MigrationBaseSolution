namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalMirrorReadinessEvaluator
{
    OperationalMirrorReadinessStatus Evaluate();
}


