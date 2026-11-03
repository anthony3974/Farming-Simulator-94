using System;
using UnityEngine;

public partial class GameCharacter
{
    public float topSpeed = 50;
    public float reverseSpeed = -10;
    public float accelerationMultiplier = 10;
    public float steeringMultiplier = 10;

    [Header("Front wheel steering (visual)")]
    [Tooltip("How far the front tires turn at full steering, in degrees.")]
    public float maxSteerAngle = 30f;
    [Tooltip("How fast the tires turn, in degrees per second.")]
    public float steerAnglePerSecond = 120f;

    // each front tire gets an un-scaled pivot above it, so it can turn without the squashed axle shearing it
    private readonly System.Collections.Generic.List<Transform> frontWheelPivots = new();
    private float steerAngle;

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
    /// <summary>
    /// Finds the two front tires (the FlTire / FrTire objects furthest forward on the truck) and puts each
    /// on a pivot at its centre. Looks at positions, not parent names, because the scene renames the axles.
    /// </summary>
    private void SetupFrontWheels()
    {
        var tires = new System.Collections.Generic.List<Transform>();
        foreach (var t in characterLocation.GetComponentsInChildren<Transform>(true))
            if (t.name == "FlTire" || t.name == "FrTire") tires.Add(t);

        if (tires.Count == 0)
        {
            Debug.LogWarning("No FlTire / FrTire found under the truck, front wheels will not steer.", this);
            return;
        }

        // forward is +Z in the truck's own space, so the front tires have the largest local Z
        float frontZ = float.MinValue;
        foreach (var t in tires) frontZ = Mathf.Max(frontZ, characterLocation.InverseTransformPoint(t.position).z);

        foreach (var tire in tires)
        {
            if (characterLocation.InverseTransformPoint(tire.position).z < frontZ - 0.5f) continue;

            Vector3 worldPos = tire.position;
            Quaternion worldRot = tire.rotation;
            Vector3 worldScale = tire.lossyScale;

            var pivot = new GameObject(tire.name + " Pivot").transform;
            pivot.SetParent(characterLocation, false);
            pivot.position = worldPos;
            pivot.rotation = characterLocation.rotation;

            tire.SetParent(pivot, false);
            tire.position = worldPos;
            tire.rotation = worldRot;
            var ps = pivot.lossyScale;
            tire.localScale = new Vector3(worldScale.x / ps.x, worldScale.y / ps.y, worldScale.z / ps.z);

            frontWheelPivots.Add(pivot);
        }
    }

    /// <summary>Turns the front tires toward the steering button (A / D, arrows or the on-screen buttons).</summary>
    private void SteerFrontWheels(Direction direction)
    {
        if (frontWheelPivots.Count == 0) return;

        float target = ((direction.Right ? 1f : 0f) - (direction.Left ? 1f : 0f)) * maxSteerAngle;
        steerAngle = Mathf.MoveTowards(steerAngle, target, steerAnglePerSecond * Time.deltaTime);

        // positive Y turns right, the same direction SteeringManager rotates the truck for Right
        var rot = Quaternion.Euler(0f, steerAngle, 0f);
        foreach (var pivot in frontWheelPivots) pivot.localRotation = rot;
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
                // in reverse the truck turns the other way, like a real car (steer right = back swings right)
                if (acceleration < 0) rotation = -rotation;
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
        SteerFrontWheels(direction);

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