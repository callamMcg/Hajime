using System;
using UnityEngine;

public abstract class Technique : MonoBehaviour
{
    //------------------Phase------------------//
    // The spine every technique shares; what happens inside each phase is the
    // technique's own business
    public enum Phase
    {
        Inactive, // idle, ready to Begin
        Reaching, // moving into position - can still be cancelled
        Executing // committed - plays out to a Score or a Fail
    }

    //------------------Variables------------------//
    // Trackers
    public Phase CurrentPhase { get; protected set; } = Phase.Inactive;
    public bool IsRunning => CurrentPhase != Phase.Inactive;

    // Outcomes - Tori and the match flow listen to these
    public event Action Scored; // the throw landed: uke is falling
    public event Action Failed; // the attempt broke off

    // The seams this technique is allowed to act through
    protected TechniqueContext ctx;

    //------------------Public Functions------------------//
    // Handed over once by Tori on Start
    public void Initialise(TechniqueContext context) => ctx = context;

    // Input pressed: enter Reaching. side is the attacker's acting foot
    public abstract void Begin(FootId side);

    // Driven from Tori's Update while running
    public abstract void Tick(float dt);

    // Input released: only honoured while Reaching - Executing is a commitment
    public abstract void Cancel();

    //------------------Protected Functions------------------//
    // The two exits - concrete techniques call these, and nothing else ends a run
    protected void Score() { CurrentPhase = Phase.Inactive; Scored?.Invoke(); }
    protected void Fail() { CurrentPhase = Phase.Inactive; Failed?.Invoke(); }
}