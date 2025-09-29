using UnityEngine;
using System.Collections;
using HTC.UnityPlugin.Vive;
using TMPro;
using System.Linq;

public class GasAnalyzerController : MonoBehaviour
{
    [Header("Состояние устройства")]
    public bool isPoweredOn = false;

    [Header("Ссылки на компоненты")]
    public Renderer displayRenderer;
    public TMP_Text displayText;
    public Material displayOffMaterial;
    public Material displayOnMaterial;
    public Material displayProgressMaterial;
    public Light displayLight;
    public AudioSource powerSound;

    [Header("Настройки дисплея")]
    public Color displayOffColor = Color.black;
    public Color displayOnColor = new Color(0.2f, 1f, 0.2f);
    public Color displayProgressColor = Color.yellow;
    public AnimationCurve displayFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float displayBootTime = 1.5f;

    [Header("Измерение расстояния")]
    public string dangerZoneTag = "DangerZone";
    public float updateInterval = 0.1f;
    public string distanceFormat = "DIST: {0:F1}m";
    public float warningDistance = 5f;
    public float criticalDistance = 2f;

    [Header("Дополнительные показания")]
    public bool showGasLevel = true;
    public bool showBattery = true;
    public float batteryLevel = 100f;
    public float batteryDrainRate = 0.1f;

    [Header("Визуальные эффекты")]
    public ParticleSystem powerParticles;
    public AudioClip warningSound;
    public AudioClip criticalSound;

    private bool isGrabbed = false;
    private Coroutine displayTransitionCoroutine;
    private Coroutine distanceUpdateCoroutine;
    private GameObject[] dangerZones;
    private GameObject nearestDangerZone;
    private GasAnalyzerGrabbable grabbable;
    private float currentDistance = 0f;
    private AudioSource warningAudioSource;

    void Start()
    {
        InitializeAnalyzer();

        if (grabbable == null)
            grabbable = GetComponent<GasAnalyzerGrabbable>();

        warningAudioSource = gameObject.AddComponent<AudioSource>();
        warningAudioSource.playOnAwake = false;
        warningAudioSource.loop = false;
    }

    void InitializeAnalyzer()
    {
        FindAllDangerZones();
        SetPowerState(isPoweredOn, false);
    }

    void FindAllDangerZones()
    {
        dangerZones = GameObject.FindGameObjectsWithTag(dangerZoneTag);
        if (dangerZones.Length == 0)
        {
            Debug.LogWarning($"Не найдены опасные зоны с тегом '{dangerZoneTag}'");
        }
        else
        {
            Debug.Log($"Найдено опасных зон: {dangerZones.Length}");
        }
    }

    GameObject FindNearestDangerZone()
    {
        if (dangerZones == null || dangerZones.Length == 0) return null;

        GameObject nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var zone in dangerZones)
        {
            if (zone == null) continue;

            float distance = Vector3.Distance(transform.position, zone.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = zone;
            }
        }

