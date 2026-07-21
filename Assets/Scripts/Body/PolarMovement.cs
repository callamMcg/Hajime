using UnityEngine;

/// <summary>
/// How a judoka moves around his opponent.
/// He never simply walks about the mat - he circles, always keeping his opponent
/// at the centre. So his position is kept as an angle around them and a distance
/// from them, and moving left or right just winds that angle round.
/// The distance looks after itself: a technique can ask him to close in or back
/// off and he eases to it rather than snapping. That easing can be suspended,
/// which matters when a technique is called off - otherwise both judokas end up
/// chasing a distance that has just changed underneath them and skate across the
/// mat after each other.
/// </summary>
[RequireComponent(typeof(JudokaBody))]
public class PolarMovement : MonoBehaviour
{
    //----------Variables----------\\

    // The body this hands its position to
    private JudokaBody body;

    // How fast he circles his opponent, in degrees per second
    public float rotationSpeed;

    // How quickly he eases back to the distance he has been asked to hold
    [SerializeField] private float radiusRecoveryRate;

    // Whatever he is circling around, which is always his opponent
    private Transform pivot;

    // The angle he is at around them right now
    private float currentAngle;

    // The angle he is winding towards
    private float targetAngle;

    // How far from them he is right now
    private float radius;

    // How far from them he is trying to be
    private float restRadius;

    // While false the distance simply holds where it is, so a rest distance that
    // has just been changed cannot drag him across the mat
    private bool recoverRadius = true;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Find the body this hands its position to
     */
    private void Awake()
    {
        body = GetComponent<JudokaBody>();
    }

    //----------Public Functions----------\\

    /* SET TARGET
     * 1 - Take whatever he is circling around
     * 2 - Read where he currently stands relative to it, as an angle and a distance
     * 3 - Aim him at that same angle wound on by however much he is being asked
     *     to move, so pushing the stick sideways sends him around them
     */
    public void SetTarget(Transform centre, float degrees)
    {
        pivot = centre;

        Vector2 ground = body.Planar() - new Vector2(pivot.position.x, pivot.position.z);
        radius = ground.magnitude;
        currentAngle = Mathf.Atan2(ground.y, ground.x) * Mathf.Rad2Deg;

        targetAngle = currentAngle + (degrees * Mathf.Rad2Deg);
    }

    /* CAPTURE REST RADIUS
     * 1 - Take however far he currently is from something and make that the
     *     distance he tries to hold
     */
    public void CaptureRestRadius(Transform centre)
    {
        Vector2 ground = body.Planar() - new Vector2(centre.position.x, centre.position.z);
        restRadius = ground.magnitude;
    }

    /* MOVE
     * 1 - Do nothing until he has been given something to circle around
     * 2 - Wind his angle towards where he is heading, at his circling speed
     * 3 - Ease his distance back towards the one he is meant to hold, unless
     *     that easing has been suspended
     * 4 - Turn the angle and distance back into a spot on the mat and hand it
     *     to the body
     */
    public void Move()
    {
        // 1
        if (pivot == null) return;

        // 2
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // 3
        if (restRadius > 0f && recoverRadius)
            radius = restRadius + (radius - restRadius) * Mathf.Exp(-radiusRecoveryRate * Time.deltaTime);

        // 4
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 ground;
        ground.x = pivot.position.x + radius * Mathf.Cos(rad);
        ground.y = pivot.position.z + radius * Mathf.Sin(rad);
        body.SetPlanar(ground);
    }

    /* SET DISTANCE
     * 1 - Set how far from his opponent he should be trying to stand
     */
    public void SetDistance(float distance)
    {
        restRadius = distance;
    }

    /* SET RECOVERY
     * 1 - Turn the distance easing on or off
     * While it is off he simply holds whatever gap he already has. A technique
     * being called off uses this, so resetting the distance does not send him
     * skating across the mat after his opponent while the gap re-settles
     */
    public void SetRecovery(bool on)
    {
        recoverRadius = on;
    }
}
