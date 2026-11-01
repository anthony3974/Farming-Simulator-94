using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private bool debug;
    private float horizontal;
    private float vertical;
    private float mouseX;
    private float mouseY;
    private bool interact;
    private bool up;
    private bool right;
    private bool down;
    private bool left;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            debug = !debug;

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        up = false; right = false; down = false; left = false;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            up = true;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            down = true;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            right = true;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            left = true;

        interact = Input.GetKeyDown(KeyCode.E);
    }
    public float Horizontal { get => horizontal; set => horizontal = value; }
    public float Vertical { get => vertical; set => vertical = value; }
    public float MouseX { get => mouseX; set => mouseX = value; }
    public float MouseY { get => mouseY; set => mouseY = value; }
    public bool Interact { get => interact; set => interact = value; }
    public bool Up { get => up; set => up = value; }
    public bool Right { get => right; set => right = value; }
    public bool Down { get => down; set => down = value; }
    public bool Left { get => left; set => left = value; }
    public bool Debug { get => debug; set => debug = value; }
}
