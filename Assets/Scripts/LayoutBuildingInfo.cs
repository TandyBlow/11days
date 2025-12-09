// LayoutBuildingInfo.cs
using UnityEngine;

// 这个组件只在编辑器模式下有用，用来告诉布局系统这个GameObject对应哪个BuildingData
public class LayoutBuildingInfo : MonoBehaviour
{
    public BuildingData buildingData;
}
