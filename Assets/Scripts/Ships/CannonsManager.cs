using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonsManager : MonoBehaviour
{
    [SerializeField] private GameObject shotParticles;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private AudioClip shootSound;

    [SerializeField] private List<Cannon> leftCannons;
    [SerializeField] private List<Cannon> rightCannons;

    [SerializeField] private float shootCooldown;
    [SerializeField] private int shootDamage = 5;

    private bool leftReloading = false;
    private bool rightReloading = false;

    public void ShootLeft()
    {
        if (leftReloading) return;

        Shoot(leftCannons, -transform.right);

        leftReloading = true;
        StartCoroutine(ReloadLeft());
    }

    public void ShootRight()
    {
        if (rightReloading) return;

        Shoot(rightCannons, transform.right);

        rightReloading = true;
        StartCoroutine(ReloadRight());
    }

    private void Shoot(List<Cannon> cannons, Vector3 direction)
    {
        foreach (Cannon cannon in cannons)
        {
            StartCoroutine(cannon.Shoot(bulletPrefab, shotParticles, shootDamage, direction, shootSound));
        }
    }

    private IEnumerator ReloadLeft()
    {
        yield return new WaitForSeconds(shootCooldown);
        leftReloading = false;
    }

    private IEnumerator ReloadRight()
    {
        yield return new WaitForSeconds(shootCooldown);
        rightReloading = false;
    }

}
