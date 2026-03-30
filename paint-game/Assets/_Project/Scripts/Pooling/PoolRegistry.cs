// PoolRegistry.cs — central pool manager. All systems request objects here.
using UnityEngine;

namespace PaintGame
{
    public class PoolRegistry : MonoBehaviour
    {
        private static Sprite _fallbackSprite;

        [Header("Prefabs")]
        [SerializeField] private BulletProjectile _bulletPrefab;
        [SerializeField] private SplatEffect      _splatPrefab;
        [SerializeField] private ImpactFlash      _flashPrefab;
        [SerializeField] private DeathBurst       _burstPrefab;

        [Header("Warm sizes")]
        [SerializeField] private int _bulletPoolSize = 60;
        [SerializeField] private int _splatPoolSize  = 64;
        [SerializeField] private int _flashPoolSize  = 32;
        [SerializeField] private int _burstPoolSize  = 12;

        private ObjectPool<BulletProjectile> _bullets;
        private ObjectPool<SplatEffect>      _splats;
        private ObjectPool<ImpactFlash>      _flashes;
        private ObjectPool<DeathBurst>       _bursts;

        private TerritoryMap _map;

        void Awake()
        {
            _map = Object.FindFirstObjectByType<TerritoryMap>();
            EnsureFallbackPrefabs();

            _bullets = new ObjectPool<BulletProjectile>(
                _bulletPrefab, transform, _bulletPoolSize,
                onGet: b => b.SetDependencies(_map, this));

            _splats  = new ObjectPool<SplatEffect>(_splatPrefab,  transform, _splatPoolSize);
            _flashes = new ObjectPool<ImpactFlash>(_flashPrefab,  transform, _flashPoolSize);
            _bursts  = new ObjectPool<DeathBurst>(_burstPrefab,   transform, _burstPoolSize);
        }

        // ── Bullet ────────────────────────────────────────────────────────────
        public BulletProjectile GetBullet() => _bullets.Get();
        public void ReturnBullet(BulletProjectile b) => _bullets.Return(b);

        // ── FX ───────────────────────────────────────────────────────────────
        public void SpawnSplat(Vector2 pos, Color color)
        {
            var s = _splats.Get();
            s.Play(pos, color, () => _splats.Return(s));
        }

        public void SpawnImpactFlash(Vector2 pos, Color color)
        {
            var f = _flashes.Get();
            f.Play(pos, color, () => _flashes.Return(f));
        }

        public void SpawnDeathBurst(Vector2 pos, Color color)
        {
            var b = _bursts.Get();
            b.Play(pos, color, () => _bursts.Return(b));
        }

        private void EnsureFallbackPrefabs()
        {
            if (_bulletPrefab == null)
            {
                var go = new GameObject("Bullet_Fallback");
                go.SetActive(false);
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetFallbackSprite();
                _bulletPrefab = go.AddComponent<BulletProjectile>();
            }

            if (_splatPrefab == null)
            {
                var go = new GameObject("Splat_Fallback");
                go.SetActive(false);
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetFallbackSprite();
                _splatPrefab = go.AddComponent<SplatEffect>();
            }

            if (_flashPrefab == null)
            {
                var go = new GameObject("ImpactFlash_Fallback");
                go.SetActive(false);
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetFallbackSprite();
                _flashPrefab = go.AddComponent<ImpactFlash>();
            }

            if (_burstPrefab == null)
            {
                var go = new GameObject("DeathBurst_Fallback");
                go.SetActive(false);
                go.transform.SetParent(transform);
                go.AddComponent<ParticleSystem>();
                _burstPrefab = go.AddComponent<DeathBurst>();
            }
        }

        private static Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist / radius) * 0.8f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
            return _fallbackSprite;
        }
    }
}
