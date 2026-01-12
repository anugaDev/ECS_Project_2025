using System.Collections.Generic;
using Buildings;
using Types;
using Unity.Entities;

public class BuildingsPrefabEntityFactory
{
    public Dictionary<BuildingType, Entity> _buildingPrefabs;

    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public BuildingsPrefabEntityFactory()
    {
        _isInitialized = false;
    }

    public void Set(BuildingPrefabComponent prefabComponent)
    {
        _isInitialized = true;

        _buildingPrefabs = new Dictionary<BuildingType, Entity>
        {
            [BuildingType.Center] = prefabComponent.TownCenter,
            [BuildingType.Barracks] = prefabComponent.Barracks,
            [BuildingType.House] = prefabComponent.House,
            [BuildingType.Farm] = prefabComponent.Farm
        };
    }

    public Entity Get(BuildingType buildingType)
    {
        return _buildingPrefabs[buildingType];
    }
}