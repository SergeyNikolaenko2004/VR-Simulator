using UnityEngine;
using HTC.UnityPlugin.Vive;

public class VRPlayerMovement : MonoBehaviour
{
    [Header("VR движение")]
    public float movementSpeed = 3f;
    public float rotationSpeed = 45f;
    public bool enableSmoothMovement = true;

    [Header("Настройки телепортации")]
    public float teleportRange = 10f;
    public LayerMask teleportLayerMask = -1;
    public GameObject teleportMarker;
    public Material validTeleportMaterial;
    public Material invalidTeleportMaterial;
    public float playerHeight = 1.6f;

    [Header("Ссылка на UI Manager")]
    public UIManager uiManager;

    public Transform leftController;
    public Transform rightController;
    private bool isVRMode = false;
    private bool isTeleporting = false;
    private HandRole teleportHand;
    private Vector3 teleportTarget;
    private bool isValidTeleport = false;
    private LineRenderer teleportLine;
    private float currentGroundOffset;

    void Start()
    {
        isVRMode = UnityEngine.XR.XRSettings.enabled;
        currentGroundOffset = transform.position.y;

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        CreateTeleportVisuals();

        if (uiManager != null)
        {
            if (isVRMode)
            {
                uiManager.ShowHint("Для телепортации нажмите тачпад на контроллере", 5f);
            }
            else
            {
                uiManager.ShowHint("Для телепортации нажмите T и отпустите", 5f);
            }
        }
    }

    void Update()
    {
        if (isVRMode || !Application.isEditor)
        {
            HandleRealVRMovement();
        }
        else
        {
            HandleEditorMovement();
        }

        UpdateTeleportVisuals();
    }

    void CreateTeleportVisuals()
    {
        teleportLine = gameObject.AddComponent<LineRenderer>();
        teleportLine.positionCount = 2;
        teleportLine.startWidth = 0.02f;
        teleportLine.endWidth = 0.01f;
        teleportLine.material = new Material(Shader.Find("Sprites/Default"));
        teleportLine.startColor = Color.blue;
        teleportLine.endColor = Color.cyan;
        teleportLine.enabled = false;

        if (teleportMarker == null)
        {
            teleportMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teleportMarker.name = "TeleportMarker";
            teleportMarker.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Destroy(teleportMarker.GetComponent<Collider>());
        }
        teleportMarker.SetActive(false);
    }

    void HandleRealVRMovement()
    {
        if (ViveInput.GetPressDown(HandRole.LeftHand, ControllerButton.Pad))
        {
            StartTeleport(HandRole.LeftHand);
            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Телепортация: левый контроллер", 2f);
        }

        if (ViveInput.GetPressDown(HandRole.RightHand, ControllerButton.Pad))
        {
            StartTeleport(HandRole.RightHand);
            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Телепортация: правый контроллер", 2f);
        }

        if (isTeleporting)
        {
            UpdateTeleportTarget();
        }

        if (isTeleporting &&
            (ViveInput.GetPressUp(teleportHand, ControllerButton.Pad) ||
             ViveInput.GetPressUp(teleportHand, ControllerButton.Trigger)))
        {
            if (isValidTeleport)
            {
                ExecuteTeleport();
                if (uiManager != null)
                    uiManager.ShowTemporaryStatus("Телепортация выполнена!", 2f);
            }
            else
            {
                if (uiManager != null)
                    uiManager.ShowTemporaryStatus("Нельзя телепортироваться сюда", 2f);
            }
            StopTeleport();
        }

        if (!isTeleporting)
        {
            Vector2 leftTrackpad = ViveInput.GetPadAxis(HandRole.LeftHand);
            Vector2 rightTrackpad = ViveInput.GetPadAxis(HandRole.RightHand);

            if (leftTrackpad.magnitude > 0.1f)
            {
                Vector3 movement = CalculateMovement(leftTrackpad);
                transform.position += movement * movementSpeed * Time.deltaTime;
            }

            if (Mathf.Abs(rightTrackpad.x) > 0.3f)
            {
                float rotation = rightTrackpad.x * rotationSpeed * Time.deltaTime;
                transform.Rotate(0, rotation, 0);
            }
        }
    }

