using System;
using TMPro;
using UnityEngine;

[Serializable]
public class ResourcePanelCostController
{
    [SerializeField]
    private GameObject _gameObject;

    [SerializeField]
    private TextMeshProUGUI _text;

    public void SetText(string text)
    {
        _text.text = text;
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
