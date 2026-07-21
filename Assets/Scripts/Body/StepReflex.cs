using UnityEngine;

/// <summary>
/// The instinct to put a foot out when you are falling.
/// While a judoka is only slightly off balance he does nothing about it - his
/// balance spring is enough to hold him up. Once he is tipped past the point
/// where that is no longer true, this starts feeding back the speed his feet
/// want to travel at to get under his weight again.
/// It chases whichever way he is actually falling, and the further gone he is
/// the harder it tries, which is what makes a judoka stumble in the direction he
/// was pulled rather than simply toppling on the spot.
/// </summary>
[RequireComponent(typeof(Balance), typeof(Facing))]
public class StepReflex : MonoBehaviour
{
    //----------Variables----------\\

    // How far off balance he is, and which way
    private Balance balance;

    // Which way he is turned, so the tip can be read in world terms
    private Facing facing;

    // How fast his feet chase his weight once he has started stumbling
    [SerializeField] private float stepSpeed;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Find the balance this reads his tip from, and the facing that says
     *     which way he is turned
     */
    private void Awake()
    {
        balance = GetComponent<Balance>();
        facing  = GetComponent<Facing>();
    }

    //----------Public Functions----------\\

    /* GET STEP
     * Gives back the speed across the mat his feet want to travel at to get
     * back under his weight.
     * 1 - Do nothing at all while he is still comfortably on balance
     * 2 - Otherwise send his feet the way he is falling, faster the further gone
     *     he is
     */
    public Vector2 GetStep()
    {
        float rate = balance.Rate();

        // 1
        if (rate <= 0f) return Vector2.zero;

        // 2
        return Direction() * (rate * stepSpeed);
    }

    //----------Private Functions----------\\

    /* DIRECTION
     * 1 - Work out which way is forward and which is right for him, from the
     *     way he is turned
     * 2 - Read how far he is tipped as a fraction of his limit
     * 3 - Combine the two: pitching takes him forward or back, rolling takes him
     *     out to the side, and together they give the direction he is falling in
     *     world terms
     */
    private Vector2 Direction()
    {
        // 1
        float rad = facing.Yaw * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right = new Vector2(forward.y, -forward.x);

        // 2
        Vector3 norm = balance.NormalisedLean();

        // 3
        return (forward * norm.x - right * norm.z).normalized;
    }
}
