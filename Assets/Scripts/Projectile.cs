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
        var sorter = gameObject.AddComponent<IsometricSorter>();
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
