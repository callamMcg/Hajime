using UnityEngine;

public class PressStartScreen : MenuScreen
{
    //----------Variables----------\\
    //Menu Referenece
    [SerializeField] private MenuScreen mainMenu;

    //----------Button Functions----------\\
    public void OnStartPressed() => Manager.SwitchTo(mainMenu);
    
    //----------Override Functions----------\\
    //Override OnBack with nothing
    public override void OnBack() { } 
}