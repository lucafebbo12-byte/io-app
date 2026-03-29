// PlayerController.cs — top-level player MonoBehaviour.
// Movement + zone detection run on the 20 Hz logic tick from MatchManager.
// Visual-only code runs in Update().
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        // ── References ────────────────────────────────────────────────────────
        public  PlayerStats  Stats   { get; private set; }
        private PlayerInput  _input;
        private PlayerVisuals _visuals;
        private WeaponBase   _weapon;
        private TerritoryMap _map;
        private CheckpointController _checkpoint;

        // ── Respawn ───────────────────────────────────────────────────────────
        private Vector2 _spawnPos;
        private bool    _respawning;

        // ── Init ──────────────────────────────────────────────────────────────
        public void Init(byte ownerIndex, Color color, Vector2 spawnWorldPos,
                         WeaponBase weapon, TerritoryMap map, PoolRegistry pool,
                         bool isHuman, CheckpointController checkpoint, string playerName)
        {
            Stats         = GetComponent<PlayerStats>();
            _input        = GetComponent<PlayerInput>();
            _visuals      = GetComponent<PlayerVisuals>();
            _weapon       = weapon;
            _map          = map;
            _checkpoint   = checkpoint;
            _spawnPos     = spawnWorldPos;

            Stats.OwnerIndex  = ownerIndex;
            Stats.PlayerColor = color;
            Stats.IsHuman     = isHuman;
            Stats.PlayerName  = playerName;
            Stats.WorldPos    = spawnWorldPos;
            transform.position = new Vector3(spawnWorldPos.x, spawnWorldPos.y, 0f);

            _weapon.Init(Stats, map, pool);

            if (isHuman && _input != null)
                _input.Init(Stats);

            _visuals?.Init(Stats);
        }

        // ── Logic tick (20 Hz, called by MatchManager) ────────────────────────
        private BotController _bot;
        private List<CheckpointController> _checkpointsCache;

        public void SetCheckpointsCache(List<CheckpointController> checkpoints)
            => _checkpointsCache = checkpoints;

        public void LogicStep(float dt, List<PlayerController> allPlayers)
        {
            if (!Stats.Alive || _respawning) return;

            // 1. Read input (human) or run bot AI
            if (Stats.IsHuman)
                _input?.ReadInput();
            else
            {
                if (_bot == null) _bot = GetComponent<BotController>();
                _bot?.Think(dt, allPlayers, _checkpointsCache ?? new List<CheckpointController>());
            }

            // 2. Update zone
            UpdateZone();

            // 3. Move with wall-slide (port of Player.move())
            Move(dt);

            // 4. Spray
            _weapon?.SprayTick(dt);
            _weapon?.FireTick(dt);
            _weapon?.RefillTick(dt);

            // Sync world pos to transform
            transform.position = new Vector3(Stats.WorldPos.x, Stats.WorldPos.y, 0f);
        }

        // ── Movement (wall-slide) ──────────────────────────────────────────────
        private void Move(float dt)
        {
            if (Stats.MoveDir.sqrMagnitude < 0.001f) return;

            float speed = GameConstants.PLAYER_SPEED
                        * Stats.SpeedMultiplier
                        * (_weapon != null ? _weapon.Config.moveSpeedMultiplier : 1f);

            Vector2 delta = Stats.MoveDir * speed * dt;
            Vector2 next  = Stats.WorldPos + delta;

            // Try full move
            if (!TileBlocked(next))
            {
                Stats.WorldPos = next;
                return;
            }

            // Slide on X
            Vector2 slideX = new Vector2(next.x, Stats.WorldPos.y);
            if (!TileBlocked(slideX)) { Stats.WorldPos = slideX; return; }

            // Slide on Y
            Vector2 slideY = new Vector2(Stats.WorldPos.x, next.y);
            if (!TileBlocked(slideY)) { Stats.WorldPos = slideY; }
        }

        private bool TileBlocked(Vector2 pos)
        {
            // Check the four corners of a small player bounding box (8×8 units)
            float half = 3.5f;
            return _map.IsWall(GameConstants.WorldToTile(pos.x - half, pos.y - half))
                || _map.IsWall(GameConstants.WorldToTile(pos.x + half, pos.y - half))
                || _map.IsWall(GameConstants.WorldToTile(pos.x - half, pos.y + half))
                || _map.IsWall(GameConstants.WorldToTile(pos.x + half, pos.y + half));
        }

        // ── Zone detection ─────────────────────────────────────────────────────
        private void UpdateZone()
        {
            var tile  = GameConstants.WorldToTile(Stats.WorldPos.x, Stats.WorldPos.y);
            byte owner = _map.GetOwner(tile.x, tile.y);

            if      (owner == Stats.OwnerIndex)         Stats.CurrentZone = PlayerStats.ZoneType.Own;
            else if (owner == GameConstants.OWNER_NEUTRAL) Stats.CurrentZone = PlayerStats.ZoneType.Neutral;
            else                                         Stats.CurrentZone = PlayerStats.ZoneType.Enemy;
        }

        // ── Damage + Death ─────────────────────────────────────────────────────
        public void TakeDamage(float amount, byte attackerIndex = 0)
        {
            if (!Stats.Alive || _respawning) return;

            Stats.HP -= Mathf.CeilToInt(amount);

            _visuals?.FlashHit();

            if (Stats.HP <= 0)
                Die(attackerIndex);
        }

        private void Die(byte killerIndex)
        {
            Stats.SetAlive(false);
            GameEvents.RaisePlayerDied(this);

            // Find killer for kill-feed
            // (MatchManager holds player list; skip for now, raise with null attacker)

            if (Stats.CheckpointAlive)
                StartCoroutine(RespawnCoroutine());
            else
                Eliminate();
        }

        private IEnumerator RespawnCoroutine()
        {
            _respawning = true;
            gameObject.GetComponent<SpriteRenderer>().enabled = false;
            yield return new WaitForSeconds(GameConstants.RESPAWN_DELAY);

            Stats.WorldPos = _spawnPos;
            transform.position = new Vector3(_spawnPos.x, _spawnPos.y, 0f);
            Stats.ResetForRespawn();
            gameObject.GetComponent<SpriteRenderer>().enabled = true;
            _respawning = false;

            GameEvents.RaisePlayerRespawned(this);
        }

        public void Eliminate()
        {
            Stats.SetAlive(false);
            gameObject.SetActive(false);
            GameEvents.RaisePlayerEliminated(this);
        }

        // ── Checkpoint link ───────────────────────────────────────────────────
        public void OnCheckpointDestroyed()
        {
            Stats.CheckpointAlive = false;
            // If currently dead and respawning — cancel, eliminate
            if (_respawning)
            {
                StopAllCoroutines();
                Eliminate();
            }
        }

        public CheckpointController GetCheckpoint() => _checkpoint;
    }
}
