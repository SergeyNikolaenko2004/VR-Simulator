using HTC.UnityPlugin.Vive;
using UnityEngine;

public class ControlButton : MonoBehaviour
{
    public enum Direction { Up, Down, East, West, North, South }
    public Direction buttonDirection;

    public Material originalMaterial;
    public Material pressedMaterial;
    public Material highlightMaterial;
    public Renderer buttonRenderer;

    [Header("Подсказки")]
    public string buttonDescription = "";

    private bool isControllerNear = false;
    private HandRole nearbyControllerHand;
    private UIManager uiManager;

    void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        uiManager = FindObjectOfType<UIManager>();

        if (originalMaterial == null && buttonRenderer != null)
            originalMaterial = buttonRenderer.material;

        if (string.IsNullOrEmpty(buttonDescription))
        {
            buttonDescription = GetDefaultDescription();
        }
    }

    string GetDefaultDescription()
    {
        switch (buttonDirection)
        {
            case Direction.Up: return "Поднять крюк";
            case Direction.Down: return "Опустить крюк";
            case Direction.East: return "Движение вправо";
            case Direction.West: return "Движение влево";
            case Direction.North: return "Движение вперед";
            case Direction.South: return "Движение назад";
            default: return "Кнопка управления";
        }
    }

    void Update()
    {
        bool isVRMode = UnityEngine.XR.XRSettings.enabled;

        if (isVRMode || !Application.isEditor)
        {
            if (isControllerNear)
            {
                if (ViveInput.GetPressDown(nearbyControllerHand, ControllerButton.Trigger))
                {
                    PressButton();
                    if (buttonRenderer != null && pressedMaterial != null)
                        buttonRenderer.material = pressedMaterial;
                }

                if (ViveInput.GetPressUp(nearbyControllerHand, ControllerButton.Trigger))
                {
                    ReleaseButton();
                    if (buttonRenderer != null && originalMaterial != null)
                        buttonRenderer.material = originalMaterial;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isControllerNear = true;
            nearbyControllerHand = other.name.Contains("Left") ? HandRole.LeftHand : HandRole.RightHand;

            if (buttonRenderer != null && highlightMaterial != null)
                buttonRenderer.material = highlightMaterial;

            if (uiManager != null)
            {
                string hint = UnityEngine.XR.XRSettings.enabled ?
                    $"Нажмите триггер для: {buttonDescription}" :
                    $"Нажмите левую/правую кнопку мыши для: {buttonDescription}";

                uiManager.ShowTemporaryStatus(hint, 3f);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isControllerNear = false;
            if (buttonRenderer != null && originalMaterial != null)
                buttonRenderer.material = originalMaterial;
        }
    }

    public void PressButton()
    {
        switch (buttonDirection)
        {
            case Direction.Up: EventManager.PressMoveUp(); break;
            case Direction.Down: EventManager.PressMoveDown(); break;
            case Direction.East: EventManager.PressMoveEast(); break;
            case Direction.West: EventManager.PressMoveWest(); break;
            case Direction.North: EventManager.PressMoveNorth(); break;
            case Direction.South: EventManager.PressMoveSouth(); break;
        }

        if (uiManager != null)
            uiManager.ShowTemporaryStatus($"{buttonDescription} - АКТИВНО", 1f);

        Debug.Log($"Кнопка {buttonDirection} нажата");
    }

    public void ReleaseButton()
    {
        switch (buttonDirection)
        {
            case Direction.Up: EventManager.ReleaseMoveUp(); break;
            case Direction.Down: EventManager.ReleaseMoveDown(); break;
            case Direction.East: EventManager.ReleaseMoveEast(); break;
            case Direction.West: EventManager.ReleaseMoveWest(); break;
            case Direction.North: EventManager.ReleaseMoveNorth(); break;
            case Direction.South: EventManager.ReleaseMoveSouth(); break;
        }

        if (uiManager != null)
            uiManager.ShowTemporaryStatus($"{buttonDescription} - остановлено", 1f);

        Debug.Log($"Кнопка {buttonDirection} отпущена");
    }
}