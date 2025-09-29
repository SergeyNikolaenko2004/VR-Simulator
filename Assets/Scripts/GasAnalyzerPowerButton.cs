using System.Collections;
using HTC.UnityPlugin.Vive;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class GasAnalyzerPowerButton : MonoBehaviour
{
    [Header("Настройки кнопки")]
    public float holdTimeToToggle = 3f;

    [Header("Визуальные материалы")]
    public Material normalMaterial;
    public Material pressedMaterial;
    public Material progressMaterial;
    public Renderer buttonRenderer;

    [Header("Ссылки")]
    public GasAnalyzerController gasAnalyzer;
    public Transform buttonTransform;
    public float pressDepth = 0.005f;

    private bool isControllerNear = false;
    private HandRole nearbyControllerHand;
    private Vector3 originalButtonPosition;
    private Vector3 pressedButtonPosition;
    private bool isPressed = false;
    private float holdTimer = 0f;
    private Coroutine holdCoroutine;

    void Start()
    {
        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        if (buttonTransform == null)
            buttonTransform = transform;

        if (gasAnalyzer == null)
            gasAnalyzer = GetComponentInParent<GasAnalyzerController>();

        originalButtonPosition = buttonTransform.localPosition;
        pressedButtonPosition = originalButtonPosition - Vector3.forward * pressDepth;

        if (buttonRenderer != null && normalMaterial != null)
            buttonRenderer.material = normalMaterial;
    }

    void Update()
    {
        bool isVRMode = UnityEngine.XR.XRSettings.enabled;

        if ((isVRMode || !Application.isEditor) && isControllerNear)
        {
            if (ViveInput.GetPressDown(nearbyControllerHand, HTC.UnityPlugin.Vive.ControllerButton.Trigger))
            {
                StartButtonPress();
            }

            if (ViveInput.GetPressUp(nearbyControllerHand, HTC.UnityPlugin.Vive.ControllerButton.Trigger))
            {
                EndButtonPress();
            }
        }

        UpdateButtonAnimation();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isControllerNear = true;
            nearbyControllerHand = other.name.Contains("Left") ? HandRole.LeftHand : HandRole.RightHand;

            if (buttonRenderer != null && progressMaterial != null && !isPressed)
                buttonRenderer.material = progressMaterial;

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isControllerNear = false;

            if (!isPressed && buttonRenderer != null && normalMaterial != null)
                buttonRenderer.material = normalMaterial;

        }
    }

    public void StartButtonPress()
    {
        if (!isPressed && gasAnalyzer != null)
        {
            isPressed = true;
            holdTimer = 0f;

            holdCoroutine = StartCoroutine(HoldButtonRoutine());

            if (buttonRenderer != null && pressedMaterial != null)
                buttonRenderer.material = pressedMaterial;

        }
    }

    public void EndButtonPress()
    {
        if (isPressed)
        {
            isPressed = false;
            holdTimer = 0f;

            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
                holdCoroutine = null;
            }

            if (holdTimer < holdTimeToToggle)
            {
                gasAnalyzer.ResetPowerProgress();
            }

            if (buttonRenderer != null)
            {
                if (isControllerNear && progressMaterial != null)
                    buttonRenderer.material = progressMaterial;
                else if (normalMaterial != null)
                    buttonRenderer.material = normalMaterial;
            }
        }
    }

    IEnumerator HoldButtonRoutine()
    {
        while (isPressed && holdTimer < holdTimeToToggle)
        {
            holdTimer += Time.deltaTime;
            float progress = holdTimer / holdTimeToToggle;

            gasAnalyzer.UpdatePowerProgress(progress);

            yield return null;
        }

        if (isPressed && holdTimer >= holdTimeToToggle)
        {
            gasAnalyzer.TogglePower();
            EndButtonPress();
        }
    }

    void UpdateButtonAnimation()
    {
        if (buttonTransform == null) return;

        Vector3 targetPosition = isPressed ? pressedButtonPosition : originalButtonPosition;
        buttonTransform.localPosition = Vector3.Lerp(
            buttonTransform.localPosition,
            targetPosition,
            10f * Time.deltaTime
        );
    }

    public void EditorPressButton()
    {
        if (Application.isEditor && !UnityEngine.XR.XRSettings.enabled)
        {
            StartButtonPress();
        }
    }

    public void EditorReleaseButton()
    {
        if (Application.isEditor && !UnityEngine.XR.XRSettings.enabled)
        {
            EndButtonPress();
        }
    }
}