using UnityEngine;
using HTC.UnityPlugin.Vive;

public class VRCameraTester : MonoBehaviour
{
    [Header("VR Camera Test")]
    public bool testCameraRotation = true;
    public float mouseSensitivity = 2f;

    private Vector3 rotation = Vector3.zero;
    private bool isVRMode = false;

    void Start()
    {
        isVRMode = UnityEngine.XR.XRSettings.enabled;
    }

    void Update()
    {
        if (testCameraRotation && Application.isEditor && !isVRMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rotation.x -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                rotation.y += Input.GetAxis("Mouse X") * mouseSensitivity;
                rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);

                transform.localEulerAngles = rotation;
            }
        }
    }
}