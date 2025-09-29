using UnityEngine;
using HTC.UnityPlugin.Vive;
using System.Collections;

public class GasAnalyzerGrabbable : MonoBehaviour
{
    [Header("Настройки захвата")]
    public GasAnalyzerController gasAnalyzer;

    [Header("Положение в левой руке")]
    public Vector3 leftHandPosition = new Vector3(-0.2f, -0.1f, 0.3f);
    public Vector3 leftHandRotation = new Vector3(0f, -90f, 0f);

    [Header("Настройки возврата")]
    public float returnSmoothSpeed = 10f;

    private bool isGrabbed = false;
    private bool isReturning = false;
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Rigidbody rb;
    private Collider[] colliders;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (gasAnalyzer == null)
        {
            gasAnalyzer = GetComponent<GasAnalyzerController>();
            if (gasAnalyzer == null)
            {
                gasAnalyzer = GetComponentInChildren<GasAnalyzerController>();
            }
        }
    }

    void Update()
    {
        if (isGrabbed)
        {
            UpdateGrabbedPosition();
        }
        else if (isReturning)
        {
            UpdateReturnPosition();
        }
    }

    void UpdateGrabbedPosition()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition = cameraTransform.position +
                                cameraTransform.right * leftHandPosition.x +
                                cameraTransform.up * leftHandPosition.y +
                                cameraTransform.forward * leftHandPosition.z;

        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(leftHandRotation);

        transform.position = Vector3.Lerp(transform.position, targetPosition, 15f * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
    }

    void UpdateReturnPosition()
    {

        transform.position = Vector3.Lerp(transform.position, originalPosition, returnSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, returnSmoothSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, originalPosition) < 0.01f &&
            Quaternion.Angle(transform.rotation, originalRotation) < 1f)
        {
            isReturning = false;

            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
    public void Grab()
    {
        if (!isGrabbed && !isReturning)
        {
            isGrabbed = true;
            isReturning = false;

            StopAllCoroutines();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            SetCollidersEnabled(false);

            transform.SetParent(cameraTransform);

            if (gasAnalyzer != null)
            {
                gasAnalyzer.GrabAnalyzer(HandRole.LeftHand);
            }

        }
    }

    // Метод для отпускания
    public void Release()
    {
        if (isGrabbed)
        {
            isGrabbed = false;
            isReturning = true;

            transform.SetParent(originalParent);

            SetCollidersEnabled(true);
            StartCoroutine(EnsureReturnToPosition());

            if (gasAnalyzer != null)
            {
                gasAnalyzer.ReleaseAnalyzer();
            }

        }
    }

    private IEnumerator EnsureReturnToPosition()
    {
        yield return new WaitForSeconds(2f);
        if (isReturning)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isReturning = false;
        }
    }

    public void OnViveGrab()
    {
        Grab();
    }

    public void OnViveRelease()
    {
        Release();
    }

    public void ToggleGrab()
    {
        if (isGrabbed)
        {
            Release();
        }
        else
        {
            Grab();
        }
    }

    public bool IsGrabbed()
    {
        return isGrabbed;
    }

    public bool IsReturning()
    {
        return isReturning;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders != null)
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }
    }

    public bool IsBeingAimedAt(Transform controllerTransform)
    {
        if (controllerTransform == null) return false;

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        float checkDistance = 2f;

        if (Physics.Raycast(ray, out hit, checkDistance))
        {
            return hit.collider.gameObject == gameObject ||
                   hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }

    public void ForceReturnToOriginalPosition()
    {
        if (isGrabbed)
        {
            Release();
        }
        else
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isReturning = false;
        }
    }

    public void UpdateOriginalPosition()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.1f);
        Gizmos.DrawLine(originalPosition, originalPosition + originalRotation * Vector3.forward * 0.2f);
    }
}