// PlayerSpawnManager.cs — instantiates the human player and 5 bots at startup.
using UnityEngine;

namespace PaintGame
{
    [DefaultExecutionOrder(5)]
    public class PlayerSpawnManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PlayerController _playerPrefab;
        [SerializeField] private PlayerController _botPrefab;

        [Header("Weapon configs")]
        [SerializeField] private WeaponConfigSO _shotgunConfig;
        [SerializeField] private WeaponConfigSO _akConfig;

        // Chosen by main menu — 0=Shotgun, 1=AK
        public static int SelectedWeaponIndex = 0;

        // All controllers (index 0 = human)
        public static PlayerController[] Players { get; private set; }

        void Start()
        {
            var map       = GameManager.Instance.TerritoryMap;
            var pool      = GameManager.Instance.PoolRegistry;

            Players = new PlayerController[GameConstants.TOTAL_PLAYERS];

            for (int i = 0; i < GameConstants.TOTAL_PLAYERS; i++)
            {
                bool isHuman     = (i == 0);
                byte ownerIndex  = (byte)(i + 1);
                Color color      = GameConstants.PLAYER_COLORS[ownerIndex];
                Vector2 spawnPos = GameConstants.TileToWorld(
                    GameConstants.SPAWN_TILES[i].x,
                    GameConstants.SPAWN_TILES[i].y);

                var prefab = isHuman ? _playerPrefab : _botPrefab;
                var go     = Instantiate(prefab, new Vector3(spawnPos.x, spawnPos.y, 0f),
                                         Quaternion.identity);
                go.name = isHuman ? "Player_Human" : $"Bot_{i}";

                // Assign weapon component
                WeaponConfigSO cfg = (isHuman && SelectedWeaponIndex == 1) ? _akConfig : _shotgunConfig;
                WeaponBase weapon  = cfg.weaponName == "AK"
                    ? (WeaponBase)go.GetComponent<AKWeapon>() ?? go.AddComponent<AKWeapon>()
                    : (WeaponBase)go.GetComponent<ShotgunWeapon>() ?? go.AddComponent<ShotgunWeapon>();

                // Reflect the config onto the weapon
                var wField = typeof(WeaponBase).GetField("_config",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                wField?.SetValue(weapon, cfg);

                // Find this player's checkpoint
                var checkpoints = FindObjectsOfType<CheckpointController>();
                CheckpointController cp = null;
                foreach (var c in checkpoints)
                    if (c.OwnerIndex == ownerIndex) { cp = c; break; }

                go.GetComponent<PlayerController>().Init(
                    ownerIndex, color, spawnPos, weapon, map, pool,
                    isHuman, cp, isHuman ? "You" : $"Bot {i}");

                // Init bot AI if applicable
                if (!isHuman)
                    go.GetComponent<BotController>()?.Init(go.GetComponent<PlayerController>());

                Players[i] = go.GetComponent<PlayerController>();
            }
        }
    }
}
