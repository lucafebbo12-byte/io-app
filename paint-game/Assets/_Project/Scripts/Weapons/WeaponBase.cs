// WeaponBase.cs — abstract base for all weapon types.
// Spray painting runs every logic tick; bullets fire on input.
using UnityEngine;

namespace PaintGame
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponConfigSO _config;
        public WeaponConfigSO Config => _config;

        protected PlayerStats    _stats;
        protected TerritoryMap   _map;
        protected PoolRegistry   _pool;

        // Fire-rate cooldown (seconds)
        private float _fireTimer;

        public virtual void Init(PlayerStats stats, TerritoryMap map, PoolRegistry pool)
        {
            _stats = stats;
            _map   = map;
            _pool  = pool;
        }

        // ── Called each logic tick (20 Hz) ────────────────────────────────────
        public void SprayTick(float dt)
        {
            if (!_stats.Alive || !_stats.IsShooting || _stats.Ink <= 0f) return;

            var tiles = SprayCone.GetConeTiles(
                _stats.WorldPos.x, _stats.WorldPos.y,
                _stats.AimAngle,
                _config.sprayRange,
                _config.sprayHalfAngle);

            foreach (var t in tiles)
                _map.Paint(t.x, t.y, _stats.OwnerIndex);

            float drain = _config.inkDrainPerSec * _stats.DrainMultiplier * dt;
            _stats.DrainInk(drain);
        }

        // ── Called each logic tick — check fire input ─────────────────────────
        public void FireTick(float dt)
        {
            _fireTimer -= dt;
            if (_fireTimer > 0f) return;
            if (!_stats.Alive || !_stats.WantsToShoot) return;

            _fireTimer = 1f / _config.fireRate;
            FireBurst(_stats.WorldPos, _stats.AimAngle, _stats.OwnerIndex, _stats.PlayerColor);
        }

        protected abstract void FireBurst(Vector2 origin, float aimAngle, byte ownerIndex, Color color);

        protected void RegisterSpawnedBullet(BulletProjectile bullet)
        {
            if (bullet == null) return;
            GameManager.Instance?.MatchManager?.RegisterBullet(bullet);
        }

        // ── Ink refill ────────────────────────────────────────────────────────
        public void RefillTick(float dt)
        {
            if (_stats.Ink >= GameConstants.INK_MAX) return;
            if (_stats.IsShooting) return;

            float refill = GameConstants.INK_REFILL_PER_SEC * _stats.RefillMultiplier * dt;
            _stats.RefillInk(refill);
        }
    }
}
