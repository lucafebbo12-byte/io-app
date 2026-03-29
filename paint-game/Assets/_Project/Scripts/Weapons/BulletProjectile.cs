// BulletProjectile.cs — pooled bullet. No Rigidbody2D.
// Logic position updates at 20 Hz; visual position interpolates in Update().
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletProjectile : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────────
        public  byte    OwnerIndex  { get; private set; }
        private Color   _color;
        private Vector2 _velocity;
        private float   _damage;
        private float   _lifetime;
        private bool    _active;

        // For visual interpolation between logic ticks
        private Vector2 _logicPos;
        private Vector2 _renderPos;

        private SpriteRenderer _sr;
        private TerritoryMap   _map;
        private PoolRegistry   _pool;

        // Set by PoolRegistry after instantiation
        public void SetDependencies(TerritoryMap map, PoolRegistry pool)
        {
            _map  = map;
            _pool = pool;
            _sr   = GetComponent<SpriteRenderer>();
        }

        public void Init(Vector2 origin, float aimAngle, byte ownerIndex, Color color,
                         float speed, float damage)
        {
            OwnerIndex = ownerIndex;
            _color     = color;
            _damage    = damage;
            _lifetime  = GameConstants.BULLET_MAX_LIFETIME;
            _active    = true;

            _logicPos  = origin;
            _renderPos = origin;
            _velocity  = new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle)) * speed;

            transform.position = new Vector3(origin.x, origin.y, 0f);
            transform.rotation = Quaternion.Euler(0, 0, aimAngle * Mathf.Rad2Deg);

            if (_sr != null)
            {
                _sr.color   = color;
                _sr.enabled = true;
            }
            gameObject.SetActive(true);
        }

        // ── Logic tick (called by MatchManager at 20 Hz) ──────────────────────
        public bool LogicStep(float dt, System.Collections.Generic.List<PlayerController> players)
        {
            if (!_active) return false;

            _lifetime -= dt;
            if (_lifetime <= 0f) { ReturnToPool(); return true; }

            // Advance
            _logicPos += _velocity * dt;

            // Wall check
            var tile = GameConstants.WorldToTile(_logicPos.x, _logicPos.y);
            if (_map.IsWall(tile.x, tile.y))
            {
                OnImpact(_logicPos);
                return true;
            }

            // Out of world bounds
            if (_logicPos.x < 0 || _logicPos.x > GameConstants.WORLD_W ||
                _logicPos.y < 0 || _logicPos.y > GameConstants.WORLD_H)
            {
                ReturnToPool();
                return true;
            }

            // Player collision
            foreach (var p in players)
            {
                if (!p.Stats.Alive) continue;
                if (p.Stats.OwnerIndex == OwnerIndex) continue;
                if (Vector2.Distance(p.Stats.WorldPos, _logicPos) < GameConstants.BULLET_HIT_RADIUS)
                {
                    p.TakeDamage(_damage, OwnerIndex);
                    OnImpact(_logicPos);
                    return true;
                }
            }

            return false;
        }

        // ── Visual interpolation (60 fps) ─────────────────────────────────────
        void Update()
        {
            if (!_active) return;
            // Extrapolate toward next logic position
            _renderPos = Vector2.MoveTowards(_renderPos, _logicPos,
                _velocity.magnitude * Time.deltaTime * 1.1f);
            transform.position = new Vector3(_renderPos.x, _renderPos.y, 0f);
        }

        // ── Impact ─────────────────────────────────────────────────────────────
        private void OnImpact(Vector2 pos)
        {
            // Paint a small circle at impact
            var tile = GameConstants.WorldToTile(pos.x, pos.y);
            _map.PaintCircle(tile.x, tile.y, GameConstants.BULLET_PAINT_RADIUS, OwnerIndex);

            // FX
            GameEvents.RaiseTilesPainted(null);  // just signal renderer to flush
            _pool.SpawnImpactFlash(pos, _color);

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _active = false;
            if (_sr != null) _sr.enabled = false;
            _pool.ReturnBullet(this);
        }
    }
}
