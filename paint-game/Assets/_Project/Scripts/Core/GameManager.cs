// GameManager.cs — top-level singleton. Holds references to all major systems.
// Survives scene loads only via Bootstrap → Game scene workflow.
using UnityEngine;

namespace PaintGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Systems (auto-found on Awake)")]
        public TerritoryMap      TerritoryMap      { get; private set; }
        public TerritoryRenderer TerritoryRenderer { get; private set; }
        public MatchManager      MatchManager      { get; private set; }
        public ScoreTracker      ScoreTracker      { get; private set; }
        public PoolRegistry      PoolRegistry      { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            TerritoryMap      = GetComponentInChildren<TerritoryMap>(true);
            TerritoryRenderer = GetComponentInChildren<TerritoryRenderer>(true);
            MatchManager      = GetComponentInChildren<MatchManager>(true);
            ScoreTracker      = GetComponentInChildren<ScoreTracker>(true);
            PoolRegistry      = Object.FindFirstObjectByType<PoolRegistry>();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                GameEvents.ClearAll();
                Instance = null;
            }
        }
    }
}
