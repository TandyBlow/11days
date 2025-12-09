public class PlacedObject
{
    private BuildingData buildingData; // 对其模板的引用
    private int x, y;                  // 它在网格中的位置
    public float currentHealth;
    public int constructionStage;
    public bool isOnFire;
    private BuildingInstance instance;
    public PlacedObject(BuildingData buildingData, int x, int y, BuildingInstance instance)
    {
        this.buildingData = buildingData;
        this.x = x;
        this.y = y;
        // 初始化实例数据
        this.currentHealth = 100f; // 假设满血
        this.constructionStage = 0;
        this.isOnFire = false;
        this.instance = instance;
    }
    // 提供方法来获取其模板数据
    public BuildingData GetBuildingData()
    {
        return buildingData;
    }

    public bool IsOrigin(int x, int y)
    {
        return x == this.x && y == this.y;
    }
    // 获取保存数据
    public BuildingSaveData GetSaveData()
    {
        if (instance != null)
        {
            return instance.GetSaveData(x, y);
        }
        return null;
    }

    public int GetOriginX() { return x; }
    public int GetOriginY() { return y; }

    public BuildingInstance GetInstance()
    {
        return instance;
    }
}