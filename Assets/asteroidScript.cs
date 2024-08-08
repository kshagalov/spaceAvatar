using UnityEngine;

public class AsteroidMovement : MonoBehaviour
{
    public Rigidbody rb;
    public float forwardForce = 1f; // Adjusted force amount for forward movement (slower)
    public float sidewaysForce = 4f; // Force amount for sideways movement (slower)

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Assign the Rigidbody component

        // Apply force in the forward and right directions
        rb.AddForce(Vector3.forward * forwardForce, ForceMode.Impulse); // Forward movement
        rb.AddForce(Vector3.right * sidewaysForce, ForceMode.Impulse); // Rightward movement
    }
}
