namespace ProtocolCreator.Core.NewDesign;

internal sealed class NegativeUnloadingHandler(
    AnalysisInformation info,
    ICoefficientProvider coefs,
    List<Delta> sink)
    : StageHandlerBase(info, coefs)
{
    public override void Handle(DriftSegment item, EngineContext ctx)
    {
        var dy = Info.RebarYieldDrift;
        var dEf = Info.EffectiveDepth;
        var step = item.UnsignedStep;

        var arr = MathExtension.Arrange(item.Start, item.End, step);
        var future = item.Start + dy;
        var n = arr.Length;

        for (int i = 0; i < n; i++)
        {
            ctx.Id += 1;
            ctx.Cycle += 0.25 / n;
            var id = ctx.Id;
            var cycle = ctx.Cycle;

            var a = arr[i];
            var b = a + step;

            if (b > future && a < future)
                throw new ArgumentException($"The future drift ({future}) is placed between the steps. This is not allowed.");

            var coef = Coefs.GetUnloadingPlastic(Direction.Negative);
            var inResidual = b <= future;
            var slope = inResidual ? 1.0 : 0.0;
            var repeat = 0;

            var ecr = slope * coef * dEf;
            var deltaE = -ecr * step / 100.0; // identical sign
            var destE = ctx.CurrentElongation + deltaE;

            var cond = Extensions.GetDeltaConditionInUnLoading(inResidual);

            var delta = BuildDelta(
                id, cycle,
                a, b,
                ctx.CurrentElongation, destE,
                ctx.ExperiencedYield, coef,
                repeat, slope, ecr,
                item,
                cond
            );

            sink.Add(delta);
            ctx.CurrentElongation = destE;
        }
    }
    
}