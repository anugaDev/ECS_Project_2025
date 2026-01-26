using System.Collections.Generic;
using Types;

namespace GatherableResources
{
    public class ElementResourceCostPolicy
    {
        private Dictionary<ResourceType, int> _currentResources;

        private int _maxPopulation;

        public Dictionary<ResourceType, int> CurrentResources => _currentResources;

        public ElementResourceCostPolicy()
        {
            _currentResources = new Dictionary<ResourceType, int>
            {
                [ResourceType.Wood] = 0,
                [ResourceType.Food] = 0,
                [ResourceType.Population] = 0
            };
        }

        public void UpdateCost(int woodCost, int foodCost, int populationCost, int maxPopulation)
        {
            _currentResources[ResourceType.Wood] = woodCost;
            _currentResources[ResourceType.Food] = foodCost;
            _currentResources[ResourceType.Population] = populationCost;
            _maxPopulation = maxPopulation;
        }
 
        public bool Get(ResourceCostEntity resourceCost)
        {
            if (resourceCost.ResourceType == ResourceType.None)
            {
                return true;
            }

            return IsAvailableResource(resourceCost);
        }

        private bool IsAvailableResource(ResourceCostEntity resourceCost)
        {
            if (resourceCost.ResourceType == ResourceType.Population)
            {
                return _currentResources[resourceCost.ResourceType] + resourceCost.Cost <= _maxPopulation;
            }
            else
            { 
                return _currentResources[resourceCost.ResourceType] >= resourceCost.Cost;
            }

        }

        public void AddCost(ResourceCostEntity resourceCost)
        {
            if (resourceCost.ResourceType == ResourceType.Population)
            {
                _currentResources[resourceCost.ResourceType] += resourceCost.Cost;
            }
            else
            { 
                _currentResources[resourceCost.ResourceType] -= resourceCost.Cost;
            }
        }
    }
}