using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class ActionCostPopUpView : MonoBehaviour
{
    [SerializeField]
    private GameObject _gameObject;

    [SerializeField]
    private RectTransform _rectTransform;

    [SerializeField] 
    private TextMeshProUGUI _titleText;

    [SerializeField]
    private ResourcePanelCostController _foodController; 

    [SerializeField]
    private ResourcePanelCostController _woodcontroller; 

    [SerializeField]
    private ResourcePanelCostController _populationController;

    public void SetTitleText(string title)
    {
        _titleText.text = title;
    }

    public void Enable()
    {
        _gameObject.SetActive(true);
    }

    public void Disable()
    {
        _gameObject.SetActive(false);
    }

    private void SetPosition(float3 position)
    {
        _rectTransform.anchoredPosition = new Vector2(position.x, position.y);
    }
    
    public void SetCostTexts(string food, string wood, string population)
    {
        _foodController.SetText(food);
        _woodcontroller.SetText(wood);
        _populationController.SetText(population);
    }
}