// PurificationAura.cs
using System.Collections.Generic;
using UnityEngine;

public class PurificationAura : MonoBehaviour
{
    public float radius = 8f;
    public float purificationRate = 10f; // 每秒净化量

    void Update()
    {
        FinaleManager finaleManager = FinaleManager.Instance;
        if (finaleManager == null) return;

        List<MonsterAI> monsters = finaleManager.activeMonsters;

        // 倒序遍历：ApplyPurification 可能触发 Purify()，进而从 activeMonsters 中移除当前元素，
        // 正序 foreach 会抛 InvalidOperationException（集合已被修改）。
        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            MonsterAI monster = monsters[i];
            if (monster == null) continue;

            if (Vector3.Distance(transform.position, monster.transform.position) <= radius)
            {
                monster.ApplyPurification(purificationRate * Time.deltaTime);
            }
        }
    }
}
