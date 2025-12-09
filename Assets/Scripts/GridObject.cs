// GridObject.cs
public class GridObject
{
    private GridSystem grid;
    public int x;
    public int y;
    private PlacedObject placedObject;

    public bool isBuildable { get; private set; } // 新增：标记此格子本身是否可建造

    // 修改：构造函数增加 isBuildable 参数
    public GridObject(GridSystem grid, int x, int y, bool isBuildable)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
        this.isBuildable = isBuildable; // 新增：初始化可建造属性
    }

    public void SetPlacedObject(PlacedObject placedObject)
    {
        this.placedObject = placedObject;
    }

    public void ClearPlacedObject()
    {
        this.placedObject = null;
    }

    public PlacedObject GetPlacedObject()
    {
        return placedObject;
    }

    // 修改：CanBuild的逻辑现在需要同时检查格子本身是否可建，以及是否已被占用
    public bool CanBuild()
    {
        return isBuildable && placedObject == null;
    }

    public override string ToString()
    {
        return x + "," + y + (isBuildable ? " (Buildable)" : " (Not Buildable)");
    }
}
