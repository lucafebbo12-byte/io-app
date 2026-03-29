// DeathBurst.cs — particle burst on player death. Uses Unity Particle System.
// Stop Action = Disable for auto-pool return.
using System;
using System.Collections;
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(ParticleSystem))]
    public class DeathBurst : MonoBehaviour
    {
        private ParticleSystem _ps;
        private Action         _onComplete;

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            var main = _ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Play(Vector2 pos, Color color, Action onComplete)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _onComplete = onComplete;

            // Tint particles to player color
            var main = _ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);

            _ps.Play();
        }

        void OnParticleSystemStopped() => _onComplete?.Invoke();
    }
}
