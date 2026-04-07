using Fusion;

namespace LichLord
{
    public interface IContextBehaviour
    {
        SceneContext Context { get; set; }
    }

    public abstract class ContextBehaviour : NetworkBehaviour, IContextBehaviour
    {
        public SceneContext Context { get; set; }
    }

    public abstract class ContextTRSPBehaviour : NetworkTRSP, IContextBehaviour
    {
        public SceneContext Context { get; set; }
    }
}
