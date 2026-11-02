using UnityEngine;

public partial class GameCharacter : MonoBehaviour
{
    // input manager
    public InputManager inputManager;

    public bool cameraY = true;
    public bool gravity = true;
    public bool drive = true;

    public CharacterController characterController;
    public Transform characterLocation;
    public Transform characterCam;

    public float mouseSensitivity = 5.0f;
    public float speed = 10;

    // Camera look limits in degrees: negative = looking up, positive = looking down
    public float minPitch = -10f;
    public float maxPitch = 50f;

    // Testing switch: when true the mouse does not move the camera at all (look code is kept, just skipped)
    public bool staticCamera = true;

    private float pitch;
    private bool mouseLook;
    private int ignoreMouseFrames;

    void Start()
    {
        var speedo = gameObject.AddComponent<Speedometer>();
        speedo.target = characterLocation;
        speedo.maxWorldSpeed = topSpeed * speedConverter * speed; // 50 km/h shown at top speed
        // start from the camera's current pitch (-180..180) instead of Euler 0..360
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, characterCam.localEulerAngles.x), minPitch, maxPitch);
        // with on-screen touch buttons the cursor must stay free so they can be clicked
        SetCursorLocked(!inputManager.TouchControlsActive);
    }

    // When the window regains focus Unity reports one huge mouse movement; skip a couple of frames.
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ignoreMouseFrames = 2;
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        ignoreMouseFrames = 2;
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
        else if (!inputManager.TouchControlsActive && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);

        mouseLook = !staticCamera && Cursor.lockState == CursorLockMode.Locked && ignoreMouseFrames <= 0;
        if (ignoreMouseFrames > 0) ignoreMouseFrames--;
    }

    // Update is called once per frame
    void Update()
    {
        HandleCursor();
        if (gravity) ApplyGravity();
        if (drive)
        {
            Drive();
        }
        else
        {
            Move();
            Rotate();
        }
        if (cameraY) RotateY();
    }
    private void ApplyGravity()
    {
        Vector3 move = -20f * Time.deltaTime * characterLocation.transform.up;

        characterController.Move(move);
    }
    private void Move()
    {
        float horz = inputManager.Horizontal;
        float vert = inputManager.Vertical;

        Vector3 move = characterLocation.transform.forward * vert + transform.right * horz;
        
        characterController.Move(speed * Time.deltaTime * move);
    }
    private void Rotate()
    {
        if (!mouseLook) return;
        float horz = inputManager.MouseX;
        characterLocation.transform.Rotate(0, horz * mouseSensitivity, 0); // good for rotate X
    }
    private void RotateY()
    {
        if (!mouseLook) return;

        // moving the mouse up (positive) looks up (negative pitch)
        pitch = Mathf.Clamp(pitch - inputManager.MouseY * mouseSensitivity, minPitch, maxPitch);
        characterCam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

}
