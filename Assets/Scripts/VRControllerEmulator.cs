using UnityEngine;
using HTC.UnityPlugin.Vive;

public class VRControllerEmulator : MonoBehaviour
{
    [Header("Controller Emulation Settings")]
    public HandRole handRole;
    public float controllerMoveSpeed = 5f;
    public float controllerRotationSpeed = 90f;

    [Header("Visual Settings")]
    public bool showLaserPointer = true;
    public LineRenderer laserPointer;
    public GameObject controllerModel;

    [Header("Управление захватом")]
    public KeyCode grabKey = KeyCode.G;

    [Header("Ссылка на UI Manager")]
    public UIManager uiManager;

    private ControlButton currentButton;
    private ControlButton previousButton;
    private bool isInVRMode = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isTriggerPressed = false;
    private GasAnalyzerGrabbable currentGrabbable;
    private GasAnalyzerProbe currentProbe;

    void Start()
    {
        isInVRMode = UnityEngine.XR.XRSettings.enabled;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (showLaserPointer && Application.isEditor && !isInVRMode)
        {
            CreateLaserPointer();
        }

        if (controllerModel != null && Application.isEditor && !isInVRMode)
        {
            controllerModel.SetActive(false);
        }

        if (uiManager != null && !isInVRMode)
        {
            uiManager.ShowHint("Используйте мышь для управления контроллерами\nH - Открыть список подсказок", 6f);
        }
    }

    void Update()
    {
        if (Application.isEditor && !isInVRMode)
        {
            HandleEditorControllerMovement();
            EmulateController();
            HandleGrabInput();
        }
    }

    void HandleGrabInput()
    {
        if (Input.GetKeyDown(grabKey))
        {
            if (handRole == HandRole.LeftHand)
            {
                ToggleGasAnalyzerGrab();
            }
            else if (handRole == HandRole.RightHand)
            {
                ToggleProbeGrab();
            }
        }
    }

