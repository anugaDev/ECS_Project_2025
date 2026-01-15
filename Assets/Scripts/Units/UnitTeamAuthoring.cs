using ElementCommons;
using Types;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class UnitTeamAuthoring : MonoBehaviour
    {
        [SerializeField]
        private TeamType Team;

        public class UnitTeamBaker : Baker<UnitTeamAuthoring>
        {
            public override void Bake(UnitTeamAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, GetUnitTeamComponent(authoring));
            }

            private ElementTeamComponent GetUnitTeamComponent(UnitTeamAuthoring authoring)
            {
                return new ElementTeamComponent
                {
                    Team = authoring.Team
                };
            }
        }
    }
}