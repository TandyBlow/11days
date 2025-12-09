using UnityEngine;

public enum BuildingType{Residential, Farmland, Commercial, Decoration, Road}

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    // public Sprite icon;
    public int cost;
    public int width = 2; // 默认值
    public int height = 2; // 默认值
    public BuildingType buildingType;
    public int rewards;
    public float rewardsTime;
    public int population;
    public int goodsNeeded;
    public int unlockDay = 1;
    public bool isTrueVersion = false;
    public bool isSpecialReplacement = false;
    
}