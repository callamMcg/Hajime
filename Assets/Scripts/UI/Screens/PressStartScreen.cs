using UnityEngine;

/// <summary>
/// The title screen the game opens on, waiting for the player to press start.
/// It hands over to the main menu rather than opening it on top, so there is
/// nothing left behind it - once you are in the menu there is no going back to
/// the title.
/// </summary>
public class PressStartScreen : MenuScreen
{
    //----------Variables----------\\

    // The menu this hands over to
    [SerializeField] private MenuScreen mainMenu;

    //----------Button Functions----------\\

    /* ON START PRESSED
     * 1 - Replace this screen with the main menu, leaving nothing behind it
     */
    public void OnStartPressed() => Manager.SwitchTo(mainMenu);

    //----------Override Functions----------\\

    /* ON BACK
     * There is nothing behind the title screen to go back to, so backing out
     * here deliberately does nothing
     */
    public override void OnBack() { }
}
