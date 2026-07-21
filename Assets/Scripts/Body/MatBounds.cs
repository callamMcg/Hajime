using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches for a judoka stepping off the mat and ends the match when one does.
/// The mat and the floor around it sit on different layers, so rather than
/// measuring positions this simply looks down from each foot and asks which
/// surface it is standing on. That way the mat can be any shape and nothing here
/// has to know about it.
/// Only feet actually carrying weight are judged. A foot in mid air is allowed
/// to swing out over the edge as long as it comes back down inside, which is
/// what stops an ordinary step near the edge from ending the match.
/// </summary>
[DefaultExecutionOrder(100)]
public class MatBounds : MonoBehaviour
{
    //----------Variables----------\\

    // The feet of everyone in the match, one entry per judoka
    [SerializeField] private FootManager[] fighters;

    // The layer the surrounding floor sits on - anything standing here is out
    [SerializeField] private LayerMask outOfBounds;

    // How far above the foot to start looking down from
    [SerializeField] private float castHeight = 0.3f;

    // How far below the foot to keep looking
    [SerializeField] private float castDepth = 0.6f;

    // Set once someone has gone out, so the dying frames cannot fire it again
    private bool tripped;

    //----------Unity Functions----------\\

    /* LATE UPDATE
     * Runs after the feet have been placed for the frame, so it reads where they
     * actually settled.
     * 1 - Only judge live play. Time is frozen while paused, which is also when
     *     the replay runs, so this can never fire on a replayed frame
     * 2 - If someone has already gone out the match is on its way out, so leave
     *     it alone
     * 3 - Otherwise, if anyone has a foot down off the mat, the match is over
     */
    private void LateUpdate()
    {
        // 1
        if (Time.deltaTime <= 0f) return;

        // 2
        if (tripped) return;

        // 3
        foreach (FootManager fighter in fighters)
        {
            if (Off(fighter.LeftFoot, fighter.LeftGrounded) ||
                Off(fighter.RightFoot, fighter.RightGrounded))
            {
                tripped = true;
                OnOutOfBounds();
                return;
            }
        }
    }

    //----------Private Functions----------\\

    /* OFF
     * 1 - A foot in mid air is never out, however far past the edge it is
     * 2 - Otherwise look down from just above it and see whether the surface
     *     underneath belongs to the floor rather than the mat
     */
    private bool Off(Transform foot, bool grounded)
    {
        // 1
        if (!grounded) return false;

        // 2
        Vector3 origin = foot.position + Vector3.up * castHeight;
        return Physics.Raycast(origin, Vector3.down, castHeight + castDepth, outOfBounds);
    }

    /* ON OUT OF BOUNDS
     * The single place going out is dealt with.
     * 1 - Reload the match for now. When there is an "out of bounds" screen this
     *     is where it goes, with the reload happening after it
     */
    private void OnOutOfBounds()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
