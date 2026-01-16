using System;
using Types;
using Unity.Entities;

namespace Buildings
{
    public class RecruitmentEntity
    {
        private float _currentTime;

        private float _recruitmentTime;

        private Entity _entity;

        private UnitType _unit;

        public Action<Entity, UnitType> OnFinishedAction;
        
        
        public RecruitmentEntity(float recruitmentTime, Entity entity)
        {
            _recruitmentTime = recruitmentTime;
            _entity = entity;
        }

        public void Update(float deltaTime)
        {
            _currentTime += deltaTime;
            if(_currentTime >= _recruitmentTime)
            {
                OnFinishedAction?.Invoke(_entity, _unit);
            }
        }
    }
}