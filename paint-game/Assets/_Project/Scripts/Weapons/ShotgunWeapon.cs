// ShotgunWeapon.cs — fires a spread of bullets.
using UnityEngine;

namespace PaintGame
{
    public class ShotgunWeapon : WeaponBase
    {
        protected override void FireBurst(Vector2 origin, float aimAngle, byte ownerIndex, Color color)
        {
            int count = _config.bulletsPerShot;
            float spread = _config.bulletSpreadDeg * Mathf.Deg2Rad;

            for (int i = 0; i < count; i++)
            {
                float angle = aimAngle + Random.Range(-spread, spread);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _pool.SpawnSplat(origin + dir * 7f, color);
                var bullet = _pool.GetBullet();
                bullet.Init(origin, angle, ownerIndex, color,
                            _config.bulletSpeed, _config.bulletDamage);
                RegisterSpawnedBullet(bullet);
            }
        }
    }
}
