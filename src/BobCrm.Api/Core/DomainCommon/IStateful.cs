namespace BobCrm.Api.Core.DomainCommon;

public interface IStateful
{
    string State { get; }
    bool CanTransitionTo(string nextState);
}
