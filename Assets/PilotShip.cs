using UnityEngine;

public class PilotShip : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform associatedTransform;

    public void Interact(Transform player)
    {
        //player.GetComponent<PlayerMovementController>().enabled = false;
        player.position = new Vector3(transform.position.x, player.transform.position.y, transform.position.z);
        player.rotation = Quaternion.LookRotation(transform.forward);
        associatedTransform.GetComponent<ShipController>().EnterShip(player);
    }
}
