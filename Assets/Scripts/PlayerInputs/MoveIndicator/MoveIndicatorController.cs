using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace PlayerInputs.MoveIndicator
{
    public class MoveIndicatorController : MonoBehaviour
    {
        private const string MATERIAL_TIME_NAME = "_LocalTime";
       
        private const string MATERIAL_SPEED_NAME = "_Speed";

        [SerializeField] 
        private GameObject _gameObject;

        [SerializeField] 
        private Transform _transform;

        [SerializeField] 
        private Renderer _renderer;

        [SerializeField] 
        private float _lifetime;

        [SerializeField] 
        private float _speed;

        private Material _material;

        private Coroutine _currentTimer;

        private bool _isPlayingTimer;

        private float _localTime;

        private void Awake()
        {
            _material = _renderer.material;
            _material.SetFloat(MATERIAL_SPEED_NAME, _speed);
        }

        public void Set(float3 spawnPosition)
        {
            _localTime = 0;
            _transform.position = spawnPosition;
            _gameObject.SetActive(true);
            if (_isPlayingTimer)
            {
                StopCoroutine(_currentTimer);
            }

            _currentTimer = StartCoroutine(DisableAfterTime());
        }

        private IEnumerator DisableAfterTime()
        {
            _isPlayingTimer = true;
            yield return new WaitForSeconds(_lifetime);
            _gameObject.SetActive(false);
            _isPlayingTimer = false;
        }

        private void Update()
        {
            _localTime += Time.deltaTime;
            _material.SetFloat(MATERIAL_TIME_NAME, _localTime);
        }
    }
}