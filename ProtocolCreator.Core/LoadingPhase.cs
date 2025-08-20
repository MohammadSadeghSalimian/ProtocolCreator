namespace ProtocolCreator.Core;

public enum LoadingPhase : byte
{
    Loading,
    Unloading,
}

public enum CycleState
{
    PL,
    PU,
    NL,
    NU,
}