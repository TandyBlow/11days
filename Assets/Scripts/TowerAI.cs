// TowerAI.cs
using UnityEngine;
using System.Collections.Generic; // --- 新增引用 ---
using System.Linq; // --- 新增引用 ---

public class TowerAI : MonoBehaviour
{
    private Health targetMonster;
    private float attackTimer;
    private float attackRange = 10f;

    void Update()
    {
        // --- 修改开始：增加对目标是否存活的更严谨判断 ---
        // 如果目标被摧毁 (gameObject 变为 null)，或者其 Health 组件被禁用/移除
        if (targetMonster != null && (targetMonster.gameObject == null || !targetMonster.enabled))
        {
            targetMonster = null;
        }
        // --- 修改结束 ---

        if (targetMonster == null)
        {
            FindNearestMonster();
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                Shoot();
                attackTimer = FinaleManager.Instance.towerAttackSpeed;
            }
            
            // 目标跑出范围，重新寻找
            if (Vector3.Distance(transform.position, targetMonster.transform.position) > attackRange)
            {
                targetMonster = null;
            }
        }
    }

    void FindNearestMonster()
    {
        // --- 修改开始：修正寻找怪物的逻辑 ---
        Health closest = null;
        float minDistance = attackRange; // 只在攻击范围内寻找

        // 从 FinaleManager 获取当前所有存活的怪物列表
        List<MonsterAI> activeMonsters = FinaleManager.Instance.activeMonsters;
        
        // 如果没有怪物，直接返回
        if (activeMonsters == null || activeMonsters.Count == 0)
        {
            targetMonster = null;
            return;
        }

        foreach (var monsterAI in activeMonsters)
        {
            // 确保怪物和其Health组件都有效
            if (monsterAI == null || !monsterAI.TryGetComponent<Health>(out var monsterHealth)) continue;

            float distance = Vector3.Distance(transform.position, monsterHealth.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = monsterHealth;
            }
        }
        
        targetMonster = closest;
        // --- 修改结束 ---
    }

    void Shoot()
    {
        // --- 修改开始：修正获取子弹脚本的方式 ---
        if (targetMonster == null) return; // 安全检查

        GameObject projGO = Instantiate(FinaleManager.Instance.projectilePrefab, transform.position, Quaternion.identity);
        
        // 使用 GetComponent 而不是 AddComponent
        Projectile projectile = projGO.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(targetMonster.transform, FinaleManager.Instance.towerDamage);
        }
        else
        {
            // 如果Prefab上没有挂脚本，这是一个备用方案，但强烈建议在编辑器里挂好
            Debug.LogError("子弹Prefab上没有找到Projectile脚本，正在动态添加！");
            projectile = projGO.AddComponent<Projectile>();
            projectile.Initialize(targetMonster.transform, FinaleManager.Instance.towerDamage);
        }
        // --- 修改结束 ---
    }
}
