using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Tori : Judoka
{
    //------------------Variables------------------//
    //References to uke on opponent
    private Uke uke;
    [SerializeField] private FootManager feet;

    // Techniques
    [SerializeField] private DeAshiBarai deAshiBarai;
    [SerializeField] private HizaGuruma hizaGuruma; // concrete, so the wrong technique cannot be dropped in by mistake
    [SerializeField] private HaraiGoshi haraiGoshi;
    [SerializeField] private TomoeNage tomoeNage;
    [SerializeField] private float sweepDeadzone = 0.2f; // pull below this has no direction to read a sweep from
    private Technique active;      // the technique currently running, if any
    private bool sweepArmed = true; // the button must be released between attempts

    private Vector2 move;
    private Vector2 pull;
    private AttackSM attackState;
    private bool holdPosition; // a committed technique has planted tori - the polar movement is suspended

    //------------------Unity Event Functions------------------//
    /* START
     * 1 - Find uke
     * 2 - Build the context every technique acts through, and hand it over
     */
    protected override void Start()
    {
        base.Start();
        // 1
        uke = opponent.GetComponent<Uke>();

        // 2
        TechniqueContext ctx = new TechniqueContext
        {
            tori = this,
            toriFeet = feet,
            uke = uke,
            ukeFeet = opponent.GetComponent<FootManager>(),
        };
        deAshiBarai.Initialise(ctx);
        hizaGuruma?.Initialise(ctx); // guarded so an unwired field cannot abort the rest of Start
        haraiGoshi.Initialise(ctx);
        tomoeNage?.Initialise(ctx); // guarded so an unwired field cannot abort the rest of Start
        // 3 - a de ashi barai whose pull fights the sweep becomes a hiza guruma
        deAshiBarai.Redirect += OnRedirect;
    }

    /* UPDATE
     * 1 - Read the attack state: a sweep press begins De Ashi Barai on that
     *     side, and movement is surrendered while attacking
     * 2 - Releasing re-arms the button and breaks off a reach; a committed
     *     technique ignores the cancel and plays out
     * 3 - Tick the running technique and drop it once it resolves
     */
    protected override void Update()
    {
        base.Update();

        attackState = InputReader.Instance.AttackState;

        // 1, 2
        switch (attackState)
        {
            case AttackSM.rightSweep:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryBegin(ChooseSweep(1), FootId.Right);
                break;
            case AttackSM.leftSweep:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryBegin(ChooseSweep(-1), FootId.Left);
                break;
            case AttackSM.rightThrow:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryBegin(haraiGoshi, FootId.Right);
                break;
            case AttackSM.leftThrow:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryBegin(haraiGoshi, FootId.Left);
                break;
            case AttackSM.doubleThrow:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryTomoe(tomoeNage);
                break;
            default:
                move = InputReader.Instance.Move;
                pull = InputReader.Instance.Pull;
                sweepArmed = true;
                active?.Cancel();
                break;
        }

        // 3
        if (active != null)
        {
            active.Tick(Time.deltaTime);
            if (!active.IsRunning) active = null;
        }

        Move();
        Pull();
    }

    //------------------Private Functions------------------//

    /* CHOOSE SWEEP
     * The pull decides which sweep this is. Pulling the same way the sweeping
     * foot travels wheels uke over the blocked leg - hiza guruma. Pulling
     * against it takes the foot out from under him - de ashi barai. A pull too
     * small to read a direction from falls to the plain foot sweep, as does an
     * unwired hiza.
     * sweepSide: +1 the right foot sweeps, -1 the left
     */
    private Technique ChooseSweep(float sweepSide)
    {
        if (hizaGuruma == null) return deAshiBarai;
        if (Mathf.Abs(pull.x) < sweepDeadzone) return deAshiBarai;
        return Mathf.Sign(pull.x) == Mathf.Sign(sweepSide) ? hizaGuruma : deAshiBarai;
    }

    /* TRY BEGIN
     * One technique at a time, one attempt per press, and none once uke is
     * already beaten
     */
    private void TryBegin(Technique technique, FootId side)
    {
        if (!sweepArmed || active != null) return;
        if (uke.State != UkeState.Fighting) return;

        sweepArmed = false;
        technique.Begin(side);
        active = technique;
    }

    /* TRY TOMOE
     * Both throw buttons. The two presses never land on the same frame, so the
     * first one has usually started a hip throw already - break that off while
     * it is still reaching and let the sacrifice take over. A hip throw that has
     * already committed is left alone to play out.
     */
    private void TryTomoe(Technique technique)
    {
        if (active == technique) return;              // already going
        if (uke.State != UkeState.Fighting) return;

        if (active != null)
        {
            if (active.CurrentPhase != TechniquePhase.Reaching) return; // committed, leave it
            active.Cancel();
            active = null;
        }
        else if (!sweepArmed) return; // nothing running and the press has not been re-armed

        sweepArmed = false;
        technique.Begin(FootId.Right); // two footed - the side is irrelevant
        active = technique;
    }

    /* ON REDIRECT
     * De ashi barai bowed out because the pull fought the sweep. Hand the
     * same side to hiza guruma, which finds the shin and wheels uke over it.
     * Until that technique is wired, free the still-swept foot so the reach
     * simply misses.
     */
    private void OnRedirect(FootId side)
    {
        if (hizaGuruma != null)
        {
            hizaGuruma.Begin(side);
            active = hizaGuruma;
        }
        else
        {
            feet.Free();
            active = null;
        }
    }
    
    public void SetDistance(float newDistance)
    {
        movement.SetDistance(newDistance);
    }
    // Suspend the polar radius easing (a cancelled throw resets the distance and
    // must not drag tori around while the gap re-settles); re-enabled on the next throw
    public void SetRadiusRecovery(bool on) => movement.SetRecovery(on);
    // Plant tori: while held, the polar movement is suspended so his hip stays
    // put as the pivot a throw turns over (handed back when the throw ends)
    public void HoldPosition(bool on) => holdPosition = on;
    public void SetLean(Vector3 lean) => body.SetLean(lean);
    private void Move()
    {
        if (holdPosition) return;

        float x = move.x;

        movement.SetTarget(opponent, x);
        movement.Move();
    }

    private void Pull()
    {
        uke.Pull(pull);
    }

    public void SetLook(float deg)
    {
        SetLookAngle(deg);
    }
}