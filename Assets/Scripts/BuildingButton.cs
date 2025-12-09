using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour
{
    [SerializeField]
    private BuildingData buildingData;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Debug.Log("按到了");
        if (buildingData != null)
        {
            GameManager.Instance.RequestEnterBuildMode(buildingData);
        }
        else
        {
            Debug.LogError("此建筑按钮没有分配BuildingData！");
        }
    }
}