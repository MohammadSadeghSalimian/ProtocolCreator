using System.Diagnostics;

namespace ProtocolCreator.Core;
[DebuggerDisplay("{Start}-->{End} K:{Coefficient} R:{RepeatCount}")]
public readonly record struct TravelPiece(
    double Start,
    double End,
    double Coefficient,
    bool IsYield,
    int RepeatCount
);