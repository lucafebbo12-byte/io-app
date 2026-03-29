// GameSessionSO.cs — persists player choices between MainMenu and Game scenes.
// Create one instance: Assets/_Project/ScriptableObjects/GameSession.asset
using UnityEngine;

namespace PaintGame
{
    [CreateAssetMenu(menuName = "PaintGame/GameSession", fileName = "GameSession")]
    public class GameSessionSO : ScriptableObject
    {
        [Tooltip("0 = Shotgun, 1 = AK")]
        public int selectedWeaponIndex = 0;

        [Tooltip("Display name entered on MainMenu (optional)")]
        public string playerName = "You";

        public void SelectShotgun() { selectedWeaponIndex = 0; }
        public void SelectAK()      { selectedWeaponIndex = 1; }
    }
}