        return nearest;
    }

    public void TogglePower()
    {
        SetPowerState(!isPoweredOn, true);

        if (powerSound != null)
            powerSound.Play();

        if (powerParticles != null)
            powerParticles.Play();

        Debug.Log($"Газоанализатор {(isPoweredOn ? "включен" : "выключен")}");
    }

    public void SetPowerState(bool powered, bool withEffects = true)
    {
        isPoweredOn = powered;
        UpdateDisplayState();

        if (isPoweredOn)
        {
            StartDistanceMeasurement();
        }
        else
        {
            StopDistanceMeasurement();
        }

        OnPowerStateChanged?.Invoke(isPoweredOn);
    }

    public void UpdatePowerProgress(float progress)
    {
        if (displayRenderer != null && displayProgressMaterial != null)
        {
            displayRenderer.material = displayProgressMaterial;
            if (displayRenderer.material.HasProperty("_Color"))
            {
                Color progressColor = Color.Lerp(displayOffColor, displayProgressColor, progress);
                displayRenderer.material.SetColor("_Color", progressColor);
            }
        }

        if (displayLight != null)
        {
            displayLight.color = Color.Lerp(displayOffColor, displayProgressColor, progress);
            displayLight.intensity = progress * 2f;
        }

        if (displayText != null)
        {
            displayText.color = Color.Lerp(displayOffColor, displayProgressColor, progress);
            displayText.text = $"BOOT: {progress:P0}";
        }
    }

    public void ResetPowerProgress()
    {
        if (displayText != null)
        {
            if (isPoweredOn)
            {
                UpdateDisplayContent();
            }
            else
            {
                displayText.text = "";
                displayText.color = displayOffColor;
            }
        }

        if (displayRenderer != null)
        {
            if (isPoweredOn && displayOnMaterial != null)
            {
                displayRenderer.material = displayOnMaterial;
            }
            else if (!isPoweredOn && displayOffMaterial != null)
            {
                displayRenderer.material = displayOffMaterial;
            }
        }

        if (displayLight != null)
        {
            displayLight.color = isPoweredOn ? displayOnColor : displayOffColor;
            displayLight.intensity = isPoweredOn ? 1f : 0f;
        }

        Debug.Log("Прогресс включения сброшен, дисплей возвращен в нормальное состояние");
    }

    void UpdateDisplayState()
    {
        if (displayRenderer == null) return;

        if (displayTransitionCoroutine != null)
            StopCoroutine(displayTransitionCoroutine);

        displayTransitionCoroutine = StartCoroutine(DisplayTransitionRoutine());
    }

    IEnumerator DisplayTransitionRoutine()
    {
        if (displayRenderer == null) yield break;

        Material targetMaterial = isPoweredOn ? displayOnMaterial : displayOffMaterial;
        Color targetColor = isPoweredOn ? displayOnColor : displayOffColor;
        float targetIntensity = isPoweredOn ? 1f : 0f;

        if (targetMaterial != null)
            displayRenderer.material = targetMaterial;

        float timer = 0f;
        while (timer < displayBootTime)
        {
            timer += Time.deltaTime;
            float progress = displayFadeCurve.Evaluate(timer / displayBootTime);

            if (displayLight != null)
            {
                displayLight.color = targetColor;
                displayLight.intensity = Mathf.Lerp(0f, targetIntensity, progress);
            }

            yield return null;
        }
    }

    void StartDistanceMeasurement()
    {
        if (distanceUpdateCoroutine != null)
            StopCoroutine(distanceUpdateCoroutine);

        distanceUpdateCoroutine = StartCoroutine(DistanceUpdateRoutine());
    }

    void StopDistanceMeasurement()
    {
        if (distanceUpdateCoroutine != null)
            StopCoroutine(distanceUpdateCoroutine);

        if (displayText != null)
        {
            displayText.text = "OFF";
            displayText.color = displayOffColor;
        }
    }

    IEnumerator DistanceUpdateRoutine()
    {
        while (isPoweredOn)
        {
            UpdateDistanceDisplay();

            if (showBattery)
            {
                batteryLevel = Mathf.Max(0, batteryLevel - batteryDrainRate * Time.deltaTime);
                if (batteryLevel <= 0)
                {
                    SetPowerState(false);
                    yield break;
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void UpdateDistanceDisplay()
    {
        if (displayText == null) return;

        nearestDangerZone = FindNearestDangerZone();

        if (nearestDangerZone == null)
        {
            displayText.text = "NO SIGNAL";
            displayText.color = Color.yellow;
            return;
        }

        currentDistance = Vector3.Distance(transform.position, nearestDangerZone.transform.position);
        UpdateDisplayContent();
        PlayDistanceWarnings();
    }

    void UpdateDisplayContent()
    {
        if (displayText == null) return;

        string displayContent = string.Format(distanceFormat, currentDistance);

        // Добавляем уровень газа если включено
        if (showGasLevel && isPoweredOn)
        {
            // Генерируем случайный уровень газа для демонстрации
            float fakeGasLevel = Mathf.PingPong(Time.time * 0.1f, 100f);
            displayContent += $"\nGAS: {fakeGasLevel:F1}%";
        }

        // Добавляем батарею если включено
        if (showBattery && isPoweredOn)
        {
            displayContent += $"\nBAT: {batteryLevel:F0}%";
        }

        displayText.text = displayContent;
        UpdateDisplayColorByDistance();
    }

    void UpdateDisplayColorByDistance()
    {
        if (displayText == null) return;

        if (currentDistance <= criticalDistance)
        {
            displayText.color = Color.red;
            if (Mathf.PingPong(Time.time * 5f, 1f) > 0.5f)
            {
                displayText.color = Color.white;
            }
        }
        else if (currentDistance <= warningDistance)
        {
            displayText.color = Color.yellow;
        }
        else
        {
            displayText.color = Color.green;
        }
    }

    void PlayDistanceWarnings()
    {
        if (warningAudioSource == null) return;

        if (currentDistance <= criticalDistance)
        {
            if (!warningAudioSource.isPlaying || warningAudioSource.clip != criticalSound)
            {
                warningAudioSource.clip = criticalSound;
                warningAudioSource.loop = true;
                warningAudioSource.Play();
            }
        }
        else if (currentDistance <= warningDistance)
        {
            if (!warningAudioSource.isPlaying)
            {
                warningAudioSource.clip = warningSound;
                warningAudioSource.loop = false;
                warningAudioSource.Play();
            }
        }
        else
        {
            if (warningAudioSource.isPlaying)
            {
                warningAudioSource.Stop();
            }
        }
    }

    public void GrabAnalyzer(HandRole hand)
    {
        isGrabbed = true;
    }

    public void ReleaseAnalyzer()
    {
        isGrabbed = false;
    }

    public void ChargeBattery(float amount)
    {
        batteryLevel = Mathf.Clamp(batteryLevel + amount, 0f, 100f);
    }

    public float GetCurrentDistance() => currentDistance;
    public bool IsInDangerZone() => currentDistance <= criticalDistance;
    public bool IsInWarningZone() => currentDistance <= warningDistance && currentDistance > criticalDistance;

    public System.Action<bool> OnPowerStateChanged;
    public System.Action<float> OnDistanceChanged;
}