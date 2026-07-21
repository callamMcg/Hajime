using System;
using UnityEngine;

/// <summary>
/// The base that every throw and sweep is built on.
/// It holds the two things they all share: how far through the attempt we are,
/// and the handles needed to reach tori, uke and both sets of feet.
/// A technique starts when the player presses the button, is driven a frame at
/// a time while it runs, and finishes exactly one of two ways - it scores or it
/// fails. Nothing else is allowed to end a run.
/// </summary>
public abstract class Technique : MonoBehaviour
{
    //----------Variables----------\\

    // How far through the attempt we are - Inactive means nothing is running
    public TechniquePhase CurrentPhase { get; protected set; } = TechniquePhase.Inactive;

    // True for as long as an attempt is under way
    public bool IsRunning => CurrentPhase != TechniquePhase.Inactive;

    // Announced when the technique puts uke down
    public event Action Scored;

    // Announced when the attempt comes to nothing
    public event Action Failed;

    // The handles on tori, uke and both sets of feet
    protected TechniqueContext ctx;

    //----------Public Functions----------\\

    /* INITIALISE
     * 1 - Take the shared handles on tori, uke and their feet
     * Tori hands these over once, as the match starts
     */
    public void Initialise(TechniqueContext context) => ctx = context;

    /* BEGIN
     * 1 - Called the moment the player presses the button
     * 2 - The technique sets itself up and starts reaching for uke
     * side is the foot tori is attacking with
     */
    public abstract void Begin(FootId side);

    /* TICK
     * 1 - Driven once a frame by tori while the attempt is running
     * 2 - Lets each technique advance whichever stage it is in
     */
    public abstract void Tick(float dt);

    /* CANCEL
     * 1 - The player has let go
     * 2 - A technique that has not committed yet packs up and hands everything
     *     back; one that has already committed ignores this and plays out
     */
    public abstract void Cancel();

    /* CHECK
     * Answers the one question every technique has to ask: is the opening there
     * right now? Each technique decides for itself what counts as an opening.
     */
    public abstract bool Check();

    //----------Protected Functions----------\\

    /* SCORE
     * 1 - Mark the attempt finished
     * 2 - Tell whoever is listening that uke went down
     */
    protected void Score()
    {
        CurrentPhase = TechniquePhase.Inactive;
        Scored?.Invoke();
    }

    /* FAIL
     * 1 - Mark the attempt finished
     * 2 - Tell whoever is listening that it came to nothing
     */
    protected void Fail()
    {
        CurrentPhase = TechniquePhase.Inactive;
        Failed?.Invoke();
    }
}
