namespace ProtocolCreator.Core.NewDesign;

internal sealed class PositiveLoadingHandler(AnalysisInformation info, ICoefficientProvider coefs, List<Delta> sink)
    : StageHandlerBase(info, coefs)
{
    public override void Handle(DriftSegment item, EngineContext ctx)
    {
        var dy = Info.RebarYieldDrift;
        var dEf = Info.EffectiveDepth;
        var step = item.UnsignedStep;

        var arr = MathExtension.Arrange(item.Start, item.End, step);
        var n = arr.Length;

        for (int i = 0; i < n; i++)
        {
            // EXACT ORDER: increment id, then cycle, then use local copies
            ctx.Id += 1;
            ctx.Cycle += 0.25 / n;
            var id = ctx.Id;
            var cycle = ctx.Cycle;

            var a = arr[i];
            var b = a + step;

            // same guards
            GuardNoCrossing(a, b, dy, "yield drift");
            GuardNoCrossing(a, b, ctx.PeakPositive, "peak positive drift");

            if (a >= dy && b > dy)
                ctx.ExperiencedYield = true;

            var inElastic = a < dy && b <= dy;
            var coef = inElastic ? Coefs.GetElastic(Direction.Positive)
                : Coefs.GetPlastic(Direction.Positive);

            var repeat = GetRepeat(ctx.RepeatCounter, a, b, ctx.ExperiencedYield, inElastic);
            var slope = Extensions.GetSlopeOfElongationLine(repeat);

            var newExperiencedDrift = a >= ctx.PeakPositive && b > ctx.PeakPositive;
            if (newExperiencedDrift) ctx.PeakPositive = b;

            var ecr = slope * coef * dEf;
            var deltaE = ecr * step / 100.0;
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
                cond // DeltaCondition
            );

            // commit
            sink.Add(delta);
            ctx.CurrentElongation = destE;
        }
    }
}