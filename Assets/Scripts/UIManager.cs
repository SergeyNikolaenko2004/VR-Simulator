using UnityEngine;
using TMPro;
using System.Collections;
using HTC.UnityPlugin.Vive;

public class UIManager : MonoBehaviour
{
    [Header("Текстовые элементы")]
    public TMP_Text hintText;
    public TMP_Text modeText;
    public TMP_Text statusText;

    [Header("Панели интерфейса")]
    public GameObject vrHintPanel;
    public GameObject desktopHintPanel;

    [Header("Настройки отображения")]
    public float hintDisplayTime = 5f;
    public float fadeDuration = 1f;

    [Header("VR Кнопки для подсказок")]
    public ControllerButton vrHintButton = ControllerButton.Menu;
    public bool useBothControllers = true;

    [Header("Тексты подсказок - VR")]
    [TextArea(3, 5)]
    public string vrGrabHint = "Чтобы взять объект: Наведите контроллер и нажмите триггер";
    [TextArea(3, 5)]
    public string vrCraneHint = "Чтобы управлять пультом: Наведите на кнопку и нажмите триггер";
    [TextArea(3, 5)]
    public string vrTeleportHint = "Чтобы телепортироваться: Нажмите тачпад на контроллере";
    [TextArea(3, 5)]
    public string vrMovementHint = "Движение: Используйте тачпад для перемещения и поворота";

    [Header("Тексты подсказок - Desktop")]
    [TextArea(3, 5)]
    public string desktopGrabHint = "Чтобы взять объект: Наведите курсором и нажмите G";
    [TextArea(3, 5)]
    public string desktopCraneHint = "Чтобы управлять пультом: Наведите курсором и нажмите левую/правую кнопку мыши";
    [TextArea(3, 5)]
    public string desktopTeleportHint = "Чтобы телепортироваться: Нажмите T и отпустите";
    [TextArea(3, 5)]
    public string desktopMovementHint = "Движение: WASD + Q/E для поворота";

    private bool isVRMode;
    private Coroutine currentHintCoroutine;
    private CanvasGroup hintCanvasGroup;
    private bool hintsVisible = false;

    void Start()
    {
        InitializeUI();
        DetectVRMode();
        ShowInitialHints();
    }

    void InitializeUI()
    {
        hintCanvasGroup = hintText.GetComponent<CanvasGroup>();
        if (hintCanvasGroup == null)
        {
            hintCanvasGroup = hintText.gameObject.AddComponent<CanvasGroup>();
        }

        if (vrHintPanel != null) vrHintPanel.SetActive(false);
        if (desktopHintPanel != null) desktopHintPanel.SetActive(false);
    }

    void DetectVRMode()
    {
        isVRMode = UnityEngine.XR.XRSettings.enabled;

        if (modeText != null)
        {
            modeText.text = isVRMode ? "Режим: VR" : "Режим: Desktop";
            modeText.color = isVRMode ? Color.green : Color.yellow;
        }

        if (vrHintPanel != null) vrHintPanel.SetActive(isVRMode);
        if (desktopHintPanel != null) desktopHintPanel.SetActive(!isVRMode);

        Debug.Log($"Режим обнаружен: {(isVRMode ? "VR" : "Desktop")}");
    }

    void Update()
    {
        HandleHintInput();
    }

    void HandleHintInput()
    {
        if (isVRMode)
        {
            if (ViveInput.GetPressDown(HandRole.RightHand, vrHintButton) ||
                (useBothControllers && ViveInput.GetPressDown(HandRole.LeftHand, vrHintButton)))
            {
                ToggleHints();
            }
        }
        else
        {  
            if (Input.GetKeyDown(KeyCode.H))
            {
                ToggleHints();
            }
        }

        if (Input.GetKeyDown(KeyCode.F1) && Application.isEditor)
        {
            ToggleVRModeDebug();
        }
    }

    void ToggleHints()
    {
        if (hintsVisible)
        {
            HideHints();
        }
        else
        {
            ShowAllHints();
        }
        hintsVisible = !hintsVisible;
    }

