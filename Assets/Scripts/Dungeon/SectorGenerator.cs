using System.Collections.Generic;
using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Dungeon
{
    /// <summary>
    /// Generates the room graph for a sector using a simplified BSP approach.
    /// Produces a list of <see cref="RoomNode"/> objects that the <see cref="RoomController"/>
    /// uses to instantiate and connect room prefabs at runtime.
    /// </summary>
    public class SectorGenerator : MonoBehaviour
    {
        [Header("Sector Layout")]
        [SerializeField] private int minCombatRooms = 6;
        [SerializeField] private int maxCombatRooms = 8;

        [Header("Room Templates")]
        [SerializeField] private RoomDataSO[] combatRoomTemplates;
        [SerializeField] private RoomDataSO   shopRoomTemplate;
        [SerializeField] private RoomDataSO   eventRoomTemplate;
        [SerializeField] private RoomDataSO   bossRoomTemplate;

        public IReadOnlyList<RoomNode> GeneratedRooms => _rooms;
        private readonly List<RoomNode> _rooms = new List<RoomNode>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Build a new room graph for the current sector.</summary>
        public void GenerateSector()
        {
            _rooms.Clear();

            int combatCount = Random.Range(minCombatRooms, maxCombatRooms + 1);

            // Start room
            var start = new RoomNode(RoomType.Start, null);
            _rooms.Add(start);

            RoomNode previous = start;

            // Combat rooms
            for (int i = 0; i < combatCount; i++)
            {
                var template = combatRoomTemplates[Random.Range(0, combatRoomTemplates.Length)];
                var room = new RoomNode(RoomType.Combat, template);
                previous.AddConnection(room);
                _rooms.Add(room);

                // Insert a shop or event room at the midpoint
                if (i == combatCount / 2)
                {
                    var mid = CreateSpecialRoom();
                    room.AddConnection(mid);
                    _rooms.Add(mid);
                    previous = mid;
                }
                else
                {
                    previous = room;
                }
            }

            // Boss room at the end
            var boss = new RoomNode(RoomType.Boss, bossRoomTemplate);
            previous.AddConnection(boss);
            _rooms.Add(boss);

            Debug.Log($"[SectorGenerator] Generated sector with {_rooms.Count} rooms.");
        }

        // ── Private ───────────────────────────────────────────────────────────

        private RoomNode CreateSpecialRoom()
        {
            // Alternate shop / event each sector
            int sector = GameManager.Instance != null
                ? GameManager.Instance.Run.CurrentSector : 1;
            bool placeShop = sector % 2 == 1;
            return placeShop
                ? new RoomNode(RoomType.Shop,  shopRoomTemplate)
                : new RoomNode(RoomType.Event, eventRoomTemplate);
        }
    }

    /// <summary>A node in the sector room graph.</summary>
    public class RoomNode
    {
        public RoomType Type       { get; }
        public RoomDataSO Template { get; }
        public List<RoomNode> Connections { get; } = new List<RoomNode>();
        public bool IsCleared { get; set; }

        public RoomNode(RoomType type, RoomDataSO template)
        {
            Type     = type;
            Template = template;
        }

        public void AddConnection(RoomNode other) => Connections.Add(other);
    }
}
