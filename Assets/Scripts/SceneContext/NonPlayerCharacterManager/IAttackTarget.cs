// using LichLord.World; // TODO: Port from LichLord

namespace VoidRogues
{
    public interface IAttackTarget
    {
        // TODO: Port IChunkTrackable from LichLord
        // public IChunkTrackable ChunkTrackable { get; }

        public bool IsAttackable { get; }
    }
}
