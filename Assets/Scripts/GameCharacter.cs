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

    // Start is called before the first frame update
    //void Start()
    //{

    //}

    // Update is called once per frame
    void Update()
    {
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
        float horz = inputManager.MouseX;
        characterLocation.transform.Rotate(0, horz * mouseSensitivity, 0); // good for rotate X
    }
    private void FixUpsideDownCamera()
    {
        if (characterCam.localEulerAngles.z == 180)
            characterCam.Rotate(0, -characterCam.localEulerAngles.y, 180);
    }
    private void RotateY()
    {
        // input
        float vert = inputManager.MouseY;
        // variable
        float camY;
        // easy function to set variable
        void calY()
        {
            camY = (characterCam.localEulerAngles.x);
        }
        characterCam.Rotate(-(vert * mouseSensitivity), 0, 0); // good for cam vert movement
        // limiting the movement
        int topL = 0, topH = 350, bot = 50; // setting top and bottom limits
        calY(); // update/declare camY
        if (camY < topL && camY > topH) // check if to high
        {
            characterCam.Rotate((vert * mouseSensitivity), 0, 0);
        }
        calY(); // recalculate
        if (camY > bot && camY < topH) // check if to low
        {
            characterCam.Rotate((vert * mouseSensitivity), 0, 0);
        }
        // fix the upside down camera issue; MIGHT BE A ISSUE LATER
        FixUpsideDownCamera();
    }

}