    void ToggleGasAnalyzerGrab()
    {
        if (currentGrabbable != null && currentGrabbable.IsGrabbed())
        {
            currentGrabbable.Release();
            currentGrabbable = null;
            UpdateLaserPointerColor(Color.red);

            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Газоанализатор отпущен", 2f);
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        GasAnalyzerGrabbable grabbable = null;
        if (Physics.Raycast(ray, out hit, 3f))
        {
            grabbable = hit.collider.GetComponent<GasAnalyzerGrabbable>();
            if (grabbable == null)
            {
                grabbable = hit.collider.GetComponentInParent<GasAnalyzerGrabbable>();
            }
        }

        if (grabbable != null && !grabbable.IsGrabbed() && !grabbable.IsReturning())
        {
            grabbable.Grab();
            currentGrabbable = grabbable;
            UpdateLaserPointerColor(Color.green);

            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Газоанализатор взят", 2f);
        }
        else if (uiManager != null && grabbable == null)
        {
            uiManager.ShowTemporaryStatus("Объект не найден", 1f);
        }
    }

    void ToggleProbeGrab()
    {
        if (currentProbe != null && currentProbe.IsGrabbed())
        {
            currentProbe.Release();
            currentProbe = null;
            UpdateLaserPointerColor(Color.blue);

            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Зонд отпущен", 2f);
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        GasAnalyzerProbe probe = null;
        if (Physics.Raycast(ray, out hit, 3f))
        {
            probe = hit.collider.GetComponent<GasAnalyzerProbe>();
            if (probe == null)
            {
                probe = hit.collider.GetComponentInParent<GasAnalyzerProbe>();
            }
        }

        if (probe != null && !probe.IsGrabbed() && !probe.IsReturning())
        {
            probe.Grab();
            currentProbe = probe;
            UpdateLaserPointerColor(Color.cyan);

            if (uiManager != null)
                uiManager.ShowTemporaryStatus("Зонд взят", 2f);
        }
        else if (uiManager != null && probe == null)
        {
            uiManager.ShowTemporaryStatus("Зонд не найден", 1f);
        }
    }

    void UpdateLaserPointerColor(Color color)
    {
        if (laserPointer != null)
        {
            laserPointer.startColor = color;
            laserPointer.endColor = color;
        }
    }

    void HandleEditorControllerMovement()
    {
        Transform cameraTransform = Camera.main.transform;

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }

        float moveX = Input.GetAxis("Mouse X") * controllerMoveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Mouse Y") * controllerMoveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(moveX * 0.1f, moveY * 0.1f, 0, Space.Self);
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            transform.Rotate(moveY * controllerRotationSpeed * Time.deltaTime,
                           -moveX * controllerRotationSpeed * Time.deltaTime, 0, Space.Self);
        }
        else
        {
            transform.Translate(moveX, moveY, 0, Space.Self);
        }

        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(0, 0, controllerMoveSpeed * Time.deltaTime, Space.Self);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(0, 0, -controllerMoveSpeed * Time.deltaTime, Space.Self);
        }
    }

    void CreateLaserPointer()
    {
        laserPointer = gameObject.AddComponent<LineRenderer>();
        laserPointer.positionCount = 2;
        laserPointer.startWidth = 0.01f;
        laserPointer.endWidth = 0.005f;
        laserPointer.material = new Material(Shader.Find("Sprites/Default"));
        laserPointer.startColor = handRole == HandRole.LeftHand ? Color.red : Color.blue;
        laserPointer.endColor = Color.white;
    }

    void EmulateController()
    {
        PositionControllerInEditor();

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (laserPointer != null)
        {
            laserPointer.SetPosition(0, transform.position);
            if (Physics.Raycast(ray, out hit, 10f))
            {
                laserPointer.SetPosition(1, hit.point);
            }
            else
            {
                laserPointer.SetPosition(1, transform.position + transform.forward * 10f);
            }
        }

        HandleGasAnalyzerInput();

        ControlButton newButton = null;
        if (Physics.Raycast(ray, out hit, 10f))
        {
            newButton = hit.collider.GetComponent<ControlButton>();
        }

        HandleButtonHighlight(newButton);
        HandleTriggerInput();
    }

    void HandleButtonHighlight(ControlButton newButton)
    {
        if (newButton != currentButton)
        {
            if (currentButton != null && isTriggerPressed)
            {
                currentButton.ReleaseButton();
                isTriggerPressed = false;

                if (currentButton.buttonRenderer != null && currentButton.originalMaterial != null)
                {
                    currentButton.buttonRenderer.material = currentButton.originalMaterial;
                }
            }

            if (currentButton != null && currentButton.buttonRenderer != null)
            {
                currentButton.buttonRenderer.material = currentButton.originalMaterial;
            }

            previousButton = currentButton;
            currentButton = newButton;

            if (currentButton != null && currentButton.buttonRenderer != null &&
                currentButton.highlightMaterial != null && !isTriggerPressed)
            {
                currentButton.buttonRenderer.material = currentButton.highlightMaterial;
            }
        }
    }

    void HandleTriggerInput()
    {
        KeyCode triggerKey = handRole == HandRole.LeftHand ? KeyCode.Mouse0 : KeyCode.Mouse1;

        if (Input.GetKeyDown(triggerKey) && currentButton != null && !isTriggerPressed)
        {
            currentButton.PressButton();
            isTriggerPressed = true;

            if (currentButton.buttonRenderer != null && currentButton.pressedMaterial != null)
            {
                currentButton.buttonRenderer.material = currentButton.pressedMaterial;
            }
        }

        if (Input.GetKeyUp(triggerKey) && currentButton != null && isTriggerPressed)
        {
            currentButton.ReleaseButton();
            isTriggerPressed = false;

            if (currentButton.buttonRenderer != null)
            {
                if (currentButton.highlightMaterial != null)
                {
                    currentButton.buttonRenderer.material = currentButton.highlightMaterial;
                }
                else if (currentButton.originalMaterial != null)
                {
                    currentButton.buttonRenderer.material = currentButton.originalMaterial;
                }
            }
        }

        if (isTriggerPressed && currentButton == null)
        {
            isTriggerPressed = false;
        }
    }

    void PositionControllerInEditor()
    {
        Transform cameraTransform = Camera.main.transform;

        if (handRole == HandRole.LeftHand)
        {
            transform.position = cameraTransform.position +
                cameraTransform.right * -0.3f +
                cameraTransform.up * -0.2f +
                cameraTransform.forward * 0.5f;
        }
        else
        {
            transform.position = cameraTransform.position +
                cameraTransform.right * 0.3f +
                cameraTransform.up * -0.2f +
                cameraTransform.forward * 0.5f;
        }

        transform.rotation = cameraTransform.rotation;
    }

    void HandleGasAnalyzerInput()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        GasAnalyzerPowerButton powerButton = null;
        if (Physics.Raycast(ray, out hit, 10f))
        {
            powerButton = hit.collider.GetComponent<GasAnalyzerPowerButton>();
        }

        if (powerButton != null)
        {
            KeyCode triggerKey = handRole == HandRole.LeftHand ? KeyCode.Mouse0 : KeyCode.Mouse1;

            if (Input.GetKeyDown(triggerKey))
            {
                powerButton.EditorPressButton();
            }

            if (Input.GetKeyUp(triggerKey))
            {
                powerButton.EditorReleaseButton();
            }
        }
    }


}