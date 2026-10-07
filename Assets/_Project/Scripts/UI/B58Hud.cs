using Liminal.Core;
using Liminal.Player;
using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>좌상단 방 이름, 하단 [E] 조사, 복도 힌트.</summary>
    public class B58Hud : MonoBehaviour
    {
        public TankController player;
        public TMP_Text roomName;
        public GameObject prompt;
        public GameObject hint;

        void Update()
        {
            var game = B58Game.Instance;
            if (game == null) return;
            string n = B58Game.RoomNames[(int)game.Room];
            if (roomName && roomName.text != n) roomName.text = n;
            if (prompt) prompt.SetActive(game.PlayerCanAct && player && player.NearestSpot != null);
            if (hint) hint.SetActive(game.Room == B58Room.Corridor);
        }
    }
}
