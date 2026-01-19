using System.Collections.Generic;
using ScriptableObjects;
using Types;
using UnityEngine;

namespace UI.UIControllers
{
    public class SelectedGroupDisplayController : MonoBehaviour
    {
        [SerializeField] 
        private RectTransform _gridTransform;
        
        [SerializeField] 
        private ElementGroupCardController _cardPrefab;
        
        private Dictionary<UnitType, ElementGroupCardController> _cardsDictionary;
        
        private void Awake()
        {
            _cardsDictionary = new Dictionary<UnitType, ElementGroupCardController>();
        }
        
        public void SetUnitsGroups(UnitsScriptableObject configuration)
        {
            Dictionary<UnitType, UnitScriptableObject> unitScriptableObjects = configuration.GetUnitsDictionary();
            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                SetGroup(unitType, unitScriptableObjects[unitType]);
            }
        }

        private void SetGroup(UnitType unitType, UnitScriptableObject unitScriptableObject)
        {
            ElementGroupCardController newCard = Instantiate(_cardPrefab, _gridTransform);
            newCard.SetImage(unitScriptableObject.Sprite);
            _cardsDictionary.Add(unitType, newCard);
        }

        public void SetGroupValue(UnitType unitType, int unitCount)
        {
            if (!_cardsDictionary.ContainsKey(unitType))
            {
                return;
            }

            EnableGroup(unitType, unitCount);
            _cardsDictionary[unitType].SetCountText(unitCount);
        }

        public void SetGroupFill(UnitType unitType, float fillAmount)
        {
            if (!_cardsDictionary.ContainsKey(unitType))
            {
                return;
            }

            _cardsDictionary[unitType].SetProgressFill(fillAmount);
        }

        private void EnableGroup(UnitType unitType, int unitCount)
        {
            if (unitCount > 0)
            {
                _cardsDictionary[unitType].Enable();
            }
            else
            {
                _cardsDictionary[unitType].Disable();
            }
        }
    }
}