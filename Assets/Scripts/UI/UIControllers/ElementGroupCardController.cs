using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControllers
{
    public class ElementGroupCardController : MonoBehaviour
    {
        [SerializeField] 
        private Image _image;
        
        [SerializeField] 
        private GameObject _gameObject;
        
        [SerializeField]
        private Image _progressFillImage;
        
        [SerializeField]
        private TextMeshProUGUI _countText;
        
        public void SetImage(Sprite sprite)
        {
            _image.sprite = sprite;
        }
        
        public void SetProgressFill(float progress)
        {
            _progressFillImage.fillAmount = progress;
        }
        
        public void SetCountText(int count)
        {
            _countText.text = count.ToString();
        }
        
        public void Enable()
        {
            _gameObject.SetActive(true);
        }
        
        public void Disable()
        {
            _gameObject.SetActive(false);
        }
    }
}