using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("Скорости движения")]
    public float upDownSpeed = 2.0f;
    public float leftRightSpeed = 1.5f;
    public float forwardBackSpeed = 1.5f;

    [Header("Ссылки на объекты")]
    public Transform beam;
    public Transform craneMover;
    public Transform hook;
    public Transform cableReel;
    public Transform wire;

    [Header("Настройки кабеля")]
    public float cableRotateSpeed = 100f;
    public float wireStretchSpeed = 2.0f;

    [Header("Звуковые эффекты")]
    public AudioSource cableSound;
    public AudioClip cableWindSound;
    public AudioClip cableUnwindSound;
    public float soundFadeSpeed = 2f;

    [Header("Настройки громкости")]
    public float maxVolume = 0.7f;

    [Header("Ограничения движения")]
    public float maxHookHeight = 10f;
    public float minHookHeight = 1f;
    public float maxLeftPosition = -5f;
    public float maxRightPosition = 5f;
    public float maxForwardPosition = 10f;
    public float maxBackwardPosition = -10f;

    [Header("Границы для отладки")]
    public bool showGizmos = true;
    public Color boundariesColor = Color.red;

    private bool isMovingUp, isMovingDown, isMovingEast, isMovingWest, isMovingNorth, isMovingSouth;
    private Vector3 wireOriginalScale;
    private float currentWireLength = 1f;
    private Vector3 initialBeamPosition;
    private Vector3 initialCraneMoverPosition;
    private Vector3 initialHookPosition;

    private bool isAtUpperLimit = false;
    private bool isAtLowerLimit = false;
    private bool isAtLeftLimit = false;
    private bool isAtRightLimit = false;
    private bool isAtForwardLimit = false;
    private bool isAtBackwardLimit = false;

    private bool wasMovingLastFrame = false;
    private float targetSoundVolume = 0f;
    private bool isWinding = false;

    void Start()
    {
        if (wire != null)
        {
            wireOriginalScale = wire.localScale;
            currentWireLength = wireOriginalScale.y;
        }

        initialBeamPosition = beam.position;
        initialCraneMoverPosition = craneMover.position;
        initialHookPosition = hook.position;

        InitializeAudio();
    }

    void InitializeAudio()
    {
        if (cableSound == null)
        {
            cableSound = gameObject.AddComponent<AudioSource>();
            cableSound.playOnAwake = false;
            cableSound.loop = true;
            cableSound.spatialBlend = 1f; 
            cableSound.volume = 0f; 
        }

        LoadAudioClips();
    }

    void LoadAudioClips()
    {
        if (cableWindSound == null)
        {
            cableWindSound = Resources.Load<AudioClip>("Sounds/crane_wind");
            if (cableWindSound != null)
                Debug.Log("Звук наматывания загружен: " + cableWindSound.name);
            else
                Debug.LogWarning("Не найден звук наматывания: Sounds/crane_wind");
        }

        if (cableUnwindSound == null)
        {
            cableUnwindSound = Resources.Load<AudioClip>("Sounds/crane_unwind");
            if (cableUnwindSound != null)
                Debug.Log("Звук разматывания загружен: " + cableUnwindSound.name);
            else
                Debug.LogWarning("Не найден звук разматывания: Sounds/crane_unwind");
        }

    }

    void OnEnable()
    {
        EventManager.MoveUpPressed += () => isMovingUp = true;
        EventManager.MoveUpReleased += () => isMovingUp = false;
        EventManager.MoveDownPressed += () => isMovingDown = true;
        EventManager.MoveDownReleased += () => isMovingDown = false;
        EventManager.MoveEastPressed += () => isMovingEast = true;
        EventManager.MoveEastReleased += () => isMovingEast = false;
        EventManager.MoveWestPressed += () => isMovingWest = true;
        EventManager.MoveWestReleased += () => isMovingWest = false;
        EventManager.MoveNorthPressed += () => isMovingNorth = true;
        EventManager.MoveNorthReleased += () => isMovingNorth = false;
        EventManager.MoveSouthPressed += () => isMovingSouth = true;
        EventManager.MoveSouthReleased += () => isMovingSouth = false;
    }

    void OnDisable()
    {
        EventManager.MoveUpPressed -= () => isMovingUp = true;
        EventManager.MoveUpReleased -= () => isMovingUp = false;
        EventManager.MoveDownPressed -= () => isMovingDown = true;
        EventManager.MoveDownReleased -= () => isMovingDown = false;
        EventManager.MoveEastPressed -= () => isMovingEast = true;
        EventManager.MoveEastReleased -= () => isMovingEast = false;
        EventManager.MoveWestPressed -= () => isMovingWest = true;
        EventManager.MoveWestReleased -= () => isMovingWest = false;
        EventManager.MoveNorthPressed -= () => isMovingNorth = true;
        EventManager.MoveNorthReleased -= () => isMovingNorth = false;
        EventManager.MoveSouthPressed -= () => isMovingSouth = true;
        EventManager.MoveSouthReleased -= () => isMovingSouth = false;
    }

    void Update()
    {
        HandleCraneMovement();
        HandleCableRotation();
        HandleWireStretching();
        HandleCableSound();
    }

    void HandleCraneMovement()
    {
        ResetLimitFlags();

        if (isMovingNorth)
        {
            float newZ = beam.position.z + forwardBackSpeed * Time.deltaTime;
            if (newZ <= initialBeamPosition.z + maxForwardPosition)
            {
                beam.Translate(Vector3.forward * forwardBackSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                isAtForwardLimit = true;
            }
        }

        if (isMovingSouth)
        {
            float newZ = beam.position.z - forwardBackSpeed * Time.deltaTime;
            if (newZ >= initialBeamPosition.z + maxBackwardPosition)
            {
                beam.Translate(Vector3.back * forwardBackSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                isAtBackwardLimit = true;
            }
        }

        if (isMovingEast)
        {
            float newX = craneMover.position.x + leftRightSpeed * Time.deltaTime;
            if (newX <= initialCraneMoverPosition.x + maxRightPosition)
            {
                craneMover.Translate(Vector3.right * leftRightSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                isAtRightLimit = true;
            }
        }

        if (isMovingWest)
        {
            float newX = craneMover.position.x - leftRightSpeed * Time.deltaTime;
            if (newX >= initialCraneMoverPosition.x + maxLeftPosition)
            {
                craneMover.Translate(Vector3.left * leftRightSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                isAtLeftLimit = true;
            }
        }

        if (isMovingUp)
        {
            float newY = hook.position.y + upDownSpeed * Time.deltaTime;
            if (newY <= initialHookPosition.y + maxHookHeight)
            {
                hook.Translate(Vector3.up * upDownSpeed * Time.deltaTime, Space.World);
                isWinding = true;
            }
            else
            {
                isAtUpperLimit = true;
            }
        }

        if (isMovingDown)
        {
            float newY = hook.position.y - upDownSpeed * Time.deltaTime;
            if (newY >= initialHookPosition.y + minHookHeight)
            {
                hook.Translate(Vector3.down * upDownSpeed * Time.deltaTime, Space.World);
                isWinding = false;
            }
            else
            {
                isAtLowerLimit = true;
            }
        }
    }

    void HandleCableRotation()
    {
        float rotationAmount = 0f;

        if (isMovingUp && !isAtUpperLimit)
        {
            rotationAmount = -cableRotateSpeed * Time.deltaTime;
        }
        else if (isMovingDown && !isAtLowerLimit)
        {
            rotationAmount = cableRotateSpeed * Time.deltaTime;
        }

        if (rotationAmount != 0f && cableReel != null)
        {
            cableReel.Rotate(rotationAmount, 0, 0, Space.Self);
        }
    }

    void HandleWireStretching()
    {
        if (wire == null) return;

        if (isMovingDown && !isAtLowerLimit)
        {
            currentWireLength += wireStretchSpeed * Time.deltaTime;
        }
        else if (isMovingUp && !isAtUpperLimit)
        {
            currentWireLength = Mathf.Max(0.1f, currentWireLength - wireStretchSpeed * Time.deltaTime);
        }

        wire.localScale = new Vector3(wireOriginalScale.x, currentWireLength, wireOriginalScale.z);
        UpdateWirePosition();
    }

    void HandleCableSound()
    {
        bool isMovingNow = (isMovingUp && !isAtUpperLimit) || (isMovingDown && !isAtLowerLimit);

        targetSoundVolume = isMovingNow ? maxVolume : 0f;

        if (cableSound != null)
        {
            cableSound.volume = Mathf.Lerp(cableSound.volume, targetSoundVolume, soundFadeSpeed * Time.deltaTime);

            if (isMovingNow && !cableSound.isPlaying)
            {
                PlayAppropriateSound();
            }
            else if (!isMovingNow && cableSound.volume < 0.01f && cableSound.isPlaying)
            {
                cableSound.Stop();
            }

            if (cableSound.isPlaying)
            {
                AdjustSoundPitch();
            }
        }

        wasMovingLastFrame = isMovingNow;
    }

    void PlayAppropriateSound()
    {
        if (isMovingUp && cableWindSound != null)
        {
            if (cableSound.clip != cableWindSound)
            {
                cableSound.clip = cableWindSound;
            }
            cableSound.Play();
        }
        else if (isMovingDown && cableUnwindSound != null)
        {
            if (cableSound.clip != cableUnwindSound)
            {
                cableSound.clip = cableUnwindSound;
            }
            cableSound.Play();
        }
        else
        {
            Debug.LogWarning("Звуковые клипы не назначены!");
        }
    }

    void AdjustSoundPitch()
    {
        float speedMultiplier = isMovingUp ? upDownSpeed : (isMovingDown ? upDownSpeed : 0f);
        float targetPitch = Mathf.Lerp(0.8f, 1.2f, speedMultiplier / 2f);
        cableSound.pitch = Mathf.Lerp(cableSound.pitch, targetPitch, soundFadeSpeed * Time.deltaTime);
    }

    void UpdateWirePosition()
    {
        if (wire == null || hook == null) return;
        Vector3 wirePosition = wire.localPosition;
        wirePosition.y = -currentWireLength;
        wire.localPosition = wirePosition;
    }

    void ResetLimitFlags()
    {
        isAtUpperLimit = false;
        isAtLowerLimit = false;
        isAtLeftLimit = false;
        isAtRightLimit = false;
        isAtForwardLimit = false;
        isAtBackwardLimit = false;
    }
    public void TestWindSound()
    {
        if (cableWindSound != null && cableSound != null)
        {
            cableSound.clip = cableWindSound;
            cableSound.volume = maxVolume;
            cableSound.Play();
            Debug.Log("Тест звука наматывания");
        }
    }

    public void TestUnwindSound()
    {
        if (cableUnwindSound != null && cableSound != null)
        {
            cableSound.clip = cableUnwindSound;
            cableSound.volume = maxVolume;
            cableSound.Play();
            Debug.Log("Тест звука разматывания");
        }
    }

    public void SetMovementLimits(float maxHeight, float minHeight, float maxLeft, float maxRight, float maxForward, float maxBackward)
    {
        maxHookHeight = maxHeight;
        minHookHeight = minHeight;
        maxLeftPosition = maxLeft;
        maxRightPosition = maxRight;
        maxForwardPosition = maxForward;
        maxBackwardPosition = maxBackward;
    }

    public bool IsAtUpperLimit() => isAtUpperLimit;
    public bool IsAtLowerLimit() => isAtLowerLimit;
    public bool IsAtLeftLimit() => isAtLeftLimit;
    public bool IsAtRightLimit() => isAtRightLimit;
    public bool IsAtForwardLimit() => isAtForwardLimit;
    public bool IsAtBackwardLimit() => isAtBackwardLimit;

    public void StopCableSound()
    {
        if (cableSound != null && cableSound.isPlaying)
        {
            cableSound.Stop();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || beam == null || craneMover == null || hook == null) return;

        Gizmos.color = boundariesColor;

        Vector3 beamPos = Application.isPlaying ? initialBeamPosition : beam.position;
        Vector3 craneMoverPos = Application.isPlaying ? initialCraneMoverPosition : craneMover.position;
        Vector3 hookPos = Application.isPlaying ? initialHookPosition : hook.position;

        Vector3 beamMin = beamPos + Vector3.forward * maxBackwardPosition;
        Vector3 beamMax = beamPos + Vector3.forward * maxForwardPosition;
        Gizmos.DrawLine(beamMin, beamMax);
        Gizmos.DrawWireCube((beamMin + beamMax) / 2, new Vector3(1f, 0.1f, maxForwardPosition - maxBackwardPosition));

        Vector3 craneMin = craneMoverPos + Vector3.right * maxLeftPosition;
        Vector3 craneMax = craneMoverPos + Vector3.right * maxRightPosition;
        Gizmos.DrawLine(craneMin, craneMax);
        Gizmos.DrawWireCube((craneMin + craneMax) / 2, new Vector3(maxRightPosition - maxLeftPosition, 0.1f, 1f));

        Vector3 hookMin = hookPos + Vector3.up * minHookHeight;
        Vector3 hookMax = hookPos + Vector3.up * maxHookHeight;
        Gizmos.DrawLine(hookMin, hookMax);
        Gizmos.DrawWireCube((hookMin + hookMax) / 2, new Vector3(0.5f, maxHookHeight - minHookHeight, 0.5f));

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            if (isAtUpperLimit) Gizmos.DrawSphere(hookMax, 0.3f);
            if (isAtLowerLimit) Gizmos.DrawSphere(hookMin, 0.3f);
            if (isAtLeftLimit) Gizmos.DrawSphere(craneMin, 0.3f);
            if (isAtRightLimit) Gizmos.DrawSphere(craneMax, 0.3f);
            if (isAtForwardLimit) Gizmos.DrawSphere(beamMax, 0.3f);
            if (isAtBackwardLimit) Gizmos.DrawSphere(beamMin, 0.3f);
        }
    }

    public float GetHookHeightNormalized()
    {
        float currentHeight = hook.position.y - initialHookPosition.y;
        return Mathf.Clamp01((currentHeight - minHookHeight) / (maxHookHeight - minHookHeight));
    }

    public float GetCraneMoverPositionNormalized()
    {
        float currentPos = craneMover.position.x - initialCraneMoverPosition.x;
        return Mathf.Clamp01((currentPos - maxLeftPosition) / (maxRightPosition - maxLeftPosition));
    }

    public float GetBeamPositionNormalized()
    {
        float currentPos = beam.position.z - initialBeamPosition.z;
        return Mathf.Clamp01((currentPos - maxBackwardPosition) / (maxForwardPosition - maxBackwardPosition));
    }
}