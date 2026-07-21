using UnityEngine;

/// <summary>
/// Holds a judoka's grip on his opponent and switches which hand is dominant.
/// He grips two places: the lapel and the sleeve. The lapel is the controlling
/// hand - the one doing the steering - so which arm holds it is what decides
/// which side he is working from.
/// Each hand has its own pair of grips, because the two of them cannot hold the
/// same piece of cloth: reaching across for the lapel puts a hand somewhere quite
/// different from where the other hand would take it, and the same goes for the
/// sleeves on either arm.
/// Pulling right makes the right hand dominant, so it takes its lapel grip and
/// the left drops to its sleeve. Pulling left swaps them round. A pull too weak
/// to read a side from changes nothing, so the grip holds rather than flickering
/// about the middle.
/// The hands cross over rather than jumping: one blend runs between the grips and
/// both hands ride it, so they trade places in a single smooth motion.
/// This only happens in ordinary play. The moment a sweep or a throw starts the
/// grip is locked, so a technique keeps whatever hold it began with and cannot
/// have the hands swapped out from under it halfway through.
/// </summary>
[DefaultExecutionOrder(55)]
public class ArmManager : MonoBehaviour
{
    //----------Variables----------\\

    // The IK target for this judoka's left hand
    [SerializeField] private Transform leftHand;

    // The IK target for this judoka's right hand
    [SerializeField] private Transform rightHand;

    // Where the left hand takes the opponent's lapel when it is the dominant one
    [SerializeField] private Transform leftLapel;

    // Where the left hand takes the sleeve when the right hand is dominant
    [SerializeField] private Transform leftSleeve;

    // Where the right hand takes the opponent's lapel when it is the dominant one
    [SerializeField] private Transform rightLapel;

    // Where the right hand takes the sleeve when the left hand is dominant
    [SerializeField] private Transform rightSleeve;

    // Seconds the hands take to trade places
    [SerializeField] private float swapDuration = 0.35f;

    // How hard the pull has to be before it counts as having a side at all
    [SerializeField] private float deadzone = 0.2f;

    // Which way round the grip currently is, from 0 with the left hand on the
    // lapel to 1 with the right hand on it
    private float blend = 1f;

    // True while a technique is running, which stops the grip swapping
    private bool locked;

    //----------Public Functions----------\\

    /* SET GRIP LOCKED
     * 1 - Lock the grip while a sweep or throw is under way, so it keeps the
     *     hold it started with, and unlock it again for ordinary play
     * The hands still follow their grips while locked - it is only the swapping
     * between them that stops
     */
    public void SetGripLocked(bool on) => locked = on;

    //----------Event Loop----------\\

    /* LATE UPDATE
     * Runs after the bodies have been placed for the frame and before the arms
     * are solved, so the hands are put on grips that are already where they
     * finally are this frame.
     * 1 - Do nothing while time is frozen. The replay runs under that freeze and
     *     owns the hands then, so this must not fight it for them
     * 2 - Do nothing until everything it needs has been wired up
     * 3 - Unless a technique has locked the grip, read which way the pull is
     *     going and pick the hold that goes with it, keeping what we have if the
     *     pull is too weak to read a side from
     * 4 - Cross the hands over towards that, taking the set time to do it
     * 5 - Place both hands, easing the crossover so they start and finish gently
     *     rather than sliding at a constant rate
     */
    private void LateUpdate()
    {
        // 1
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 2
        if (!Wired()) return;

        // 3
        if (!locked)
        {
            float target = blend;
            float pull = InputReader.Instance != null ? InputReader.Instance.Pull.x : 0f;
            if (pull > deadzone) target = 1f;
            else if (pull < -deadzone) target = 0f;

            // 4
            blend = Mathf.MoveTowards(blend, target, dt / Mathf.Max(swapDuration, 0.01f));
        }

        // 5
        float ease = blend * blend * (3f - 2f * blend);
        Place(rightHand, rightSleeve, rightLapel, ease);
        Place(leftHand, leftLapel, leftSleeve, ease);
    }

    //----------Private Functions----------\\

    /* WIRED
     * 1 - Report whether both hands and all four grips have been set up, since
     *     there is nothing sensible to do with only some of them
     */
    private bool Wired()
    {
        return leftHand != null && rightHand != null
            && leftLapel != null && leftSleeve != null
            && rightLapel != null && rightSleeve != null;
    }

    /* PLACE
     * 1 - Put one hand somewhere between its two grips, according to how far
     *     through the crossover we are
     * 2 - Turn it to match, so the hand rolls over as it moves rather than
     *     arriving at the new grip still facing the old way
     * Each hand is handed its own pair in the opposite order to the other, which
     * is what sends them in opposite directions so they trade places
     */
    private void Place(Transform hand, Transform from, Transform to, float t)
    {
        hand.position = Vector3.Lerp(from.position, to.position, t);
        hand.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
    }

    //----------Getters----------\\

    // The IK target for the left hand, so the judoka can adopt it as his own
    public Transform LeftHand => leftHand;

    // The IK target for the right hand, for the same reason
    public Transform RightHand => rightHand;

    // True while the right hand is the dominant one, holding the lapel
    public bool RightIsDominant => blend >= 0.5f;
}
