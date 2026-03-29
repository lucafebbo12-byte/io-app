// SprayCone.cs — static utility: compute which tiles fall within a spray cone.
// Direct C# port of checkpoint-paint/shared/sprayCone.js → getConeTiles()
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public static class SprayCone
    {
        // Reuse a HashSet per-call to avoid per-call allocation in hot path
        // (Not thread-safe — fine for single-threaded game logic.)
        private static readonly HashSet<int> _seen  = new HashSet<int>(256);
        private static readonly List<Vector2Int> _result = new List<Vector2Int>(256);

        /// <summary>
        /// Returns all tile coordinates (tx,ty) inside the spray cone.
        /// worldX/Y: shooter world position.
        /// aimAngle: radians, 0=right.
        /// </summary>
        public static List<Vector2Int> GetConeTiles(
            float worldX, float worldY,
            float aimAngle,
            float range     = GameConstants.SPRAY_RANGE,
            float halfAngle = GameConstants.SPRAY_HALF_ANGLE_RAD,
            int   rayCount  = GameConstants.RAY_COUNT)
        {
            _seen.Clear();
            _result.Clear();

            float rayStep = GameConstants.TILE_SIZE;

            for (int ri = 0; ri <= rayCount; ri++)
            {
                float angle = aimAngle - halfAngle + (2f * halfAngle * ri / rayCount);
                float cosA  = Mathf.Cos(angle);
                float sinA  = Mathf.Sin(angle);

                for (float dist = 0; dist <= range; dist += rayStep)
                {
                    float wx = worldX + cosA * dist;
                    float wy = worldY + sinA * dist;

                    int tx = Mathf.FloorToInt(wx / GameConstants.TILE_SIZE);
                    int ty = Mathf.FloorToInt(wy / GameConstants.TILE_SIZE);

                    if (!GameConstants.InBounds(tx, ty)) break;

                    int key = ty * GameConstants.MAP_W + tx;
                    if (_seen.Add(key))
                        _result.Add(new Vector2Int(tx, ty));
                }
            }

            return _result;
        }
    }
}
