using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControllers
{
    public class SelectedDetailsDisplayController : MonoBehaviour
    {
        [SerializeField]
        private GameObject _gameObject;
        
        [SerializeField]
        private TextMeshProUGUI _name;

        [SerializeField] 
        private Image _unitImage;
        
        [SerializeField]
        private TextMeshProUGUI _maxHitPoints;

        [SerializeField]
        private TextMeshProUGUI _currentHitPoints;

        [SerializeField] 
        private Image _healthPointFill;

        public void SetName(string name)
        {
            _name.text = name;
        }

        public void SetImage(Sprite sprite)
        {
            _unitImage.sprite = sprite;
        }

        public void EnableDetails()
        {
            _gameObject.SetActive(true);
        }

        public void DisableDetails()
        {
            _gameObject.SetActive(false);
        }

        public void UpdateHitPoints(int currentHitPoints, int maxHitPoints)
        {
            _maxHitPoints.text = maxHitPoints.ToString();
            _currentHitPoints.text = currentHitPoints.ToString();
            _healthPointFill.fillAmount = (float)currentHitPoints / maxHitPoints;
        }
    }
}