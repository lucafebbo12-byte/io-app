// PlayerVisuals.cs — visual-only effects. No game logic.
using UnityEngine;

namespace PaintGame
{
    public class PlayerVisuals : MonoBehaviour
    {
        private static readonly Sprite[] _shapeSprites = new Sprite[7]; // indices 1-6

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
            int idx = Mathf.Clamp(stats.OwnerIndex, 1, 6);
            if (_shapeSprites[idx] == null)
                _shapeSprites[idx] = BuildShapeSprite(idx);
            if (_body != null)
            {
                _body.sprite = _shapeSprites[idx];
                _body.color  = stats.PlayerColor;
            }
            if (_gunRenderer != null) _gunRenderer.color = stats.PlayerColor;
            if (_glowSprite  != null)
            {
                if (_glowSprite.sprite == null) _glowSprite.sprite = _shapeSprites[idx];
                _glowSprite.color   = stats.PlayerColor;
                _glowSprite.enabled = false;
            }
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

        private static Sprite BuildShapeSprite(int ownerIndex)
        {
            const int size = 64;
            float center = (size - 1) * 0.5f;
            float r = size * 0.44f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center) / r;
                float ny = (y - center) / r;
                bool inside = IsInsideShape(ownerIndex, nx, ny);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
        }

        private static bool IsInsideShape(int idx, float nx, float ny)
        {
            switch (idx)
            {
                case 1: // Circle
                    return nx * nx + ny * ny <= 1f;

                case 2: // Rounded square (superellipse p=4)
                {
                    float ax = Mathf.Abs(nx), ay = Mathf.Abs(ny);
                    return ax * ax * ax * ax + ay * ay * ay * ay <= 1f;
                }

                case 3: // Wide oval
                    return (nx / 1.4f) * (nx / 1.4f) + ny * ny <= 1f;

                case 4: // Triangle pointing right, vertices: (1,0), (-0.8, 0.8), (-0.8, -0.8)
                {
                    float ax=1f, ay=0f, bx=-0.8f, by=0.8f, cx=-0.8f, cy=-0.8f;
                    float d1 = (nx - bx) * (ay - by) - (ax - bx) * (ny - by);
                    float d2 = (nx - cx) * (by - cy) - (bx - cx) * (ny - cy);
                    float d3 = (nx - ax) * (cy - ay) - (cx - ax) * (ny - ay);
                    bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                    bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                    return !(hasNeg && hasPos);
                }

                case 5: // 4-lobe star
                {
                    float angle = Mathf.Atan2(ny, nx);
                    float dist  = Mathf.Sqrt(nx * nx + ny * ny);
                    float wobble = 1f + 0.22f * Mathf.Cos(4f * angle);
                    return dist <= wobble * 0.9f;
                }

                case 6: // Tall oval
                    return nx * nx + (ny / 1.4f) * (ny / 1.4f) <= 1f;

                default:
                    return nx * nx + ny * ny <= 1f;
            }
        }
    }
}
