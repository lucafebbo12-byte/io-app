// BulletProjectile.cs — pooled bullet. No Rigidbody2D.
// Logic position updates at 20 Hz; visual position interpolates in Update().
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletProjectile : MonoBehaviour
    {
        private static Sprite _runtimeBulletSprite;
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
            EnsureVisualSetup();
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
            transform.localScale = new Vector3(4.5f, 1.8f, 1f);
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

        private void EnsureVisualSetup()
        {
            if (_sr == null) return;
            _sr.sortingOrder = 12;
            if (_sr.sprite == null)
                _sr.sprite = GetRuntimeBulletSprite();
        }

        private static Sprite GetRuntimeBulletSprite()
        {
            if (_runtimeBulletSprite != null) return _runtimeBulletSprite;

            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var rx = size * 0.42f;
            var ry = size * 0.24f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / rx;
                    float ny = (y - center) / ry;
                    float d = nx * nx + ny * ny;
                    if (d <= 1f)
                    {
                        float a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            _runtimeBulletSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
            return _runtimeBulletSprite;
        }
    }
}
