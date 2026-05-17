using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = transform.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.75f;
    }

    public IEnumerator Shoot(GameObject bulletPrefab, GameObject shootParticle, int shootDamage, Vector3 direction, AudioClip shootSound)
    {
        yield return new WaitForSeconds(Random.Range(0, 1f));

        GameObject bullet = Instantiate(bulletPrefab);
        GameObject particle = Instantiate(shootParticle);
        
        bullet.transform.position = transform.position + direction * 2;
        particle.transform.position = transform.position + direction * 2;
        particle.transform.rotation = Quaternion.LookRotation(direction);

        bullet.GetComponent<Bullet>().Init(shootDamage);
        bullet.GetComponent<Rigidbody>().AddForce((direction+Vector3.up*0.1f) * 1000, ForceMode.Impulse);
        audioSource.PlayOneShot(shootSound);

        Destroy(bullet, 5f);
        Destroy(particle, 5f);
    }
}
