using UnityEngine;
using HTC.UnityPlugin.Vive;
using System.Collections;

public class GasAnalyzerProbe : MonoBehaviour
{
    [Header("Настройки зонда")]
    public GasAnalyzerController gasAnalyzer;
    public Transform cableAttachmentPoint; 

    [Header("Положение в правой руке")]
    public Vector3 rightHandPosition = new Vector3(0.2f, -0.1f, 0.3f);
    public Vector3 rightHandRotation = new Vector3(0f, 90f, 0f);

    [Header("Настройки возврата")]
    public float returnSmoothSpeed = 10f;

    [Header("Визуальные эффекты")]
    public Material normalMaterial;
    public Material grabbedMaterial;
    public Renderer probeRenderer;
    public ParticleSystem connectionParticles;

    private bool isGrabbed = false;
    private bool isReturning = false;
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Rigidbody rb;
    private Collider[] colliders;
    private CableRenderer cableRenderer;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        cableRenderer = GetComponent<CableRenderer>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (gasAnalyzer == null)
        {
            gasAnalyzer = FindObjectOfType<GasAnalyzerController>();
        }

        if (cableRenderer == null)
        {
            cableRenderer = gameObject.AddComponent<CableRenderer>();
        }

        SetupCable();
    }

    void Update()
    {
        if (isGrabbed)
        {
            UpdateGrabbedPosition();
            UpdateCable();
        }
        else if (isReturning)
        {
            UpdateReturnPosition();
            UpdateCable();
        }
    }

    void SetupCable()
    {
        if (gasAnalyzer != null && cableRenderer != null)
        {
            Transform analyzerCablePoint = gasAnalyzer.transform.Find("CableAttachmentPoint");
            if (analyzerCablePoint == null)
            {
                GameObject cablePoint = new GameObject("CableAttachmentPoint");
                cablePoint.transform.SetParent(gasAnalyzer.transform);
                cablePoint.transform.localPosition = new Vector3(0.1f, 0.05f, 0.1f);
                analyzerCablePoint = cablePoint.transform;
            }

            cableRenderer.startPoint = analyzerCablePoint;
            cableRenderer.endPoint = cableAttachmentPoint != null ? cableAttachmentPoint : transform;
            cableRenderer.cableMaterial = normalMaterial;
        }
    }

    void UpdateGrabbedPosition()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition = cameraTransform.position +
                                cameraTransform.right * rightHandPosition.x +
                                cameraTransform.up * rightHandPosition.y +
                                cameraTransform.forward * rightHandPosition.z;

        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(rightHandRotation);

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

    void UpdateCable()
    {
        if (cableRenderer != null)
        {
            cableRenderer.UpdateCable();
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

            if (probeRenderer != null && grabbedMaterial != null)
            {
                probeRenderer.material = grabbedMaterial;
            }

            if (connectionParticles != null)
            {
                connectionParticles.Play();
            }

        }
    }

    public void Release()
    {
        if (isGrabbed)
        {
            isGrabbed = false;
            isReturning = true;

            transform.SetParent(originalParent);
            SetCollidersEnabled(true);

            if (probeRenderer != null && normalMaterial != null)
            {
                probeRenderer.material = normalMaterial;
            }

            StartCoroutine(EnsureReturnToPosition());

        }
    }

    IEnumerator EnsureReturnToPosition()
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.05f);
        Gizmos.DrawLine(originalPosition, originalPosition + originalRotation * Vector3.forward * 0.1f);
    }
}