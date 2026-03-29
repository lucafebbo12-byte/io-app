// ScoreTracker.cs — caches tile counts per player, feeds scoreboard UI.
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public class ScoreTracker : MonoBehaviour
    {
        // Updated by MatchManager every 2 seconds to avoid counting every tick
        private float _updateTimer;
        private const float UPDATE_INTERVAL = 2f;

        public struct ScoreEntry
        {
            public PlayerController Player;
            public int TileCount;
            public float Percentage;
        }

        public List<ScoreEntry> Scores { get; } = new List<ScoreEntry>(6);

        public void UpdateScores(List<PlayerController> players)
        {
            _updateTimer += GameConstants.LOGIC_TICK_INTERVAL;
            if (_updateTimer < UPDATE_INTERVAL) return;
            _updateTimer = 0f;

            var map  = GameManager.Instance.TerritoryMap;
            int total = GameConstants.MAP_W * GameConstants.MAP_H;

            Scores.Clear();
            foreach (var p in players)
            {
                int count = map.CountTiles(p.Stats.OwnerIndex);
                Scores.Add(new ScoreEntry
                {
                    Player     = p,
                    TileCount  = count,
                    Percentage = (float)count / total * 100f,
                });
            }

            // Sort descending
            Scores.Sort((a, b) => b.TileCount.CompareTo(a.TileCount));
        }
    }
}
