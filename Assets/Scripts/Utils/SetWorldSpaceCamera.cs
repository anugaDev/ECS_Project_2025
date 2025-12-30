using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetWorldSpaceCamera : MonoBehaviour
{
    [SerializeField] 
    private Canvas _canvas;
    void Awake()
    {
        _canvas.worldCamera = Camera.main;
    }
}
