// ResidentAI.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 确保该对象上始终有Health组件，因为居民也可能被怪物杀死
[RequireComponent(typeof(Health))]
public class ResidentAI : MonoBehaviour
{
    // 居民的AI状态
    private enum State { Seeking, Moving, Attacking }
    private State currentState = State.Seeking;

    [Header("战斗参数")]
    [SerializeField] private float maxHealth = 100f;       // 生命值，Health 需要运行时显式初始化
    [SerializeField] private float moveSpeed = 2.5f;       // 移动速度
    [SerializeField] private float attackRange = 1.5f;     // 攻击距离（近战）
    [SerializeField] private float damage = 12f;           // 攻击伤害，略高于怪物
    [SerializeField] private float attackSpeed = 1.8f;     // 攻击间隔（秒），越小越快

    // 内部状态变量
    private Health targetMonster;
    private float attackTimer;
    private bool isExecutingAttack = false; // 用于防止在攻击动画期间执行其他逻辑

    private void Awake()
    {
        // Health 必须在这里显式初始化：它默认 currentHealth = 0，
        // 而 Health.TakeDamage 在 currentHealth <= 0 时会直接 return（等于居民永远无敌）。
        if (TryGetComponent(out Health health))
        {
            health.Initialize(maxHealth, null);
        }

        // 确保居民和其他动态对象一样，有正确的渲染排序
        if (!TryGetComponent<IsometricSorter>(out _))
        {
            var sorter = gameObject.AddComponent<IsometricSorter>();
            sorter.Initialize(false); // 'false' 因为居民是动态的
        }
    }
    
    private void Start()
    {
        // 当居民被创建时，立即向FinaleManager注册自己
        if (FinaleManager.Instance != null)
        {
            FinaleManager.Instance.OnResidentCreated(this);
        }
        else
        {
            Debug.LogError("ResidentAI无法找到FinaleManager实例！");
        }
    }

    private void OnDestroy()
    {
        // 当居民死亡并被销毁时，从FinaleManager注销自己
        // 添加实例检查，防止在场景关闭时因销毁顺序问题报错
        if (FinaleManager.Instance != null)
        {
            FinaleManager.Instance.OnResidentDestroyed(this);
        }
    }

    private void Update()
    {
        // 如果正在播放攻击动画，则暂停所有AI逻辑
        if (isExecutingAttack) return;
        
        // 检查目标是否仍然有效
        if (targetMonster != null && (targetMonster.gameObject == null || !targetMonster.enabled))
        {
            targetMonster = null;
            currentState = State.Seeking;
        }

        // 根据当前状态执行相应逻辑
        switch (currentState)
        {
            case State.Seeking:
                FindNearestMonster();
                break;
            case State.Moving:
                MoveToTarget();
                break;
            case State.Attacking:
                AttackTarget();
                break;
        }
    }

    /// <summary>
    /// 寻找最近的怪物作为目标
    /// </summary>
    private void FindNearestMonster()
    {
        Health closest = null;
        float minDistance = float.MaxValue;

        // 从FinaleManager获取当前所有存活的怪物
        List<MonsterAI> activeMonsters = FinaleManager.Instance.activeMonsters;
        
        if (activeMonsters == null || activeMonsters.Count == 0)
        {
            // 如果没有怪物了，就原地待命
            currentState = State.Seeking;
            return;
        }

        foreach (var monsterAI in activeMonsters)
        {
            if (monsterAI == null || !monsterAI.TryGetComponent<Health>(out var monsterHealth)) continue;

            float distance = Vector3.Distance(transform.position, monsterHealth.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = monsterHealth;
            }
        }
        
        if (closest != null)
        {
            targetMonster = closest;
            currentState = State.Moving;
        }
    }

    /// <summary>
    /// 向目标移动，直到进入攻击范围
    /// </summary>
    private void MoveToTarget()
    {
        if (targetMonster == null)
        {
            currentState = State.Seeking;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetMonster.transform.position);
        if (distanceToTarget > attackRange)
        {
            // 朝目标移动
            transform.position = Vector3.MoveTowards(transform.position, targetMonster.transform.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            // 已进入攻击范围，切换到攻击状态
            currentState = State.Attacking;
            attackTimer = attackSpeed; // 准备第一次攻击
        }
    }

    /// <summary>
    /// 攻击目标
    /// </summary>
    private void AttackTarget()
    {
        if (targetMonster == null)
        {
            currentState = State.Seeking;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            // 使用协程来执行带动画的攻击
            StartCoroutine(LungeAttackCoroutine());
        }

        // 如果怪物逃出攻击范围，切换回移动状态去追击
        float escapeRange = attackRange + 0.5f; 
        if (Vector3.Distance(transform.position, targetMonster.transform.position) > escapeRange)
        {
            currentState = State.Moving;
        }
    }
    
    /// <summary>
    /// 执行一个向前冲刺的攻击动画，并造成伤害
    /// </summary>
    private IEnumerator LungeAttackCoroutine()
    {
        isExecutingAttack = true; // 锁定AI状态
        
        Vector3 originalPosition = transform.position;

        // 安全检查，防止目标在协程开始后瞬间消失
        if (targetMonster == null)
        {
            isExecutingAttack = false;
            yield break;
        }

        Vector3 targetPosition = targetMonster.transform.position;
        Vector3 lungePosition = originalPosition + (targetPosition - originalPosition).normalized * 0.5f;

        // --- 向前冲刺 ---
        float lungeDuration = 0.1f;
        float timer = 0f;
        while (timer < lungeDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, lungePosition, timer / lungeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = lungePosition;

        // --- 造成伤害 ---
        if (targetMonster != null)
        {
            targetMonster.TakeDamage(damage);
        }

        // 短暂地停顿，增加打击感
        yield return new WaitForSeconds(0.1f);

        // --- 撤回原位 ---
        float returnDuration = 0.2f;
        timer = 0f;
        // 安全检查，防止在等待期间原目标消失
        if (targetMonster != null) lungePosition = transform.position; // 更新一下当前位置
        while (timer < returnDuration)
        {
            transform.position = Vector3.Lerp(lungePosition, originalPosition, timer / returnDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        
        // --- 重置状态 ---
        attackTimer = attackSpeed; // 重置攻击计时器
        isExecutingAttack = false; // 解锁AI
    }
}
