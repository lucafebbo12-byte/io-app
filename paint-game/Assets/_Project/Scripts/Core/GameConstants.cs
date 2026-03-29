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
        // Saturated palette matching Paper.io / Dye Hard feel
        public static readonly Color[] PLAYER_COLORS = new Color[]
        {
            Color.clear,                                            // 0 = neutral (unused)
            new Color(0.96f, 0.26f, 0.21f, 1f),  // 1 Red
            new Color(0.13f, 0.59f, 0.95f, 1f),  // 2 Blue
            new Color(0.30f, 0.69f, 0.31f, 1f),  // 3 Green
            new Color(1.00f, 0.76f, 0.03f, 1f),  // 4 Yellow
            new Color(0.61f, 0.15f, 0.69f, 1f),  // 5 Purple
            new Color(1.00f, 0.34f, 0.13f, 1f),  // 6 Orange
        };

        // ── Spawn / Checkpoint positions (tile coordinates) ───────────────────
        // 6 positions: 4 corners + 2 mid-sides
        public static readonly Vector2Int[] SPAWN_TILES = new Vector2Int[]
        {
            new Vector2Int(20,  20),   // top-left
            new Vector2Int(220, 20),   // top-right
            new Vector2Int(20,  220),  // bottom-left
            new Vector2Int(220, 220),  // bottom-right
            new Vector2Int(120, 20),   // top-mid
            new Vector2Int(120, 220),  // bottom-mid
        };

        // ── Wall geometry helpers ──────────────────────────────────────────────
        // Cross structure matching the JS Map.isWall()
        public const int BORDER_THICKNESS        = 2;
        public const int CROSS_GAP_HALF          = 22;   // gap = 44 tiles centred on map

        public static bool IsWall(int tx, int ty)
        {
            // Border
            if (tx < BORDER_THICKNESS || ty < BORDER_THICKNESS ||
                tx >= MAP_W - BORDER_THICKNESS || ty >= MAP_H - BORDER_THICKNESS)
                return true;

            // Central cross (horizontal beam)
            int cx = MAP_W / 2;
            int cy = MAP_H / 2;
            bool inHBeam = (ty >= cy - 2 && ty <= cy + 1) && (tx < cx - CROSS_GAP_HALF || tx >= cx + CROSS_GAP_HALF);
            bool inVBeam = (tx >= cx - 2 && tx <= cx + 1) && (ty < cy - CROSS_GAP_HALF || ty >= cy + CROSS_GAP_HALF);
            return inHBeam || inVBeam;
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
