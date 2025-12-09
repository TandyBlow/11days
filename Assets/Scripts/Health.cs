// Health.cs
using UnityEngine;

public class Health : MonoBehaviour
{
    private float currentHealth;
    private GameObject ruinPrefab;

    public void Initialize(float maxHealth, GameObject ruinPrefab)
    {
        this.currentHealth = maxHealth;
        this.ruinPrefab = ruinPrefab;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 从管理器列表中移除
        if (TryGetComponent<MonsterAI>(out var monster))
        {
            FinaleManager.Instance.OnMonsterDefeated(monster);
        }
        else if (TryGetComponent<BuildingInstance>(out _))
        {
            FinaleManager.Instance.OnBuildingDestroyed(this);
            // 如果是建筑，生成废墟
            if (ruinPrefab != null)
            {
                Instantiate(ruinPrefab, transform.position, transform.rotation);
            }
        }

        // 死亡
        Destroy(gameObject);
    }
}
