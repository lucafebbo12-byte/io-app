// GameConstants.cs — single source of truth for all numeric constants.
// Ported from checkpoint-paint/shared/constants.js
using UnityEngine;

namespace PaintGame
{
    public static class GameConstants
    {
        // ── Map ──────────────────────────────────────────────────────────────
        public const int   MAP_W             = 240;
        public const int   MAP_H             = 240;
        public const int   TILE_SIZE         = 8;          // world units per tile
        public const float WORLD_W           = MAP_W * TILE_SIZE;   // 1920
        public const float WORLD_H           = MAP_H * TILE_SIZE;   // 1920

        // ── Players ──────────────────────────────────────────────────────────
        public const int   TOTAL_PLAYERS     = 6;
        public const int   BOT_COUNT         = 5;
        public const int   PLAYER_MAX_HP     = 3;
        public const float RESPAWN_DELAY     = 2.0f;

        // ── Movement ─────────────────────────────────────────────────────────
        // Original: 6.5 units/tick at 20 Hz → 130 units/sec
        public const float PLAYER_SPEED      = 130f;       // units/sec
        public const float ZONE_SPEED_OWN    = 1.4f;
        public const float ZONE_SPEED_ENEMY  = 0.65f;

        // ── Spray / Ink ───────────────────────────────────────────────────────
        public const float SPRAY_RANGE           = 200f;
        public const float SPRAY_HALF_ANGLE_RAD  = Mathf.PI / 7.2f;   // 25°
        public const int   RAY_COUNT             = 22;
        public const float INK_MAX               = 100f;
        // Per-second equivalents (original was per-tick at 20 Hz)
        public const float INK_DRAIN_PER_SEC     = 2.5f * 20f;  // 50/sec
        public const float INK_REFILL_PER_SEC    = 5f   * 20f;  // 100/sec (own zone)

        // ── Bullets ───────────────────────────────────────────────────────────
        public const float BULLET_SPEED          = 14f * 20f;  // units/sec (280)
        public const int   BULLET_PAINT_RADIUS   = 2;          // tiles
        public const float BULLET_HIT_RADIUS     = 12f;        // world units
        public const float BULLET_MAX_LIFETIME   = 6f;         // seconds

        // ── Checkpoints ──────────────────────────────────────────────────────
        public const int   CHECKPOINT_HP         = 25;
        public const int   CHECKPOINT_RADIUS     = 3;          // tiles (Chebyshev)
        public const int   SPAWN_RADIUS_TILES    = 8;          // initial painted circle

        // ── Match ─────────────────────────────────────────────────────────────
        public const float ROUND_TIME            = 90f;        // seconds
        public const float LOGIC_TICK_RATE       = 20f;        // Hz
        public const float LOGIC_TICK_INTERVAL   = 1f / LOGIC_TICK_RATE;  // 0.05s

        // ── Ownership indices ─────────────────────────────────────────────────
        public const byte  OWNER_NEUTRAL         = 0;
        public const byte  OWNER_WALL            = 255;
        // Players use indices 1-6

        // ── Player colors ─────────────────────────────────────────────────────
        // Bold saturated palette — index 0 = neutral/clear, 1-6 = players
        public static readonly Color[] PLAYER_COLORS = new Color[]
        {
            Color.clear,                                              // 0 = neutral
            new Color(0.95f, 0.15f, 0.15f, 1f),  // 1 Red
            new Color(0.10f, 0.55f, 1.00f, 1f),  // 2 Blue
            new Color(1.00f, 0.65f, 0.00f, 1f),  // 3 Orange
            new Color(0.10f, 0.90f, 0.20f, 1f),  // 4 Bright Green
            new Color(0.65f, 0.10f, 0.90f, 1f),  // 5 Purple
            new Color(0.00f, 0.90f, 0.90f, 1f),  // 6 Cyan
        };

        // ── Blob arena definition ─────────────────────────────────────────────
        public struct BlobDef
        {
            public float CenterX, CenterY, RadiusX, RadiusY;
        }

        // 7 blobs: 1 central hub + 6 arms (one per player)
        public static readonly BlobDef[] BLOB_DEFS = new BlobDef[]
        {
            new BlobDef { CenterX=120, CenterY=120, RadiusX=42, RadiusY=42 }, // hub
            new BlobDef { CenterX=188, CenterY=120, RadiusX=28, RadiusY=22 }, // arm 0°
            new BlobDef { CenterX=154, CenterY=179, RadiusX=22, RadiusY=28 }, // arm 60°
            new BlobDef { CenterX= 86, CenterY=179, RadiusX=22, RadiusY=28 }, // arm 120°
            new BlobDef { CenterX= 52, CenterY=120, RadiusX=28, RadiusY=22 }, // arm 180°
            new BlobDef { CenterX= 86, CenterY= 61, RadiusX=22, RadiusY=28 }, // arm 240°
            new BlobDef { CenterX=154, CenterY= 61, RadiusX=22, RadiusY=28 }, // arm 300°
        };

        // ── Spawn positions (tile coordinates) — one per arm blob ─────────────
        public static readonly Vector2Int[] SPAWN_TILES = new Vector2Int[]
        {
            new Vector2Int(188, 120),  // arm 0°
            new Vector2Int(154, 179),  // arm 60°
            new Vector2Int( 86, 179),  // arm 120°
            new Vector2Int( 52, 120),  // arm 180°
            new Vector2Int( 86,  61),  // arm 240°
            new Vector2Int(154,  61),  // arm 300°
        };

        // ── Wall geometry helpers ──────────────────────────────────────────────
        public static bool IsInBlob(int tx, int ty)
        {
            foreach (var b in BLOB_DEFS)
            {
                float nx = (tx - b.CenterX) / b.RadiusX;
                float ny = (ty - b.CenterY) / b.RadiusY;
                if (nx * nx + ny * ny <= 1f) return true;
            }
            return false;
        }

        public static bool IsWall(int tx, int ty)
        {
            if (!InBounds(tx, ty)) return true;
            return !IsInBlob(tx, ty);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        public static Vector2 TileToWorld(int tx, int ty)
            => new Vector2(tx * TILE_SIZE + TILE_SIZE * 0.5f,
                           ty * TILE_SIZE + TILE_SIZE * 0.5f);

        public static Vector2Int WorldToTile(float wx, float wy)
            => new Vector2Int(Mathf.FloorToInt(wx / TILE_SIZE),
                              Mathf.FloorToInt(wy / TILE_SIZE));

        public static bool InBounds(int tx, int ty)
            => tx >= 0 && ty >= 0 && tx < MAP_W && ty < MAP_H;
    }
}
