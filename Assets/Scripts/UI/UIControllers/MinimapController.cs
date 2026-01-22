using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.UIControllers
{
    public class MinimapController : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private const float NORMALIZED_MIDDLE_SCREEN = 0.5F;

        private const float HEIGHT_INTERPOLATOR = 100F;

        private const float MIN_SIZE = 10F;

        private const float MAX_SIZE = 40F;

        [Header("Minimap References")]
        [SerializeField] private RectTransform _minimapRect;

        [SerializeField] private RawImage _minimapImage;

        [SerializeField] private RectTransform _cameraIndicator;

        [Header("World Settings")]
        [SerializeField] private Vector2 _worldSize;

        [SerializeField] private Vector2 _worldCenter;

        [SerializeField] private float _cameraIndicatorSize = 20f;

        public event Action<Vector3> OnMinimapClicked;
        public event Action<Vector3> OnMinimapDragged;

        private bool _isDragging;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_minimapRect == null || _isDragging)
            {
                return;
            }

            SetCameraPosition(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_minimapRect == null)
            {
                return;
            }

            Vector2 pointedPosition = GetPointedPosition(eventData);
            Vector3 worldPosition = MinimapToWorldPosition(pointedPosition);
            UpdateCameraIndicatorPosition(worldPosition);
            OnMinimapDragged?.Invoke(worldPosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        private void SetCameraPosition(PointerEventData eventData)
        {
            Vector2 pointedPosition = GetPointedPosition(eventData);
            Vector3 worldPosition = MinimapToWorldPosition(pointedPosition);
            UpdateCameraIndicatorPosition(worldPosition);
            OnMinimapClicked?.Invoke(worldPosition);
        }

        private Vector2 GetPointedPosition(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _minimapRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            
            return localPoint;
        }
        public void UpdateCameraIndicatorPosition(Vector3 position)
        {
            SetAnchoredPosition(position);
        }

        private void SetAnchoredPosition(Vector3 position)
        {
            Vector2 minimapLocalPos = GetMinimapLocalPosition(position);
            Vector2 minimapSize = _minimapRect.rect.size;
            float halfIndicatorSize = _cameraIndicator.sizeDelta.x * NORMALIZED_MIDDLE_SCREEN;

            minimapLocalPos.x = GetClampedAxis(minimapLocalPos.x, minimapSize.x, halfIndicatorSize);
            minimapLocalPos.y = GetClampedAxis(minimapLocalPos.y, minimapSize.y, halfIndicatorSize);
            _cameraIndicator.anchoredPosition = minimapLocalPos;
        }

        private Vector2 GetMinimapLocalPosition(Vector3 position)
        {
            Vector3 cameraWorldPos = position;
            Vector2 cameraPos2D = new Vector2(cameraWorldPos.x, cameraWorldPos.z);
            return WorldToMinimapPosition(cameraPos2D);
        }

        private float GetClampedAxis(float localPositionAxis, float axis, float halfIndicatorSize)
        {
            return Mathf.Clamp(localPositionAxis,
                -axis * NORMALIZED_MIDDLE_SCREEN + halfIndicatorSize,
                axis * NORMALIZED_MIDDLE_SCREEN - halfIndicatorSize);
        }

        public void UpdateCameraIndicatorSize(float zoomDistance)
        {
            float normalizedZoom = Mathf.InverseLerp(HEIGHT_INTERPOLATOR, 0f, zoomDistance);
            float size = Mathf.Lerp(MIN_SIZE, MAX_SIZE, normalizedZoom);
            _cameraIndicator.sizeDelta = new Vector2(size, size);
        }

        private Vector3 MinimapToWorldPosition(Vector2 minimapLocalPos)
        {
            Vector2 minimapSize = _minimapRect.rect.size;
            Vector2 normalizedPos = new Vector2(
                (minimapLocalPos.x + minimapSize.x * NORMALIZED_MIDDLE_SCREEN) / minimapSize.x,
                (minimapLocalPos.y + minimapSize.y * NORMALIZED_MIDDLE_SCREEN) / minimapSize.y
            );
            float worldX = _worldCenter.x + (normalizedPos.x - NORMALIZED_MIDDLE_SCREEN) * _worldSize.x;
            float worldZ = _worldCenter.y + (normalizedPos.y - NORMALIZED_MIDDLE_SCREEN) * _worldSize.y;

            return new Vector3(worldX, GlobalParameters.DEFAULT_SCENE_HEIGHT, worldZ);
        }

        private Vector2 WorldToMinimapPosition(Vector2 worldPos2D)
        {
            Vector2 minimapSize = _minimapRect.rect.size;
            Vector2 normalizedPos = new Vector2(
                GetNormalizedPosition(worldPos2D.x, _worldCenter.x, _worldSize.x),
                GetNormalizedPosition(worldPos2D.y, _worldCenter.y, _worldSize.y));
            Vector2 minimapLocalPos = new Vector2(
                GetLocalAxisPosition(normalizedPos.x, minimapSize.x),
                GetLocalAxisPosition(normalizedPos.y, minimapSize.y));
            return minimapLocalPos;
        }

        private float GetNormalizedPosition(float worldPositionAxis, float worldCenterAxis, float worldSizeAxis)
        {
            return (worldPositionAxis - worldCenterAxis) / worldSizeAxis + NORMALIZED_MIDDLE_SCREEN;
        }

        private float GetLocalAxisPosition(float axisPosition, float sizeAxis)
        {
            return (axisPosition - NORMALIZED_MIDDLE_SCREEN) * sizeAxis;
        }
    }
}
