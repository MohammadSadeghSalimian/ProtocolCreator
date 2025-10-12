using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProtocolCreator.Core.NewDesign;

public sealed class NewEngine : BaseEngine
{
    private readonly Dictionary<CycleState, IStageHandler> _handlers;


    public NewEngine(IReadOnlyList<DriftSegment> driftSegments, AnalysisInformation info)
        : base(driftSegments, info)
    {
        var coefProvider = new DefaultCoefficientProvider(info.Coefficients);
        _handlers = new()
        {
            [CycleState.PL] = new PositiveLoadingHandler(info, coefProvider, AllDeltas),
            [CycleState.PU] = new PositiveUnloadingHandler(info, coefProvider, AllDeltas),
            [CycleState.NL] = new NegativeLoadingHandler(info, coefProvider, AllDeltas),
            [CycleState.NU] = new NegativeUnloadingHandler(info, coefProvider, AllDeltas),
        };
    }


    public override void Calculate()
    {
        var ctx = new EngineContext
        {
            Id = 0,
            Cycle = 0,
            CurrentElongation = 0,
            PeakPositive = 0,
            PeakNegative = 0,
            ExperiencedYield = false
        };


        foreach (var seg in DriftSegments)
        {
            if (!_handlers.TryGetValue(seg.CycleState, out var handler))
                throw new ArgumentOutOfRangeException(nameof(seg.CycleState), seg.CycleState, "Unsupported cycle state");


            handler.Handle(seg, ctx);
        }
    }
}

// Strongly-typed doubles with tolerance-safe equality for (a,b) pairs

// Direction & Phase make intent explicit and remove overload duplication

// Context carries the minimal mutable state across steps (id, cycle, peaks, etc.)

// Encapsulate coefficients access

// Strategy per CycleState → avoids giant switch in Calculate()