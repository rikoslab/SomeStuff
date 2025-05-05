using UnityEngine;

public class Mario64Camera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // The player character to follow
    public float targetHeight = 1.0f; // Height offset from the target's position

    [Header("Camera Distance")]
    public float distance = 5.0f; // Default distance from target
    public float minDistance = 2.0f; // Minimum allowed distance
    public float maxDistance = 10.0f; // Maximum allowed distance
    public float zoomSpeed = 5.0f; // Mouse wheel zoom speed

    [Header("Rotation Settings")]
    public float xSpeed = 200.0f; // X-axis rotation speed
    public float ySpeed = 200.0f; // Y-axis rotation speed
    public float rotationDampening = 3.0f; // Rotation smoothness
    public float yMinLimit = -20.0f; // Minimum vertical angle
    public float yMaxLimit = 80.0f; // Maximum vertical angle
    public float alignToTargetSpeed = 5.0f; // Speed for aligning to target's forward direction

    [Header("Collision Settings")]
    public bool enableCollision = true; // Enable camera collision
    public float collisionOffset = 0.2f; // How much to offset from collision
    public LayerMask collisionLayers = -1; // Which layers to check for collision

    private float x = 0.0f; // Current x rotation
    private float y = 0.0f; // Current y rotation
    private float currentDistance; // Current camera distance
    private float desiredDistance; // Desired camera distance
    private float correctedDistance; // Distance after collision correction
    private bool isAligningToTarget = false; // Flag for alignment in progress

    void Start()
    {
        // Initialize angles
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Set initial distances
        currentDistance = distance;
        desiredDistance = distance;
        correctedDistance = distance;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Handle mouse input for rotation
        if (Input.GetMouseButton(1)) // Right mouse button held
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            isAligningToTarget = false; // Cancel alignment if user rotates manually
        }

        // Handle alignment to target's forward direction
        if (Input.GetKeyDown(KeyCode.C)) // You can change this to any key you prefer
        {
            AlignToTargetDirection();
        }

        // If alignment is in progress, smoothly rotate to target's forward direction
        if (isAligningToTarget)
        {
            AlignCameraToTarget();
        }

        // Clamp vertical rotation
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // Calculate rotation
        Quaternion rotation = isAligningToTarget ? transform.rotation : Quaternion.Euler(y, x, 0);

        // Handle zoom input
        desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

        // Calculate desired camera position
        Vector3 position = target.position - (rotation * Vector3.forward * desiredDistance + new Vector3(0, -targetHeight, 0));

        // Check for camera collision
        correctedDistance = desiredDistance;
        if (enableCollision)
        {
            RaycastHit hit;
            Vector3 targetPos = new Vector3(target.position.x, target.position.y + targetHeight, target.position.z);
            Vector3 dir = position - targetPos;

            if (Physics.Raycast(targetPos, dir.normalized, out hit, desiredDistance, collisionLayers))
            {
                correctedDistance = hit.distance - collisionOffset;
            }
        }

        // Smoothly transition to the corrected distance
        currentDistance = Mathf.Lerp(currentDistance, correctedDistance, Time.deltaTime * zoomSpeed);

        // Recalculate position based on corrected distance
        position = target.position - (rotation * Vector3.forward * currentDistance + new Vector3(0, -targetHeight, 0));

        // Apply rotation and position to camera
        if (!isAligningToTarget)
        {
            transform.rotation = rotation;
        }
        transform.position = position;

        // Make camera look at target with height offset
        Vector3 targetPosWithHeight = target.position + new Vector3(0, targetHeight, 0);
        transform.LookAt(targetPosWithHeight);
    }

    // Clamp angle between min and max values
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    // Start aligning camera to target's forward direction
    public void AlignToTargetDirection()
    {
        isAligningToTarget = true;
    }

    // Smoothly align camera to target's forward direction
    private void AlignCameraToTarget()
    {
        // Get target's forward direction (ignoring Y rotation to keep current camera height)
        Vector3 targetForward = target.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(targetForward, Vector3.up);

        // Get current camera rotation (ignoring Y rotation)
        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        Quaternion currentRotation = Quaternion.LookRotation(cameraForward, Vector3.up);

        // Smoothly interpolate between current and target rotation
        float angle = Quaternion.Angle(currentRotation, targetRotation);
        if (angle > 0.1f)
        {
            Quaternion newRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * alignToTargetSpeed);

            // Update the x rotation value to match the new rotation
            x = newRotation.eulerAngles.y;
            y = transform.rotation.eulerAngles.x; // Maintain current vertical angle

            // Apply the rotation
            transform.rotation = Quaternion.Euler(y, x, 0);
        }
        else
        {
            // Alignment complete
            isAligningToTarget = false;
            x = targetRotation.eulerAngles.y;
        }
    }
}