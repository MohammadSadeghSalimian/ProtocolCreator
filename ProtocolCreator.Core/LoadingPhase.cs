namespace ProtocolCreator.Core;

public enum LoadingPhase : byte
{
    Loading,
    Unloading,
}

public enum CycleState
{
    PositiveLoading,
    PositiveUnloading,
    NegativeLoading,
    NegativeUnloading,
}