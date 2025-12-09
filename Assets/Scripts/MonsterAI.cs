// MonsterAI.cs (完整版)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Health))]
public class MonsterAI : MonoBehaviour
{
    // 怪物AI状态
    private enum State { Seeking, Moving, Attacking, GoToSea }
    private State currentState = State.Seeking;

    [Header("AI参数")]
    [SerializeField] private float moveSpeed = 2f;
    
    [Header("二周目机制")]
    [Tooltip("怪物被净化后生成的居民Prefab")]
    [SerializeField] private GameObject residentPrefab;

    // 内部状态变量
    private Health targetBuilding;
    private float attackTimer;
    private bool isExecutingAttack = false;

    // --- 新增：净化相关变量 ---
    private float purificationLevel = 0f;
    private bool isPurified = false;
    private SpriteRenderer spriteRenderer; // 用于净化时改变颜色

    private void Awake()
    {
        var sorter = gameObject.AddComponent<IsometricSorter>();
        sorter.Initialize(false); // 'false' 因为怪物是动态的
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 如果正在执行攻击动画或已被净化，则不执行任何逻辑
        if (isExecutingAttack || isPurified) return;

        // 检查目标建筑是否仍然存在
        if (targetBuilding != null && (targetBuilding.gameObject == null || !targetBuilding.enabled))
        {
            targetBuilding = null;
            currentState = State.Seeking;
        }
        
        // AI状态机
        switch (currentState)
        {
            case State.Seeking:
                FindNearestBuilding();
                break;
            case State.Moving:
                MoveToTarget();
                break;
            case State.Attacking:
                AttackTarget();
                break;
            case State.GoToSea: // 坏结局时，走向大海
                transform.position = Vector3.MoveTowards(transform.position, FinaleManager.Instance.seaPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, FinaleManager.Instance.seaPosition) < 1f)
                {
                    Destroy(gameObject); // 到达后消失
                }
                break;
        }
    }

    /// <summary>
    /// 寻找最近的建筑作为攻击目标
    /// </summary>
    void FindNearestBuilding()
    {
        Health closest = null;
        float minDistance = float.MaxValue;

        List<Health> buildings = FinaleManager.Instance.activeBuildings;
        if (buildings == null || buildings.Count == 0)
        {
            // 如果没有可攻击的建筑，原地待命
            currentState = State.Seeking;
            return;
        }

        // 遍历所有可攻击的建筑和塔
        foreach(var building in buildings)
        {
            if (building == null) continue;

            float distance = Vector3.Distance(transform.position, building.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = building;
            }
        }

        if (closest != null)
        {
            targetBuilding = closest;
            currentState = State.Moving;
        }
    }

    /// <summary>
    /// 向目标移动，直到进入攻击范围
    /// </summary>
    void MoveToTarget()
    {
        if (targetBuilding == null)
        {
            currentState = State.Seeking;
            return;
        }

        float attackRange = 1.0f; // 近战攻击距离

        float distanceToTarget = Vector3.Distance(transform.position, targetBuilding.transform.position);
        if (distanceToTarget > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetBuilding.transform.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            currentState = State.Attacking;
            attackTimer = FinaleManager.Instance.monsterAttackSpeed; // 准备第一次攻击
        }
    }

    /// <summary>
    /// 攻击目标
    /// </summary>
    void AttackTarget()
    {
        if (targetBuilding == null)
        {
            currentState = State.Seeking;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            StartCoroutine(LungeAttackCoroutine());
        }

        // 如果目标跑出范围，回去追
        float escapeRange = 1.2f; 
        if (Vector3.Distance(transform.position, targetBuilding.transform.position) > escapeRange)
        {
            currentState = State.Moving;
        }
    }
    
    /// <summary>
    /// 执行一个向前冲刺的攻击动画，并对目标造成伤害
    /// </summary>
    private IEnumerator LungeAttackCoroutine()
    {
        isExecutingAttack = true; // 锁定AI
        
        Vector3 originalPosition = transform.position;
        
        // 再次检查目标，防止在协程开始时目标消失
        if (targetBuilding == null)
        {
            isExecutingAttack = false;
            currentState = State.Seeking;
            yield break;
        }

        Vector3 targetPosition = targetBuilding.transform.position;
        Vector3 lungePosition = originalPosition + (targetPosition - originalPosition).normalized * 0.5f;

        // 向前冲
        float lungeDuration = 0.1f;
        float timer = 0f;
        while (timer < lungeDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, lungePosition, timer / lungeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = lungePosition;

        // 造成伤害
        if (targetBuilding != null)
        {
            targetBuilding.TakeDamage(FinaleManager.Instance.currentMonsterDamage);
        }

        yield return new WaitForSeconds(0.1f); // 短暂等待，增加打击感

        // 撤回
        float returnDuration = 0.2f;
        timer = 0f;
        while (timer < returnDuration)
        {
            transform.position = Vector3.Lerp(lungePosition, originalPosition, timer / returnDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        
        // 重置计时器和状态
        attackTimer = FinaleManager.Instance.monsterAttackSpeed;
        isExecutingAttack = false; // 解锁AI
    }

    /// <summary>
    /// 由FinaleManager在坏结局时调用，命令怪物走向大海
    /// </summary>
    public void GoToSea()
    {
        currentState = State.GoToSea;
    }

    #region 净化逻辑

    /// <summary>
    /// 由PurificationAura调用，对怪物施加净化效果
    /// </summary>
    public void ApplyPurification(float amount)
    {
        if (isPurified) return;

        purificationLevel += amount;
        
        // 视觉效果：怪物颜色逐渐变白
        if (spriteRenderer != null)
        {
            float purityRatio = Mathf.Clamp01(purificationLevel / FinaleManager.Instance.currentMonsterHealth);
            spriteRenderer.color = Color.Lerp(Color.white, new Color(0.8f, 1f, 0.8f, 1f), purityRatio);
        }

        // 当净化值达到其最大生命值时，触发净化
        if (purificationLevel >= FinaleManager.Instance.currentMonsterHealth)
        {
            Purify();
        }
    }

    /// <summary>
    /// 执行净化，将怪物转变为居民
    /// </summary>
    private void Purify()
    {
        isPurified = true;
        Debug.Log("一个怪物被净化了！转变为居民。");

        // 1. 从怪物列表中移除自己
        if (FinaleManager.Instance != null)
        {
            FinaleManager.Instance.OnMonsterDefeated(this);
        }

        // 2. 在当前位置生成一个居民
        if (residentPrefab != null)
        {
            Instantiate(residentPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("Resident Prefab 未在怪物上指定！无法生成居民。");
        }

        // 3. 销毁自身
        Destroy(gameObject);
    }
    
    #endregion
}
