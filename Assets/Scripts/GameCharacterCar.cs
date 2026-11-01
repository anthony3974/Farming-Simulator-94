using System;
using UnityEngine;

public partial class GameCharacter
{
    public float topSpeed = 50;
    public float reverseSpeed = -10;
    public float accelerationMultiplier = 10;
    public float steeringMultiplier = 10;

    private float acceleration = 0;
    private const float accAmt = 0.02f;
    private const float speedConverter = 0.06f;
    private const float steeringPower = 10;

    /// <summary>
    /// Up, Right, Down, Left
    /// </summary>
    private readonly Direction direction = new(false, false, false, false);

    /// <summary>
    /// Debug Only
    /// </summary>
    /// <param name="mess">Add a message</param>
#pragma warning disable IDE0051 // Remove unused private members
    private void ShowKeys(string mess = "")
#pragma warning restore IDE0051 // Remove unused private members
    {
        if (mess == "")
            Debug.Log(direction.ToString());
        else
            Debug.Log(direction.ToString() + "MESSAGE: " + mess);
    }
    private void Accelerate()
    {
        acceleration += accAmt * accelerationMultiplier * Time.deltaTime;
    }
    private void Decelerate()
    {
        acceleration -= accAmt * accelerationMultiplier * Time.deltaTime;
    }
    private void KeyManager(Direction direction)
    {
        direction.Reset();

        if (inputManager.Up)
        {
            direction.Up = true;
        }
        if (inputManager.Down)
        {
            direction.Down = true;
        }
        if (inputManager.Right)
        {
            direction.Right = true;
        }
        if (inputManager.Left)
        {
            direction.Left = true;
        }
    }
    private void DecelerationManager(Direction direction)
    {
        if (direction.Up == false && acceleration > 0)
        {
            Decelerate();
            if (acceleration < 0) acceleration = 0;
        }
        if (direction.Down == false && acceleration < 0)
            Accelerate();
    }
    private void AccelerationManager(Direction direction)
    {
        if (direction.Up && acceleration < topSpeed * speedConverter)
            Accelerate();
        if (direction.Down && acceleration > reverseSpeed * speedConverter)
        {
            Decelerate();
            Decelerate();
        }
    }

    private void SteeringManager(Direction direction)
    {
        float minAccR = -0.085f;
        float minAccF = 0.085f;
        if (acceleration == 0 || (acceleration >= minAccR && acceleration <= minAccF))
        {
            return;
        }

        float rotationAmount = Time.deltaTime * (steeringPower * steeringMultiplier);

        if (direction.Right || direction.Left)
        {
            if (acceleration > minAccF || acceleration < minAccR)
            {
                float rotation = direction.Right ? rotationAmount : -rotationAmount;
                characterLocation.transform.Rotate(0, rotation, 0);
            }
        }
    }

    private void DriveManager(Direction direction)
    {
        // slows the vehicle down if no input
        DecelerationManager(direction);
        // handles acceleration of the vehicle
        AccelerationManager(direction);
        // handles the steering of the vehicle
        SteeringManager(direction);

        // applies the movement
        Vector3 move = acceleration * speed * characterLocation.transform.forward;

        characterController.Move(Time.deltaTime * move);
    }
    public void Drive()
    {

        // takes input and saves it to the class
        KeyManager(direction);
        // controls all vehicle movement
        DriveManager(direction);

    }
}

public class Direction
{
    private bool up;
    private bool down;
    private bool left;
    private bool right;

    public Direction(bool up, bool down, bool left, bool right)
    {
        Up = up;
        Down = down;
        Left = left;
        Right = right;
    }

    public void Reset()
    {
        Up = false;
        Down = false;
        Left = false;
        Right = false;
    }
    public override string ToString()
    {
        return "Up: " + Up + " Down: " + Down + " Left: " + Left + " Right: " + Right;
    }

    public bool Up { get => up; set => up = value; }
    public bool Down { get => down; set => down = value; }
    public bool Left { get => left; set => left = value; }
    public bool Right { get => right; set => right = value; }
}