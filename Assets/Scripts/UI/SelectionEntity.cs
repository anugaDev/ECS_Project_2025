using Unity.Entities;

public class SelectionEntity
{
    private int _id;

    private int _type;

    private Entity _selectedEntity;

    public int Id => _id;
    
    public int Type => _type;

    public Entity SelectedEntity => _selectedEntity;

    public SelectionEntity(int id, int type, Entity selectedEntity)
    {
        _id = id;
        _type = type;
        _selectedEntity = selectedEntity;
    }
}