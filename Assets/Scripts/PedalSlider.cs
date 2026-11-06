/// <summary>
/// The on-screen pedal slider (touch pedals option "Slider", instead of the GAS and BRAKE buttons): push it up
/// for gas, down to brake (and, once stopped, to reverse). How far from the middle is how much: half up drives
/// at about half the top speed. It springs back to the middle (no gas, no brake) when let go. See <see cref="TouchSlider"/>.
/// </summary>
public class PedalSlider : TouchSlider
{
    protected override bool Vertical => true;
    protected override void Send(InputManager input, float value) => input.SetThrottle(value);
}
