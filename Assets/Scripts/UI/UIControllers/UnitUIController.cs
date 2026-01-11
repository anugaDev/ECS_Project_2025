using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UnitUIController : MonoBehaviour
    {
        [SerializeField]
        private Slider _healthBarSlider;
        
        [SerializeField]
        private Canvas _healthbarCanvas;

        [SerializeField]
        private GameObject _selectionCircle; 

        public void UpdateHealthBar(int curHitPoints, int maxHitPoints)
        {
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = maxHitPoints;
            _healthBarSlider.value = curHitPoints;
        }

        public void EnableUI()
        {
            _healthbarCanvas.enabled = true;
            _selectionCircle.SetActive(true);
        }

        public void DisableUI()
        {
            _healthbarCanvas.enabled = false;
            _selectionCircle.SetActive(false);
        }
    }
}