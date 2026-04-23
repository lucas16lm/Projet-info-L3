using UnityEngine;

public class PilotShip : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform associatedTransform;

    public void Interact(Transform player)
    {
        associatedTransform.GetComponent<ShipController>().EnterShip(player);
    }
}
