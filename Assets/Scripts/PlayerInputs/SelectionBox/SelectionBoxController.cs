using UnityEngine;

namespace PlayerInputs.SelectionBox
{
    public class SelectionBoxController : MonoBehaviour
    {
        private const float DEFAULT_BOX_SCALE = 1;
     
        public static SelectionBoxController Instance; //TEMP

        [SerializeField]
        private RectTransform _boxTransform;
        
        [SerializeField]
        private Canvas _canvas;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Disable();
        }

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

        public Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform,
                screenPos, null, out Vector2 localPoint);
            return localPoint;
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