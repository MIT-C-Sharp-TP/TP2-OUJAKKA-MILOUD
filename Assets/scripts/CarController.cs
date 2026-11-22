using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Drives the car.
//   Forward / back : W S   (also Z on AZERTY, and Up / Down arrows)
//   Steer          : A D   (also Q on AZERTY, and Left / Right arrows)
//   Brake          : Left Shift
//   Jump           : Space
// Input is read in Update, the car moves in FixedUpdate.
// The car always moves where its FRONT points: turning rotates the car,
// so holding a steering key while driving makes a circle.
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField] private float acceleration = 6f;    // metres per second, per second
    [SerializeField] private float maxSpeed = 8f;        // metres per second
    [SerializeField] private float brakeForce = 14f;     // how fast braking takes speed away
    [SerializeField] private float drag = 1.5f;          // slowing down when nothing is pressed

    [Header("Steering")]
    [SerializeField] private float turnDegreesPerSecond = 70f; // how fast the front turns at full steering
    [SerializeField] private float steerEase = 4f;             // lower = softer, higher = snappier
    [SerializeField] private float fullSteerSpeed = 3f;        // speed at which the car turns at full rate
    [SerializeField] private float minTurnFactor = 0.4f;       // turning when almost standing still
    [SerializeField] private bool invertSteering = false;      // tick it if A and D feel reversed

    [Header("Wheels (look only)")]
    [SerializeField] private Transform[] frontWheels;     // the two FRONT wheels: they steer and roll
    [SerializeField] private Transform[] rearWheels;      // the two REAR wheels: they only roll
    [SerializeField] private float maxWheelAngle = 25f;   // how far the front wheels turn
    [SerializeField] private float wheelRadius = 0.225f;  // half of the wheel's X scale (0.45 / 2)

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 4.5f;     // metres per second, upwards
    [SerializeField] private float groundCheck = 0.35f;  // how far below the car the ground may be

    private Rigidbody rb;
    private float throttle;     // -1 back, 0, +1 forward
    private float steering;     // raw key: -1 left, 0, +1 right
    private float steerInput;   // smoothed steering
    private bool braking;
    private bool jumpWanted;
    private float speed;        // the car's own speed along its forward direction
    private float wheelSpin;    // how far the wheels have rolled, in degrees
    private Quaternion[] frontStart;
    private Quaternion[] rearStart;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // settings that make the car smooth and solid, set here so nothing
        // has to be changed by hand in the Inspector. Only X and Z are frozen:
        // the car must be free to turn around Y.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // remember how each wheel is turned, to roll and steer them from there
        frontStart = RememberRotations(frontWheels);
        rearStart = RememberRotations(rearWheels);
    }

    private static Quaternion[] RememberRotations(Transform[] wheels)
    {
        if (wheels == null)
        {
            return new Quaternion[0];
        }

        Quaternion[] start = new Quaternion[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                start[i] = wheels[i].localRotation;
            }
        }
        return start;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool forward = Held(keyboard, keyboard.wKey, "w") || Held(keyboard, keyboard.zKey, "z") || keyboard.upArrowKey.isPressed;
            bool backward = Held(keyboard, keyboard.sKey, "s") || keyboard.downArrowKey.isPressed;
            bool left = Held(keyboard, keyboard.aKey, "a") || Held(keyboard, keyboard.qKey, "q") || keyboard.leftArrowKey.isPressed;
            bool right = Held(keyboard, keyboard.dKey, "d") || keyboard.rightArrowKey.isPressed;

            throttle = (forward ? 1f : 0f) - (backward ? 1f : 0f);
            steering = (right ? 1f : 0f) - (left ? 1f : 0f);
            braking = keyboard.leftShiftKey.isPressed;

            // a key press lasts one frame: remember it until the next physics step
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpWanted = true;
            }
        }

        UpdateWheels();
    }

    // True if the key at the physical position OR the key that prints this
    // character on the current keyboard layout (QWERTY, AZERTY...) is held.
    private static bool Held(Keyboard keyboard, KeyControl physicalKey, string layoutCharacter)
    {
        if (physicalKey.isPressed)
        {
            return true;
        }

        KeyControl byLayout = keyboard.FindKeyOnCurrentKeyboardLayout(layoutCharacter);
        return byLayout != null && byLayout.isPressed;
    }

    // Only looks: the wheels roll with the speed, the front ones also turn.
    private void UpdateWheels()
    {
        // distance driven / radius = angle rolled (in radians), then in degrees
        float rolled = speed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
        wheelSpin = Mathf.Repeat(wheelSpin + rolled, 360f);

        // the wheels' axle is the car's X axis, so they roll around X
        Quaternion roll = Quaternion.Euler(wheelSpin, 0f, 0f);
        Quaternion steer = Quaternion.Euler(0f, steerInput * maxWheelAngle, 0f);

        for (int i = 0; i < frontStart.Length; i++)
        {
            if (frontWheels[i] != null)
            {
                frontWheels[i].localRotation = steer * roll * frontStart[i];
            }
        }

        for (int i = 0; i < rearStart.Length; i++)
        {
            if (rearWheels[i] != null)
            {
                rearWheels[i].localRotation = roll * rearStart[i];
            }
        }
    }

    void FixedUpdate()
    {
        float step = Time.fixedDeltaTime;

        if (braking)
        {
            // the brake takes speed away, in both directions, and can stop the car
            speed = Mathf.MoveTowards(speed, 0f, brakeForce * step);
        }
        else if (throttle != 0f)
        {
            speed += throttle * acceleration * step;
        }
        else
        {
            // no key held: the car coasts and slowly stops
            speed = Mathf.MoveTowards(speed, 0f, drag * step);
        }

        speed = Mathf.Clamp(speed, -maxSpeed * 0.4f, maxSpeed);

        // the car moves along its FRONT (transform.forward): the speed goes
        // where the nose points; gravity keeps the vertical speed
        Vector3 velocity = transform.forward * speed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        // smooth steering: the wheel eases towards the key instead of jumping
        float target = invertSteering ? -steering : steering;
        float blend = 1f - Mathf.Exp(-steerEase * step);
        steerInput = Mathf.Lerp(steerInput, target, blend);

        // the front turns left or right as long as the key is held.
        // It turns slowly even when standing, at full rate from fullSteerSpeed,
        // and a little less at top speed so the car stays stable.
        // In reverse the steering is inverted, like a real car.
        float speedFactor = Mathf.Clamp(Mathf.Abs(speed) / fullSteerSpeed, minTurnFactor, 1f);
        float stability = Mathf.Lerp(1f, 0.7f, Mathf.Abs(speed) / maxSpeed);
        float direction = speed < -0.1f ? -1f : 1f;
        float angle = steerInput * direction * turnDegreesPerSecond * speedFactor * stability * step;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, angle, 0f));

        // collisions must not leave the car spinning
        rb.angularVelocity = Vector3.zero;

        if (jumpWanted)
        {
            jumpWanted = false;
            if (Grounded())
            {
                rb.linearVelocity += Vector3.up * jumpSpeed;
            }
        }
    }

    // When the car runs into a wall, its own speed goes back to zero,
    // so it really stops instead of pushing against the wall.
    void OnCollisionEnter(Collision collision)
    {
        StopAgainstWall(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        StopAgainstWall(collision);
    }

    private void StopAgainstWall(Collision collision)
    {
        Vector3 moving = transform.forward * (speed >= 0f ? 1f : -1f);

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            // a wall is a steep surface (not the floor, not the ramp)
            // facing against the direction the car is going
            if (contact.normal.y < 0.5f && Vector3.Dot(contact.normal, moving) < -0.5f)
            {
                speed = 0f;
                return;
            }
        }
    }

    // a short ray downwards: is there ground right under the car?
    private bool Grounded()
    {
        Vector3 from = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(from, Vector3.down, groundCheck + 0.1f);
    }
}
