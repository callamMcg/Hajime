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
    [SerializeField] private Technique hizaGuruma; // typed as the base so it can be wired before the concrete class exists
    private Technique active;      // the technique currently running, if any
    private bool sweepArmed = true; // the button must be released between attempts

    private Vector2 move;
    private Vector2 pull;
    private AttackSM attackState;

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
        if (hizaGuruma != null) hizaGuruma.Initialise(ctx);

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
                TryBegin(deAshiBarai, FootId.Right);
                break;
            case AttackSM.leftSweep:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                TryBegin(deAshiBarai, FootId.Left);
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

    private void Move()
    {
        float x = move.x;

        movement.SetTarget(opponent, x);
        movement.Move();
    }

    private void Pull()
    {
        uke.Pull(pull);
    }
}