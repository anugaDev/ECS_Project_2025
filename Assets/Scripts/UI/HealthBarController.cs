using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBarController : MonoBehaviour
    {
        [SerializeField]
        private Slider _healthBarSlider;
        
        [SerializeField]
        private Canvas _canvas; 

        public void UpdateHealthBar(int curHitPoints, int maxHitPoints)
        {
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = maxHitPoints;
            _healthBarSlider.value = curHitPoints;
        }

        public void Enable()
        {
            _canvas.enabled = true;
        }

        public void Disable()
        {
            _canvas.enabled = false;;
        }
    }
}