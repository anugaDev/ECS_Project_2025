using Types;
using Units;
using Unity.Entities;

public struct UnitsPrefabEntityFactory
{
    private UnitPrefabComponent _prefabComponent;

    public bool IsInitialized;

    public void Set(UnitPrefabComponent prefabs)
    {
        _prefabComponent = prefabs;
        IsInitialized = true;
    }

    public Entity Get(UnitType type)
    {
        return type switch
        {
            UnitType.Ballista => _prefabComponent.Ballista,
            UnitType.Warrior => _prefabComponent.Warrior,
            UnitType.Archer => _prefabComponent.Archer,
            UnitType.Worker => _prefabComponent.Worker,
            _ => Entity.Null
        };
    }
}