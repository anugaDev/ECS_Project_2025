using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Slider _healthBarSlider;

        public void SetHealthBar(int curHitPoints, int maxHitPoints)
        {
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = maxHitPoints;
            _healthBarSlider.value = curHitPoints;
        }    
    }
}