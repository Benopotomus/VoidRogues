using UnityEngine;

namespace VoidRogues
{
    public struct FNonPlayerCharacterSpawnParams
    {
        public int Index;
        public int DefinitionId;
        public Vector3 Position;
        public Quaternion Rotation;
        public ETeamID TeamId;

        public void Copy(FNonPlayerCharacterSpawnParams other)
        {
            Index = other.Index;
            DefinitionId = other.DefinitionId;
            Position = other.Position;
            Rotation = other.Rotation;
            TeamId = other.TeamId;
        }
    }
}
