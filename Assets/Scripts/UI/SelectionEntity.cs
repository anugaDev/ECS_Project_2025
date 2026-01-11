public class SelectionEntity
{
    private int _id;

    private int _type;

    public int Id => _id;
    
    public int Type => _type;

    public SelectionEntity(int id, int type)
    {
        _id = id;
        _type = type;
    }
}