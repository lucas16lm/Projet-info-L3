using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int damage = 5;
    private GameObject impactPrefab;

    public void Init(int damage, GameObject impactPrefab)
    {
        this.damage = damage;
        this.impactPrefab = impactPrefab;
    }

    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.GetComponent<Ship>()?.Damage(damage);
        GameObject impact = Instantiate(impactPrefab, transform);
        impact.transform.position = collision.GetContact(0).point;
        Destroy(gameObject, 1.5f);
    }
}
