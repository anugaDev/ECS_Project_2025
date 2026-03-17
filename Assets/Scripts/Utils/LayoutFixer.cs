using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LayoutFixer : MonoBehaviour
{
    [SerializeField] 
    private bool _mustFix;
    
    private bool _isShortText;

    void Update()
    {
        if (!_mustFix)
        {
            return;
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}
