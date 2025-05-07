using Game.player;
using Unity.VisualScripting;
using UnityEngine;

public class PossessionSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public MovementSet currentMovement;
    public PossessableBody StartBody;
    private GameObject currentBody;
    public Transform currentBodyTransform;

    [Header("Settings")]
    public float possessionRange = 5f;
    public LayerMask possessableMask;


    private void Start()
    {
        OnStartPrefab(StartBody);
    }
    void Update()
    {
        if (currentBodyTransform != null)
        {
            playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            currentBodyTransform.position + new Vector3(0, 2, -5),
            Time.deltaTime * 5f
            );
        }
        if (Input.GetKeyDown(KeyCode.E))
            TryPossess();
    }
    void OnStartPrefab(PossessableBody StartBody)
    {
        currentBody = Instantiate(StartBody.bodyPrefab,
                                transform.position,
                                transform.rotation);
    }
    void TryPossess()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position,
                          playerCamera.transform.forward,
                          out hit,
                          possessionRange,
                          possessableMask))
        {
            if (hit.collider.TryGetComponent<PossessableBody>(out var newBody))
            {
                Possess(newBody);
            }
        }
    }
    void Possess(PossessableBody newBody)
    {
        if (currentBody != null)
        {
            Destroy(currentBody);
            currentBodyTransform = null;
        }

        currentBody = Instantiate(newBody.bodyPrefab,
                                transform.position,
                                transform.rotation);

        currentMovement = currentBody.GetComponent<MovementSet>();
        currentMovement.SetMovementStats(newBody.stats);

        if (newBody.cameraAnchor != null)
            playerCamera.transform.SetParent(currentBodyTransform);
    }

    public Vector3 GetBodyPosition()
    {
        if (currentBodyTransform != null)
            return currentBodyTransform.position;

        return Vector3.zero;
    }
}