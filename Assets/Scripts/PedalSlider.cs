/// <summary>
/// The on-screen pedal slider (touch pedals option "Slider", instead of the GAS and BRAKE buttons): push it up
/// for gas, down to brake (and, once stopped, to reverse). It rests 20% up the bar, so 80% of the travel is gas
/// and 20% brake (owner, 2026-11-06). How far from the rest point is how much: half way up drives at about half
/// the top speed. It springs back to the rest point (no gas, no brake) when let go. See <see cref="TouchSlider"/>.
/// </summary>
public class PedalSlider : TouchSlider
{
    // the default for a pedal slider that has no saved Rest Point (the base class default is the middle)
    public PedalSlider() { restPoint = 0.2f; }

    protected override bool Vertical => true;
    protected override void Send(InputManager input, float value) => input.SetThrottle(value);
}