    void ShowInitialHints()
    {
        if (isVRMode)
        {
            ShowHint("Добро пожаловать в VR режим!\n" +
                    vrGrabHint + "\n" +
                    vrCraneHint + "\n" +
                    vrTeleportHint + "\n" +
                    vrMovementHint +
                    "\n\nНажмите кнопку Menu на контроллере для подсказок", 12f);
        }
        else
        {
            ShowHint("Добро пожаловать в Desktop режим!\n" +
                    desktopGrabHint + "\n" +
                    desktopCraneHint + "\n" +
                    desktopTeleportHint + "\n" +
                    desktopMovementHint +
                    "\n\nНажмите H для показа подсказок", 12f);
        }
    }

    public void ShowHint(string message, float displayTime = -1)
    {
        if (displayTime < 0) displayTime = hintDisplayTime;

        if (currentHintCoroutine != null)
        {
            StopCoroutine(currentHintCoroutine);
        }

        currentHintCoroutine = StartCoroutine(ShowHintCoroutine(message, displayTime));
    }

    IEnumerator ShowHintCoroutine(string message, float displayTime)
    {
        hintText.text = message;
        hintCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayTime);

        if (!hintsVisible)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                hintCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            hintCanvasGroup.alpha = 0f;
        }

        currentHintCoroutine = null;
    }

    void HideHints()
    {
        if (currentHintCoroutine != null)
        {
            StopCoroutine(currentHintCoroutine);
        }
        StartCoroutine(HideHintCoroutine());
    }

    IEnumerator HideHintCoroutine()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        hintCanvasGroup.alpha = 0f;
    }

    public void ShowTemporaryStatus(string status, float duration = 3f)
    {
        if (statusText != null)
        {
            StartCoroutine(ShowStatusCoroutine(status, duration));
        }
    }

    IEnumerator ShowStatusCoroutine(string status, float duration)
    {
        statusText.text = status;
        statusText.color = Color.white;

        yield return new WaitForSeconds(duration);

        float timer = 0f;
        Color originalColor = statusText.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            statusText.color = Color.Lerp(originalColor, new Color(1, 1, 1, 0), timer / fadeDuration);
            yield return null;
        }

        statusText.text = "";
        statusText.color = originalColor;
    }

    public void ShowGrabHint()
    {
        ShowHint(isVRMode ? vrGrabHint : desktopGrabHint);
    }

    public void ShowCraneHint()
    {
        ShowHint(isVRMode ? vrCraneHint : desktopCraneHint);
    }

    public void ShowTeleportHint()
    {
        ShowHint(isVRMode ? vrTeleportHint : desktopTeleportHint);
    }

    public void ShowMovementHint()
    {
        ShowHint(isVRMode ? vrMovementHint : desktopMovementHint);
    }

    public void ShowObjectSpecificHint(string objectName)
    {
        string hint = isVRMode ?
            $"Взаимодействие с {objectName}: Наведите контроллер и используйте триггер" :
            $"Взаимодействие с {objectName}: Наведите курсор и используйте клавиши управления";

        ShowHint(hint);
    }

    void ShowAllHints()
    {
        if (isVRMode)
        {
            ShowHint(vrGrabHint + "\n\n" + vrCraneHint + "\n\n" + vrTeleportHint + "\n\n" + vrMovementHint +
                    "\n\nНажмите кнопку Menu для скрытия", 0f); 
        }
        else
        {
            ShowHint(desktopGrabHint + "\n\n" + desktopCraneHint + "\n\n" + desktopTeleportHint + "\n\n" + desktopMovementHint +
                    "\n\nНажмите H для скрытия", 0f); 
        }
    }

    void ToggleVRModeDebug()
    {
        isVRMode = !isVRMode;
        DetectVRMode();
        ShowHint($"Режим переключен на: {(isVRMode ? "VR" : "Desktop")}", 3f);
    }
}