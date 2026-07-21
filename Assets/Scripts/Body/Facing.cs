using UnityEngine;

/// <summary>
/// Which way a judoka is turned. He is always squared up to his opponent, so
/// this works out the direction across the mat to them and hands that to the
/// body as his facing.
/// A technique can ask him to look off to one side of that by passing an angle,
/// which is how a throw turns his back in while still keeping him anchored to
/// where his opponent actually is.
/// </summary>
[RequireComponent(typeof(JudokaBody))]
public class Facing : MonoBehaviour
{
    //----------Variables----------\\

    // The body this hands its facing to
    private JudokaBody body;

    // The direction across the mat to the opponent, ignoring any angle asked for
    private float yaw;

    // Read by anything that needs to know which way he is squared up
    public float Yaw => yaw;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Find the body this hands its facing to
     */
    private void Awake()
    {
        body = GetComponent<JudokaBody>();
    }

    //----------Public Functions----------\\

    /* LOOK AT
     * 1 - Work out the direction from here to the target, flattened so looking
     *     up or down at them cannot tip him over
     * 2 - Turn that into a compass facing and remember it
     * 3 - Hand it to the body, offset by however far off square he was asked to
     *     look. That offset is how a throw turns his back in on his opponent
     */
    public void LookAt(Transform target, float lookAngle = 0)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        offset = offset.normalized;

        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        body.SetYaw(yaw + lookAngle);
    }

    /* YAW TO
     * 1 - Work out the same facing LookAt would, but hand it straight back
     *     instead of writing it to the body
     * 2 - If the target is right on top of him there is no direction to read, so
     *     keep whatever he was already facing
     * Used by a technique that has frozen his facing and wants to know where
     * square is, so it can ease him back to it before letting go
     */
    public float YawTo(Transform target)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 1e-6f) return yaw;

        offset = offset.normalized;
        return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }
}
