using UnityEngine;

namespace UI.UIControllers
{
    public class SelectionBoxController : MonoBehaviour
    {
        private const float SIZE_MULTIPLIER = 0.5F;

        [SerializeField]
        private RectTransform _parentTransform;
        
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
            Vector2 convertedStartingPosition = GetConvertedPosition(startingPosition);
            Vector2 convertedCurrentPosition = GetConvertedPosition(currentPosition);
            Vector2 size = convertedCurrentPosition - convertedStartingPosition;
            _boxTransform.anchoredPosition = GetAnchoredPosition(convertedStartingPosition, size);
            _boxTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private Vector2 GetAnchoredPosition(Vector2 convertedStartingPosition, Vector2 size)
        {
            return convertedStartingPosition + size * SIZE_MULTIPLIER;
        }

        private Vector2 GetConvertedPosition(Vector2 startingPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentTransform,
                startingPosition,
                null,
                out Vector2 localPoint
            );

            return localPoint;
        }
    }
}