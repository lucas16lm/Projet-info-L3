using Cinemachine;
using StylizedWater3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipMovement))]
public class Ship : MonoBehaviour
{
    
    [SerializeField] private List<GameObject> sailsUp;
    [SerializeField] private List<GameObject> sailsDown;

    private CinemachineFreeLook freeLookCam;

    [SerializeField] private int health = 100;
 

 
    private void Awake()
    {
        freeLookCam = GetComponentInChildren<CinemachineFreeLook>();
    }


    public void SetCruiseView()
    {
        freeLookCam.Priority = 15;
    }

    public void UnsetCruiseView()
    {
        freeLookCam.Priority = 0;
    }

    public void OpenSails()
    {
        foreach (GameObject sail in sailsUp)
        {
            sail.SetActive(false);
        }

        foreach (GameObject sail in sailsDown)
        {
            sail.SetActive(true);
        }
    }

    public void CloseSails()
    {
        foreach (GameObject sail in sailsUp)
        {
            sail.SetActive(true);
        }

        foreach (GameObject sail in sailsDown)
        {
            sail.SetActive(false);
        }
    }

    public void Damage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Sink();
        }
    }
    
    [ContextMenu("Sink ship")]
    public void Sink()
    {
        transform.GetComponent<ShipMovement>().RemoveBuoyancyForce();
        
        Collider[] colliders = transform.GetComponentsInChildren<Collider>();
        foreach(Collider collider in colliders)
        {
            collider.enabled = false;
        }
        StartCoroutine(TimedDestroy());
    }

    private IEnumerator TimedDestroy()
    {
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }
}