    void HandleEditorMovement()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartTeleportEditor();
            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Режим телепортации: наведите и отпустите клавишу", 3f);
        }

        if (isTeleporting)
        {
            UpdateTeleportTargetEditor();
        }

        if (isTeleporting && Input.GetKeyUp(KeyCode.T))
        {
            if (isValidTeleport)
            {
                ExecuteTeleport();
                if (uiManager != null)
                    uiManager.ShowTemporaryStatus("Телепортация выполнена!", 2f);
            }
            else
            {
                if (uiManager != null)
                    uiManager.ShowTemporaryStatus("Нельзя телепортироваться сюда", 2f);
            }
            StopTeleport();
        }


        if (!isTeleporting)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, 0, vertical);
            movement = transform.TransformDirection(movement);
            transform.position += movement * movementSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.Q))
            {
                transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
            }
            if (Input.GetKey(KeyCode.E))
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }
    }

    void StartTeleport(HandRole hand)
    {
        isTeleporting = true;
        teleportHand = hand;
        teleportLine.enabled = true;
        teleportMarker.SetActive(true);
    }

    void StartTeleportEditor()
    {
        isTeleporting = true;
        teleportHand = HandRole.RightHand;
        teleportLine.enabled = true;
        teleportMarker.SetActive(true);
    }

    void UpdateTeleportTarget()
    {
        Transform controller = teleportHand == HandRole.LeftHand ? leftController : rightController;
        UpdateTeleportRay(controller.position, controller.forward);
    }

    void UpdateTeleportTargetEditor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        UpdateTeleportRay(ray.origin, ray.direction);
    }

    void UpdateTeleportRay(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        isValidTeleport = false;

        if (Physics.Raycast(origin, direction, out hit, teleportRange, teleportLayerMask))
        {
            if (IsValidTeleportSurface(hit.collider))
            {
                teleportTarget = hit.point;
                isValidTeleport = true;
                teleportLine.startColor = Color.green;
                teleportLine.endColor = Color.green;

                if (validTeleportMaterial != null)
                    teleportMarker.GetComponent<Renderer>().material = validTeleportMaterial;
            }
        }

        if (!isValidTeleport)
        {
            teleportTarget = origin + direction * teleportRange;
            teleportLine.startColor = Color.red;
            teleportLine.endColor = Color.red;

            if (invalidTeleportMaterial != null)
                teleportMarker.GetComponent<Renderer>().material = invalidTeleportMaterial;
        }

        teleportMarker.transform.position = teleportTarget + Vector3.up * 0.1f;
    }

    void UpdateTeleportVisuals()
    {
        if (isTeleporting)
        {
            Transform controller = teleportHand == HandRole.LeftHand ? leftController : rightController;
            teleportLine.SetPosition(0, controller.position);
            teleportLine.SetPosition(1, teleportTarget);
        }
    }

    bool IsValidTeleportSurface(Collider collider)
    {
        return !collider.isTrigger &&
               collider.gameObject.layer != LayerMask.NameToLayer("IgnoreTeleport");
    }

    void ExecuteTeleport()
    {
        Vector3 newPosition = teleportTarget;

        float teleportPointHeight = teleportTarget.y;

        newPosition.y = teleportPointHeight + currentGroundOffset;

        transform.position = newPosition;

        currentGroundOffset = newPosition.y - teleportPointHeight;
    }

    void StopTeleport()
    {
        isTeleporting = false;
        teleportLine.enabled = false;
        teleportMarker.SetActive(false);
    }

    Vector3 CalculateMovement(Vector2 trackpadInput)
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        return forward * trackpadInput.y + right * trackpadInput.x;
    }

    public void SetPlayerHeight(float height)
    {
        playerHeight = height;
    }

    public void AdjustHeightToSurface()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 3f, teleportLayerMask))
        {
            float surfaceHeight = hit.point.y;
            transform.position = new Vector3(transform.position.x, surfaceHeight + playerHeight, transform.position.z);
        }
    }
}