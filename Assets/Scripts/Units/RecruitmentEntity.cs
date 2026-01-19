using System;
using Types;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class RecruitmentEntity
    {
        private float _currentTime;

        private float _recruitmentTime;

        private Entity _entity;

        private UnitType _unit;

        public Action<Entity, UnitType, RecruitmentEntity> OnFinishedAction;
        
        private bool _eventCalled;

        public Entity Entity => _entity;

        public RecruitmentEntity(float recruitmentTime, Entity entity, UnitType unitType)
        {
            _recruitmentTime = recruitmentTime;
            _entity = entity;
            _unit = unitType;
        }

        public void Update(float deltaTime)
        {
            _currentTime += deltaTime;
            if(_currentTime >= _recruitmentTime && !_eventCalled)
            {
                FinishedRecruitmentEvent();
            }
        }

        private void FinishedRecruitmentEvent()
        {
            OnFinishedAction?.Invoke(_entity, _unit, this);
            _eventCalled = true;
        }

        public bool IsSameEntity(Entity entity)
        {
            return _entity.Index == entity.Index;
        }
    }
}