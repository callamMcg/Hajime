using System;
using UnityEngine;

public abstract class Technique : MonoBehaviour
{
    //------------------Variables------------------//
    // Trackers
    public TechniquePhase CurrentPhase { get; protected set; } = TechniquePhase.Inactive;
    public bool IsRunning => CurrentPhase != TechniquePhase.Inactive;

    // Outcomes - Tori and uke listen to these
    public event Action Scored; 
    public event Action Failed; 

    //Contains the tori, uke and their foot mangers
    protected TechniqueContext ctx;

    //------------------Public Functions------------------//
    // Handed over once by Tori on Start
    public void Initialise(TechniqueContext context) => ctx = context;

    // Input pressed: enter Reaching. side is the attacker's acting foot
    public abstract void Begin(FootId side);

    // Driven from Tori's Update while running
    public abstract void Tick(float dt);

    // Return to normal
    public abstract void Cancel();

    //Checks if the uke can be tripped by this technique
    public abstract bool Check();

    //------------------Protected Functions------------------//
    // The two exits - concrete techniques call these, and nothing else ends a run
    protected void Score() { CurrentPhase = TechniquePhase.Inactive; Scored?.Invoke(); }
    protected void Fail() { CurrentPhase = TechniquePhase.Inactive; Failed?.Invoke(); }
}