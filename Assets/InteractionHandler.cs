using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHandler : MonoBehaviour
{
    private InputAction interactAction;
    private Collider currentTrigger;

    private void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
        interactAction.performed += OnInteraction;
    }

    private void OnTriggerEnter(Collider other)
    {
        currentTrigger = other;
    }

    private void OnTriggerExit(Collider other)
    {
        currentTrigger = null;
    }

    private void OnInteraction(InputAction.CallbackContext context)
    {
        if (currentTrigger == null)
        {
            Debug.Log("No interaction here");
            return;
        }

        currentTrigger.transform.GetComponent<PilotShip>().Interact(transform);

    }

}
