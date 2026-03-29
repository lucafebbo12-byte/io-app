// WeaponConfigSO.cs — ScriptableObject for weapon/class parameters.
// Create two instances: Shotgun and AK.
using UnityEngine;

namespace PaintGame
{
    [CreateAssetMenu(menuName = "PaintGame/WeaponConfig", fileName = "WeaponConfig")]
    public class WeaponConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Shotgun";
        public Sprite weaponSprite;

        [Header("Movement")]
        [Tooltip("Multiplier applied on top of base PLAYER_SPEED")]
        public float moveSpeedMultiplier = 1.0f;

        [Header("Spray Cone")]
        [Tooltip("Half-angle of spray cone in radians")]
        public float sprayHalfAngle  = GameConstants.SPRAY_HALF_ANGLE_RAD;
        public float sprayRange      = GameConstants.SPRAY_RANGE;

        [Header("Shooting")]
        public int   bulletsPerShot  = 1;
        [Tooltip("Random per-bullet angle offset in degrees")]
        public float bulletSpreadDeg = 0f;
        [Tooltip("Shots per second")]
        public float fireRate        = 5f;

        [Header("Ink")]
        [Tooltip("Ink drained per second while spraying")]
        public float inkDrainPerSec  = GameConstants.INK_DRAIN_PER_SEC;

        [Header("Bullet")]
        public float bulletDamage    = 1f;
        public float bulletSpeed     = GameConstants.BULLET_SPEED;

        // ── Preset helpers ────────────────────────────────────────────────────
#if UNITY_EDITOR
        [ContextMenu("Set Shotgun Defaults")]
        void SetShotgunDefaults()
        {
            weaponName         = "Shotgun";
            moveSpeedMultiplier= 1.15f;
            sprayHalfAngle     = 36f * Mathf.Deg2Rad;
            sprayRange         = 160f;
            bulletsPerShot     = 5;
            bulletSpreadDeg    = 18f;
            fireRate           = 2.5f;
            inkDrainPerSec     = 80f;
        }

        [ContextMenu("Set AK Defaults")]
        void SetAKDefaults()
        {
            weaponName         = "AK";
            moveSpeedMultiplier= 0.90f;
            sprayHalfAngle     = 15f * Mathf.Deg2Rad;
            sprayRange         = 280f;
            bulletsPerShot     = 1;
            bulletSpreadDeg    = 3f;
            fireRate           = 6f;
            inkDrainPerSec     = 40f;
        }
#endif
    }
}
