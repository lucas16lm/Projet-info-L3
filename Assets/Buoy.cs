using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpringJoint))]
public class Buoy : MonoBehaviour
{
    private Rigidbody rb;
    private SpringJoint joint;
    private Vector3 lastAnchorPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        joint = GetComponent<SpringJoint>();
    }

    public void Move()
    {
        Vector3 worldAnchorPos = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
        Vector3 targetPos = new Vector3(worldAnchorPos.x, rb.position.y, worldAnchorPos.z);
        rb.MovePosition(targetPos);
        rb.MoveRotation(joint.connectedBody.transform.rotation);
    }
}
