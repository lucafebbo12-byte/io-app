// VirtualJoystick.cs — mobile dual-stick control. Draggable knob within a fixed bg.
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaintGame
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _knob;
        [SerializeField] private float         _maxRadius = 60f;

        public Vector2 Direction { get; private set; }
        private int    _pointerId = -1;

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != -1) return;
            _pointerId = e.pointerId;
            MoveKnob(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            MoveKnob(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _pointerId = -1;
            Direction  = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }

        private void MoveKnob(Vector2 screenPos)
        {
            if (_background == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, screenPos, null, out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _maxRadius);
            if (_knob != null) _knob.anchoredPosition = clamped;

            Direction = clamped / _maxRadius;
        }
    }
}
