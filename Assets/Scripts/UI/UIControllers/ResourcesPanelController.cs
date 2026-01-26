using TMPro;
using UnityEngine;

namespace UI.UIControllers
{
    public class ResourcesPanelController : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _foodText;
        
        [SerializeField]
        private TextMeshProUGUI _woodText;
        
        [SerializeField]
        private TextMeshProUGUI _populationText;
        
        public void SetFoodText(int food)
        {
            _foodText.text = food.ToString();
        }
        
        public void SetWoodText(int wood)
        {
            _woodText.text = wood.ToString();
        }
        
        public void SetPopulationText(int currentPopulation, int maxPopulation)
        {
            _populationText.text = currentPopulation + "/" + maxPopulation;
        }
    }
}