// PoolRegistry.cs — central pool manager. All systems request objects here.
using UnityEngine;

namespace PaintGame
{
    public class PoolRegistry : MonoBehaviour
    {
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
            _map = FindObjectOfType<TerritoryMap>();

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
    }
}
