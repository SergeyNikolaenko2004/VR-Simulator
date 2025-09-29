using UnityEngine;

public class CableRenderer : MonoBehaviour
{
    [Header("Настройки кабеля")]
    public Transform startPoint;
    public Transform endPoint;
    public Material cableMaterial;
    public float cableWidth = 0.01f;
    public int segmentCount = 20;
    public float sagAmount = 0.5f;

    private LineRenderer lineRenderer;
    private Vector3[] cablePoints;
    private Vector3[] cableVelocities;

    void Start()
    {
        InitializeCable();
    }

    void InitializeCable()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = cableWidth;
        lineRenderer.endWidth = cableWidth;
        lineRenderer.material = cableMaterial != null ? cableMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.useWorldSpace = true; 

        cablePoints = new Vector3[segmentCount];
        cableVelocities = new Vector3[segmentCount];

        UpdateCablePoints();
    }

    void Update()
    {
        UpdateCable();
    }

    public void UpdateCable()
    {
        if (lineRenderer == null || startPoint == null || endPoint == null)
        {
            return;
        }

        UpdateCableStatic();

        lineRenderer.SetPositions(cablePoints);
    }

    void UpdateCablePoints()
    {
        Vector3 startPos = GetStartPosition();
        Vector3 endPos = GetEndPosition();

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            cablePoints[i] = Vector3.Lerp(startPos, endPos, t);

            cablePoints[i] += Vector3.down * sagAmount * Mathf.Sin(Mathf.PI * t);
            cableVelocities[i] = Vector3.zero;
        }
    }

    void UpdateCableStatic()
    {
        Vector3 startPos = GetStartPosition();
        Vector3 endPos = GetEndPosition();

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            cablePoints[i] = Vector3.Lerp(startPos, endPos, t);

            cablePoints[i] += Vector3.down * sagAmount * Mathf.Sin(Mathf.PI * t);
        }
    }

    Vector3 GetStartPosition()
    {
        if (startPoint == null)
        {
            return transform.position;
        }
        return startPoint.position;
    }

    Vector3 GetEndPosition()
    {
        if (endPoint == null)
        {
            return transform.position;
        }
        return endPoint.position; 
    }

    void OnValidate()
    {
        if (lineRenderer != null)
        {
            InitializeCable();
        }
    }

    void OnDrawGizmos()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.02f);
            Gizmos.DrawIcon(startPoint.position, "CableStart");
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.02f);
            Gizmos.DrawIcon(endPoint.position, "CableEnd");
        }

        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
    }

    void OnDestroy()
    {
        if (lineRenderer != null)
        {
            Destroy(lineRenderer);
        }
    }
}