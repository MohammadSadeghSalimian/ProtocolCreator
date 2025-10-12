namespace ProtocolCreator.Core.NewDesign;

internal sealed class NegativeLoadingHandler(AnalysisInformation info, ICoefficientProvider coefs, List<Delta> sink)
    : StageHandlerBase(info, coefs)
{
    public override void Handle(DriftSegment item, EngineContext ctx)
    {
        var dy = Info.RebarYieldDrift;
        var dEf = Info.EffectiveDepth;
        var step = item.UnsignedStep;

        var arr = MathExtension.Arrange(item.Start, item.End, -step);
        var n = arr.Length;

        for (int i = 0; i < n; i++)
        {
            ctx.Id += 1;
            ctx.Cycle += 0.25 / n;
            var id = ctx.Id;
            var cycle = ctx.Cycle;

            var a = arr[i];
            var b = a - step;

            GuardNoCrossing(a, b, -dy, "yield drift");
            GuardNoCrossing(a, b, ctx.PeakNegative, "peak negative drift");

            if (a <= -dy && b < -dy)
                ctx.ExperiencedYield = true;

            var inElastic = a > -dy && b >= -dy;
            var coef = inElastic ? Coefs.GetElastic(Direction.Negative)
                : Coefs.GetPlastic(Direction.Negative);

            var repeat = GetRepeat(ctx.RepeatCounter, a, b, ctx.ExperiencedYield, inElastic);
            var slope = Extensions.GetSlopeOfElongationLine(repeat);

            var newExperiencedDrift = a <= ctx.PeakNegative && b < ctx.PeakNegative;
            if (newExperiencedDrift) ctx.PeakNegative = b;

            var ecr = slope * coef * dEf;
            var deltaE = -ecr * step / 100.0; // identical to your code
            var destE = ctx.CurrentElongation + deltaE;

            var cond = Extensions.GetConditionInLoading(
                ctx.ExperiencedYield,
                newExperiencedDrift,
                /* isInPlasticArea */ !inElastic
            );

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

// This engine wires the strategy handlers.