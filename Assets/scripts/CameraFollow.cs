using UnityEngine;

// Keeps the camera behind the car. LateUpdate runs after every Update,
// so the car has already moved when the camera follows it.
// The camera sticks close to the car but turns around it a little LATER
// than the car, so you can see the front of the car turn.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, -4.5f);
    [SerializeField] private float followSharpness = 8f;  // how tightly the camera stays with the car (higher = tighter)
    [SerializeField] private float turnLag = 2.5f;        // how fast the camera swings behind the car (lower = you see the car turn more)

    private float yaw;   // the direction the camera looks from, smoothed

    void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // the camera's direction follows the car's direction, but with a delay
        float yawBlend = 1f - Mathf.Exp(-turnLag * Time.deltaTime);
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, yawBlend);

        // the camera stays at the offset behind the car, in that delayed direction
        Vector3 wanted = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;

        // 1 - exp(...) gives the same smoothness at any frame rate
        float positionBlend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, wanted, positionBlend);

        // always look at the car
        Vector3 lookPoint = target.position + Vector3.up * 0.3f;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position);
    }
}
