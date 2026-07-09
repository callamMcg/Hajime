using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches every judoka's grounded feet and resets the match the moment one
/// plants off the mat. The mat and the surrounding floor sit on different
/// layers; a short downward ray from each grounded foot reads which surface it
/// is standing on, so leaving the area is detected by layer, not by position.
/// Only grounded feet are tested - an airborne swing can arc over the edge and
/// still land in bounds, and that must not trip a reset.
/// </summary>

// After FootManager (order 50) has planted this frame's feet, so the ray reads
// their settled positions
[DefaultExecutionOrder(100)]
public class MatBounds : MonoBehaviour
{
    //------------------Variables------------------//
    // References
    [SerializeField] private FootManager[] fighters;    // one per judoka

    // Layers
    [SerializeField] private LayerMask outOfBounds;     // the floor surrounding the mat

    // Cast
    [SerializeField] private float castHeight = 0.3f;   // start the ray a little above the foot
    [SerializeField] private float castDepth = 0.6f;    // how far below the foot to look

    // Latch, so the reset fires once and the dying frames are ignored
    private bool tripped;

    //------------------Unity Functions------------------//
    /* LATE UPDATE
     * 1 - Only judge live play. deltaTime is zero while paused, which is also
     *     when the replay runs, so this never fires on a replayed frame
     * 2 - Already tripped, the scene is on its way out - do nothing
     * 3 - Any grounded foot off the mat ends the match
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

    //------------------Private Functions------------------//
    /* OFF
     * A grounded foot is out when the surface beneath it is on the
     * out-of-bounds layer. Airborne feet are never out.
     */
    private bool Off(Transform foot, bool grounded)
    {
        if (!grounded) return false;
        Vector3 origin = foot.position + Vector3.up * castHeight;
        return Physics.Raycast(origin, Vector3.down, castHeight + castDepth, outOfBounds);
    }

    /* ON OUT OF BOUNDS
     * The single exit point. Reload the active scene for now; later this is
     * where the "out of bounds" screen goes, and the reload happens after it.
     */
    private void OnOutOfBounds()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}