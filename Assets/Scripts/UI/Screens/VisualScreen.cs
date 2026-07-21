/// <summary>
/// A screen with nothing behind it but its own artwork - the controls page and
/// anything else that only has to be looked at.
/// It needs no code of its own: everything it does, fading in and out and backing
/// out to whatever opened it, is already handled by the screen it inherits from.
/// It exists so those pages can be a screen at all, and be pushed and popped like
/// any other.
/// </summary>
public class VisualScreen : MenuScreen
{
}
