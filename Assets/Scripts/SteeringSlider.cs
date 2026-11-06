/// <summary>
/// The on-screen steering slider (touch steering option "Slider"): slide left or right, how far from the middle
/// is how hard you steer; it springs back to the middle (straight) when let go. See <see cref="TouchSlider"/>.
/// </summary>
public class SteeringSlider : TouchSlider
{
    protected override bool Vertical => false;
    protected override void Send(InputManager input, float value) => input.SetSteer(value);
}
