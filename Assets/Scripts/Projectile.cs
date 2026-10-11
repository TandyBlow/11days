// Projectile.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed = 15f;

    public void Initialize(Transform target, float damage)
    {
        this.target = target;
        this.damage = damage;

        // Prefab 上已经挂了 IsometricSorter 时不要再加一个（两个实例会互相覆盖 sortingOrder）
        if (!TryGetComponent(out IsometricSorter sorter))
        {
            sorter = gameObject.AddComponent<IsometricSorter>();
        }
        sorter.Initialize(false, 100f, 10);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            if (target.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
