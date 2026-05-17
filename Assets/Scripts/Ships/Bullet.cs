using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int damage = 5;
    private GameObject explosionPrefab;

    public void Init(int damage)
    {
        this.damage = damage;
    }

    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.GetComponent<Ship>()?.Damage(damage);
        Destroy(gameObject);
    }
}
