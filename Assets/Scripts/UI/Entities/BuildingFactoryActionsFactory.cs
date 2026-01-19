using System.Collections.Generic;
using Buildings;
using Types;

namespace UI.Entities
{
    public class BuildingFactoryActionsFactory
    {
        public Dictionary<BuildingType, PlayerUIActionType> _actionTypes;

        public Dictionary<BuildingType, int[]> _payloadIds;

        private BuildingType _currentBuildingType;

        public BuildingFactoryActionsFactory()
        {
            _actionTypes = new Dictionary<BuildingType, PlayerUIActionType>
            {
                [BuildingType.Barracks] = PlayerUIActionType.Recruit,
                [BuildingType.Center] = PlayerUIActionType.Recruit,
                [BuildingType.House] = PlayerUIActionType.None,
                [BuildingType.Farm] = PlayerUIActionType.None
            };

            _payloadIds = new Dictionary<BuildingType, int[]>
            {
                [BuildingType.Barracks] = new []{(int) UnitType.Archer, (int) UnitType.Warrior, (int) UnitType.Ballista},
                [BuildingType.Center] = new []{(int) UnitType.Worker}
            };

        }
        public void Set(BuildingType buildingTypeSelected)
        {
            _currentBuildingType = buildingTypeSelected;
        }

        public PlayerUIActionType Get()
        {
            return _actionTypes[_currentBuildingType];
        }

        public int[] GetPayload(PlayerUIActionType playerUIActionType)
        {
            if (playerUIActionType == PlayerUIActionType.None)
            {
                return new[] { -1 };
            }
            
            return _payloadIds[_currentBuildingType];
        }
    }
}