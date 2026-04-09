// using DWD.Utility.Loading; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "CommandTaskDefinition", menuName = "VoidRogues/NonPlayerCharacters/CommandTaskDefinition")]
    public class CommandTaskDefinition : ScriptableObject
    {
        // [BundleObject(typeof(Sprite))] // TODO: Port BundleObject from LichLord
        // [SerializeField]
        // protected BundleObject _icon;
        // public BundleObject Icon => _icon;

        [SerializeField]
        protected ECommandTaskType _taskType;
        public ECommandTaskType TaskType => _taskType;
    }

    public enum ECommandTaskType : byte
    {
        None,
        Lumbering,
        Mining,
        Foraging,
        Herbalism,
        Transport
    }
}
