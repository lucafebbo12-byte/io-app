// PlayerVisuals.cs — visual-only effects. No game logic.
using UnityEngine;

namespace PaintGame
{
    public class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Transform      _gunPivot;
        [SerializeField] private SpriteRenderer _gunRenderer;
        [SerializeField] private SpriteRenderer _glowSprite;

        private PlayerStats _stats;
        private float       _bobTimer;
        private bool        _blinkActive;
        private float       _blinkTimer;
        private Coroutine   _hitFlashCoroutine;

        // Zone tint: own=slightly brighter, enemy=slightly darker
        private static readonly Color TINT_OWN    = new Color(1.1f, 1.1f, 1.1f, 1f);
        private static readonly Color TINT_ENEMY  = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color TINT_NORMAL = Color.white;

        public void Init(PlayerStats stats)
        {
            _stats = stats;
            if (_body        != null) _body.color        = stats.PlayerColor;
            if (_gunRenderer != null) _gunRenderer.color = stats.PlayerColor;
            if (_glowSprite  != null) { _glowSprite.color = stats.PlayerColor; _glowSprite.enabled = false; }
        }

        void Update()
        {
            if (_stats == null || !_stats.Alive) return;

            // Gun rotation follows aim angle
            if (_gunPivot != null)
                _gunPivot.rotation = Quaternion.Euler(0f, 0f, _stats.AimAngle * Mathf.Rad2Deg);

            // Walking bob
            if (_stats.MoveDir.sqrMagnitude > 0.01f)
            {
                _bobTimer += Time.deltaTime * 8f;
                float bob = Mathf.Sin(_bobTimer) * 0.5f;
                if (_body != null) _body.transform.localPosition = new Vector3(0f, bob, 0f);
            }

            // Zone tint on body
            if (_body != null)
            {
                Color tint = _stats.CurrentZone == PlayerStats.ZoneType.Own   ? TINT_OWN :
                             _stats.CurrentZone == PlayerStats.ZoneType.Enemy ? TINT_ENEMY : TINT_NORMAL;
                _body.color = _stats.PlayerColor * tint;
            }

            // Glow when on own territory
            if (_glowSprite != null)
                _glowSprite.enabled = _stats.CurrentZone == PlayerStats.ZoneType.Own;

            // Low-ink blink
            if (_stats.Ink < 15f)
            {
                _blinkTimer += Time.deltaTime * 6f;
                float alpha = Mathf.PingPong(_blinkTimer, 1f) < 0.5f ? 0.3f : 1f;
                if (_body != null)
                    _body.color = new Color(_body.color.r, _body.color.g, _body.color.b, alpha);
            }
        }

        public void FlashHit()
        {
            if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = StartCoroutine(HitFlash());
        }

        private System.Collections.IEnumerator HitFlash()
        {
            if (_body == null) yield break;
            var original = _body.color;
            _body.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            _body.color = original;
        }
    }
}
