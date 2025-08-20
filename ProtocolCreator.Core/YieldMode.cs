namespace ProtocolCreator.Core;

public enum YieldMode
{
    EitherSide,   // crosses either +station or -station
    Directional   // positive-going must cross +station; negative-going must cross -station
}