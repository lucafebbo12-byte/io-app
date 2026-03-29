// TerritoryMap.cs — authoritative 240×240 tile ownership grid.
// Ported from checkpoint-paint/server/Map.js
// Uses NativeArray<byte> for zero-GC, cache-friendly access.
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PaintGame
{
    public class TerritoryMap : MonoBehaviour
    {
        // Ownership grid: index 0=neutral, 1-6=players, 255=wall
        private NativeArray<byte> _grid;

        // Tiles changed this logic tick — flushed to renderer + checkpoint system
        private readonly List<PaintedTile> _dirty = new List<PaintedTile>(512);

        void Awake()
        {
            _grid = new NativeArray<byte>(
                GameConstants.MAP_W * GameConstants.MAP_H,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory
            );
            InitWalls();
        }

        void OnDestroy()
        {
            if (_grid.IsCreated) _grid.Dispose();
        }

        // ── Init ──────────────────────────────────────────────────────────────
        private void InitWalls()
        {
            for (int ty = 0; ty < GameConstants.MAP_H; ty++)
            for (int tx = 0; tx < GameConstants.MAP_W;  tx++)
            {
                if (GameConstants.IsWall(tx, ty))
                    _grid[Index(tx, ty)] = GameConstants.OWNER_WALL;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────
        public byte GetOwner(int tx, int ty)
        {
            if (!GameConstants.InBounds(tx, ty)) return GameConstants.OWNER_WALL;
            return _grid[Index(tx, ty)];
        }

        public bool IsWall(int tx, int ty)
        {
            if (!GameConstants.InBounds(tx, ty)) return true;
            return _grid[Index(tx, ty)] == GameConstants.OWNER_WALL;
        }

        /// <summary>Paint a tile. Walls are never overwritten.</summary>
        public void Paint(int tx, int ty, byte ownerIndex)
        {
            if (!GameConstants.InBounds(tx, ty)) return;
            int i = Index(tx, ty);
            byte current = _grid[i];
            if (current == GameConstants.OWNER_WALL) return;
            if (current == ownerIndex) return;  // no change needed

            _grid[i] = ownerIndex;
            _dirty.Add(new PaintedTile(tx, ty, ownerIndex));
        }

        /// <summary>
        /// Returns all tiles changed since last flush and clears the dirty list.
        /// Call once per logic tick — TerritoryRenderer and CheckpointController both read this.
        /// </summary>
        public List<PaintedTile> FlushDirty()
        {
            // Return a snapshot; caller must not hold reference across ticks
            var snapshot = new List<PaintedTile>(_dirty);
            _dirty.Clear();
            return snapshot;
        }

        /// <summary>Count tiles owned by a player index (for scoring).</summary>
        public int CountTiles(byte ownerIndex)
        {
            int count = 0;
            for (int i = 0; i < _grid.Length; i++)
                if (_grid[i] == ownerIndex) count++;
            return count;
        }

        /// <summary>Paint a circle of radius r tiles at tile center (cx,cy).</summary>
        public void PaintCircle(int cx, int cy, int radius, byte ownerIndex)
        {
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                    Paint(cx + dx, cy + dy, ownerIndex);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static int Index(int tx, int ty) => ty * GameConstants.MAP_W + tx;
    }
}
