using UnityEngine;

namespace UI.UIControllers
{
    public class SelectionBoxController : MonoBehaviour
    {
        private const float DEFAULT_BOX_SCALE = 1;
        
        [SerializeField]
        private RectTransform _boxTransform;
        
        [SerializeField]
        private Canvas _canvas;
        
        public void Enable()
        {
            _canvas.enabled = true;
        }

        public void Disable()
        {
            _canvas.enabled = false;
        }

        public void UpdateBoxSize(Vector2 startingPosition, Vector2 currentPosition)
        {
            Vector2 size = currentPosition - startingPosition;
            _boxTransform.anchoredPosition = startingPosition;
            _boxTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            FlipBoxIfNeeded(size);
        }

        private void FlipBoxIfNeeded(Vector2 size)
        {
            float newSizeX = GetFlippedAxis(size.x);
            float newSizeY = GetFlippedAxis(size.y);
            _boxTransform.localScale = new Vector3(newSizeX, newSizeY, DEFAULT_BOX_SCALE);
        }

        private float GetFlippedAxis(float axis)
        {
            if (axis < 0)
            {
                return -DEFAULT_BOX_SCALE;
            }

            return DEFAULT_BOX_SCALE;
        }
    }
}