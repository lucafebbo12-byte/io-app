// AKWeapon.cs — single precise bullet per shot.
using UnityEngine;

namespace PaintGame
{
    public class AKWeapon : WeaponBase
    {
        protected override void FireBurst(Vector2 origin, float aimAngle, byte ownerIndex, Color color)
        {
            float spread = _config.bulletSpreadDeg * Mathf.Deg2Rad;
            float angle  = aimAngle + Random.Range(-spread, spread);
            Vector2 dir  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _pool.SpawnSplat(origin + dir * 8f, color);
            var bullet   = _pool.GetBullet();
            bullet.Init(origin, angle, ownerIndex, color,
                        _config.bulletSpeed, _config.bulletDamage);
            RegisterSpawnedBullet(bullet);
        }
    }
}
