// CameraController.cs — manages Cinemachine camera behaviour.
// Death zoom-out, respawn zoom-in, screen shake via CinemachineImpulseSource.
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _vcam;
        [SerializeField] private float _normalOrthoSize  = 9f;
        [SerializeField] private float _deadOrthoSize    = 14f;
        [SerializeField] private float _zoomSpeed        = 3f;

        private CinemachineImpulseSource _impulse;
        private float _targetOrthoSize;

        void Awake()
        {
            _impulse         = GetComponent<CinemachineImpulseSource>();
            _targetOrthoSize = _normalOrthoSize;
        }

        void OnEnable()
        {
            GameEvents.OnPlayerDied      += OnPlayerDied;
            GameEvents.OnPlayerRespawned += OnPlayerRespawned;
            GameEvents.OnCheckpointDamaged += OnCheckpointDamaged;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerDied      -= OnPlayerDied;
            GameEvents.OnPlayerRespawned -= OnPlayerRespawned;
            GameEvents.OnCheckpointDamaged -= OnCheckpointDamaged;
        }

        void Update()
        {
            if (_vcam == null) return;

            if (_vcam.Follow == null &&
                PlayerSpawnManager.Players != null &&
                PlayerSpawnManager.Players.Length > 0 &&
                PlayerSpawnManager.Players[0] != null)
            {
                _vcam.Follow = PlayerSpawnManager.Players[0].transform;
            }

            var lens = _vcam.Lens;
            lens.OrthographicSize = Mathf.Lerp(
                lens.OrthographicSize, _targetOrthoSize, Time.deltaTime * _zoomSpeed);
            _vcam.Lens = lens;
        }

        private void OnPlayerDied(PlayerController p)
        {
            if (!p.Stats.IsHuman) return;
            _targetOrthoSize = _deadOrthoSize;
            Shake(0.3f);
        }

        private void OnPlayerRespawned(PlayerController p)
        {
            if (!p.Stats.IsHuman) return;
            _targetOrthoSize = _normalOrthoSize;
        }

        private void OnCheckpointDamaged(CheckpointController cp, float hpPct)
        {
            // Shake harder the lower the HP
            Shake(Mathf.Lerp(0.05f, 0.25f, 1f - hpPct));
        }

        public void Shake(float force)
        {
            _impulse?.GenerateImpulse(force);
        }
    }
}
