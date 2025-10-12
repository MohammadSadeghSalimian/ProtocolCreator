namespace ProtocolCreator.Core.NewDesign;

public sealed class EngineContext
{
    public int Id { get; set; }
    public double Cycle { get; set; }
    public double CurrentElongation { get; set; }
    public double PeakPositive { get; set; }
    public double PeakNegative { get; set; }
    public bool ExperiencedYield { get; set; }


    // repeat counter per (a,b)
    public Dictionary<DoublePair, int> RepeatCounter { get; } = new();


    // convenience
    public void AdvanceCycle(int n) => Cycle += 0.25 / n;
    public int NextId() => ++Id;
}