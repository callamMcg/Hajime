using UnityEngine;

/// <summary>
/// A judoka's balance, modelled as a spring he is always fighting to stand up in.
/// Pulling on him tips him over; his own stiffness hauls him back upright, and
/// damping stops that turning into a wobble. The further he is tipped the harder
/// he tries to step his feet back underneath himself, which fights the tip too.
/// He can only be leaned so far before he hits his limit and simply stops going
/// any further, and it is that limit a throw is trying to drive him to.
/// The lean this works out is handed to the body as pitch and roll.
/// </summary>
[RequireComponent(typeof(JudokaBody))]
public class Balance : MonoBehaviour
{
    //----------Variables----------\\

    // The body this hands its lean to
    private JudokaBody body;

    // How far he is tipped right now - pitch in x, roll in z, y unused
    private Vector3 lean;

    // How fast that lean is changing
    private Vector3 leanV;

    // The pull being applied to him this frame
    private Vector2 pull;

    // The furthest he can be tipped on each axis before he is simply stuck there
    private Vector3 limits;

    // How hard a pull tips him
    [SerializeField] private float pullStrength;

    // How hard he hauls himself back upright
    [SerializeField] private float recoveryStiffness;

    // How much that recovery is damped, which stops him wobbling. Keep it over 2
    [SerializeField] private float recoveryDamping;

    // How much an opponent's movement takes out of the pull on him
    [SerializeField] private float movementResistance;

    // How far tipped, as a fraction of his limit, before he starts wanting to step
    [SerializeField] private float stepThreshold;

    // How hard stepping his feet back under himself fights the tip
    [SerializeField] private float stepRecovery;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Find the body this hands its lean to
     */
    private void Awake()
    {
        body = GetComponent<JudokaBody>();
    }

    //----------Public Functions----------\\

    /* SET BALANCE
     * 1 - Store the pull being applied to him this frame, to be worked in on
     *     the next step
     */
    public void SetBalance(Vector2 amount) => pull = amount;

    /* ADD MOVEMENT PULL
     * 1 - Do nothing if he has no resistance set, as there is nothing to divide by
     * 2 - Otherwise take some of the pull back off him, so an opponent who is
     *     moving with him is not hauling on him as hard
     */
    public void AddMovementPull(Vector2 amount)
    {
        if (movementResistance == 0) return;
        else pull -= amount / movementResistance;
    }

    /* SET LIMITS
     * 1 - Take the furthest he is allowed to be tipped on each axis
     */
    public void SetLimits(Vector3 newLimits)
    {
        limits = newLimits;
    }

    /* STEP
     * 1 - Add up everything acting on his lean this frame: the pull tipping him
     *     over, his own stiffness hauling him back up, damping bleeding off the
     *     wobble, and his feet trying to step back underneath him
     * 2 - Let that build his lean speed, and let the speed move the lean
     * 3 - Stop him at his limit, killing the speed that pushed him into it so he
     *     does not keep straining against it
     * 4 - Hand the finished lean to the body
     */
    public void Step()
    {
        // 1
        Vector3 torque = new Vector3(pull.y, 0f, pull.x) * pullStrength;
        torque -= lean  * recoveryStiffness;
        torque -= leanV * recoveryDamping;
        torque -= NormalisedLean().normalized * (Rate() * stepRecovery);

        // 2
        leanV += torque * Time.deltaTime;
        lean  += leanV  * Time.deltaTime;

        // 3
        if (Mathf.Abs(lean.x) > limits.x) { lean.x = Mathf.Sign(lean.x) * limits.x; leanV.x = 0f; }
        if (Mathf.Abs(lean.z) > limits.z) { lean.z = Mathf.Sign(lean.z) * limits.z; leanV.z = 0f; }

        // 4
        body.SetLean(lean);
    }

    /* ADOPT LEAN
     * 1 - Take a lean as the spring's own state and settle it there, with no
     *     speed left over
     * Used when a technique has been posing the body directly and is handing it
     * back, so the spring carries on from where the body actually is
     */
    public void AdoptLean(Vector3 newLean)
    {
        lean = newLean;
        leanV = Vector3.zero;
    }

    //----------Getters----------\\

    /* IS PAST LATERAL LIMIT
     * 1 - Report whether he has been tipped sideways as far as the given limit
     */
    public bool IsPastLateralLimit(float limit = 0)
    {
        return Mathf.Abs(lean.z) >= limit;
    }

    /* NORMALISED LEAN
     * 1 - Give the lean as a fraction of his limit on each axis, so 1 means he
     *     is tipped as far as he can go
     * The y axis is unused, as a judoka is not leaned about it
     */
    public Vector3 NormalisedLean()
    {
        return new Vector3(
            limits.x > 0f ? lean.x / limits.x : 0f,
            0f,
            limits.z > 0f ? lean.z / limits.z : 0f);
    }

    /* GET LEAN
     * 1 - Give back how far he is tipped right now
     */
    public Vector3 GetLean()
    {
        return lean;
    }

    /* RATE
     * 1 - Report how far into the "he needs to step" zone his lean has got, from
     *     0 at the threshold to 1 at his limit
     */
    public float Rate()
    {
        return Mathf.InverseLerp(stepThreshold, 1f, NormalisedLean().magnitude);
    }
}
