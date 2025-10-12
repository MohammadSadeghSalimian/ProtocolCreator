using ProtocolCreator.Core.NewDesign;

namespace ProtocolCreator.Core;

public interface IStageHandler
{
    void Handle(DriftSegment segment, EngineContext ctx);
}