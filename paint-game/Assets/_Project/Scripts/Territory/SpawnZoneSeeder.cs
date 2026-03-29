// SpawnZoneSeeder.cs — paints initial spawn circles on Awake.
// Must run AFTER TerritoryRenderer is ready.
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    [DefaultExecutionOrder(10)]
    public class SpawnZoneSeeder : MonoBehaviour
    {
        void Start()
        {
            var map      = GameManager.Instance.TerritoryMap;
            var renderer = GameManager.Instance.TerritoryRenderer;

            var seeded = new List<PaintedTile>(512);

            for (int i = 0; i < GameConstants.SPAWN_TILES.Length; i++)
            {
                var spawn = GameConstants.SPAWN_TILES[i];
                byte ownerIndex = (byte)(i + 1);

                int r = GameConstants.SPAWN_RADIUS_TILES;
                for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int tx = spawn.x + dx;
                    int ty = spawn.y + dy;
                    if (!GameConstants.InBounds(tx, ty)) continue;
                    if (GameConstants.IsWall(tx, ty))   continue;

                    map.Paint(tx, ty, ownerIndex);
                    seeded.Add(new PaintedTile(tx, ty, ownerIndex));
                }
            }

            // Flush initial seeded tiles directly to RT (bypasses the dirty queue)
            renderer.SeedTilesImmediate(seeded);
            // Also flush map dirty so checkpoints don't see these as new changes
            map.FlushDirty();
        }
    }
}
