// PurificationAura.cs
using UnityEngine;

public class PurificationAura : MonoBehaviour
{
    public float radius = 8f;
    public float purificationRate = 10f; // 每秒净化量

    void Update()
    {
        foreach (MonsterAI monster in FinaleManager.Instance.activeMonsters)
        {
            if (monster != null && Vector3.Distance(transform.position, monster.transform.position) <= radius)
            {
                monster.ApplyPurification(purificationRate * Time.deltaTime);
            }
        }
    }
}
