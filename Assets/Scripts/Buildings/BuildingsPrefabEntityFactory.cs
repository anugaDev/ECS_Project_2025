using Buildings;
using Types;
using Unity.Entities;

public struct BuildingsPrefabEntityFactory
{
    private BuildingPrefabComponent _prefabComponent;

    public bool IsInitialized;

    public void Set(BuildingPrefabComponent prefabs)
    {
        _prefabComponent = prefabs;
        IsInitialized = true;
    }

    public Entity Get(BuildingType type)
    {
        return type switch
        {
            BuildingType.Center => _prefabComponent.TownCenter,
            BuildingType.Barracks => _prefabComponent.Barracks,
            BuildingType.House => _prefabComponent.House,
            BuildingType.Farm => _prefabComponent.Farm,
            _ => Entity.Null
        };
    }
}