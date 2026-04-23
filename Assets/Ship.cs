using Cinemachine;
using StylizedWater3;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ship : MonoBehaviour
{
    
    [SerializeField] private List<GameObject> sailsUp;
    [SerializeField] private List<GameObject> sailsDown;

    private CinemachineFreeLook freeLookCam;
 

 
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
}
