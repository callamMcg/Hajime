using UnityEngine;

/// <summary>
/// The one place a judoka's transform is ever written.
/// Everything else - his balance, his gait, his facing, the technique currently
/// throwing him - describes what it wants in pieces: where he stands, how tall
/// he is, how far he is tipped, which way he faces. This holds all those pieces
/// and puts them together into a single position and rotation once per frame,
/// at the end, after everyone has had their say.
/// Because it is the only writer, nothing can fight anything else for the body,
/// and reading the pose here always gives the whole truth about where he is.
/// A technique that needs to turn him bodily - rolling him over a hip - can take
/// his rotation outright, and hand it back when it is done.
/// </summary>
public class JudokaBody : MonoBehaviour
{
    //----------Variables----------\\

    // Where he stands on the mat, x across and y along
    private Vector2 planar;

    // How tall he is standing right now
    private float height;

    // How far he is tipped - pitch in x, roll in z
    private Vector3 lean;

    // Which way he is turned
    private float yaw;

    // True while a technique has taken his rotation outright, such as a throw
    // rolling him over a hip
    private bool useWorldRotation;

    // The rotation that technique wants him held at
    private Quaternion worldRotation;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Take where he was placed in the scene as his starting position, so he
     *     does not snap anywhere on the first frame
     */
    private void Awake()
    {
        planar = new Vector2(transform.position.x, transform.position.z);
        height = transform.position.y;
    }

    /* LATE UPDATE
     * 1 - Write the body out at the very end of the frame, once every system has
     *     finished saying what it wants
     */
    private void LateUpdate() => Write();

    //----------Private Functions----------\\

    /* WRITE
     * 1 - Put his position together from where he stands and how tall he is
     * 2 - Put his rotation together from his tip and his facing - unless a
     *     technique has taken his rotation outright, in which case use that
     */
    private void Write()
    {
        // 1
        transform.position = new Vector3(planar.x, height, planar.y);

        // 2
        transform.rotation = useWorldRotation ? worldRotation : Quaternion.Euler(lean.x, yaw, lean.z);
    }

    //----------Public Functions----------\\

    /* SET PLANAR
     * 1 - Put him at a spot on the mat
     */
    public void SetPlanar(Vector2 position)
    {
        planar = position;
    }

    /* MOVE PLANAR
     * 1 - Shift him across the mat by an amount, rather than to a spot
     */
    public void MovePlanar(Vector2 delta)
    {
        planar += delta;
    }

    /* SET HEIGHT
     * 1 - Set how tall he is standing
     */
    public void SetHeight(float y)
    {
        height = y;
    }

    /* SET LEAN
     * 1 - Set how far he is tipped, as pitch and roll
     */
    public void SetLean(Vector3 amount)
    {
        lean = amount;
    }

    /* SET YAW
     * 1 - Set which way he is turned
     */
    public void SetYaw(float degrees)
    {
        yaw = degrees;
    }

    /* SET WORLD ROTATION
     * 1 - Let a technique take his rotation outright, ignoring his tip and
     *     facing until it hands control back
     */
    public void SetWorldRotation(Quaternion r)
    {
        useWorldRotation = true;
        worldRotation = r;
    }

    /* CLEAR WORLD ROTATION
     * 1 - Give his rotation back to his tip and facing
     */
    public void ClearWorldRotation()
    {
        useWorldRotation = false;
    }

    /* APPLY POSE
     * 1 - Adopt a whole pose at once, which is how the replay puts him where he
     *     was on a recorded frame
     * 2 - Write it through immediately rather than waiting for the end of the
     *     frame. The replay places limb targets in world space straight after
     *     this, and a target parented to him needs him already standing in the
     *     right place, or it gets dragged when he finally moves
     * The end of frame write then repeats the same values, harmlessly
     */
    public void ApplyPose(BodyPose p)
    {
        planar = p.planar;
        height = p.height;
        lean = p.lean;
        yaw = p.yaw;
        useWorldRotation = p.useWorldRotation;
        worldRotation = p.worldRotation;
        Write();
    }

    //----------Getters----------\\

    /* GET POSE
     * 1 - Hand back everything about where he is in one piece, for the replay to
     *     record or another system to read
     */
    public BodyPose GetPose() => new BodyPose
    {
        planar = planar,
        height = height,
        lean = lean,
        yaw = yaw,
        useWorldRotation = useWorldRotation,
        worldRotation = worldRotation,
    };

    /* PLANAR
     * 1 - Give back where he stands on the mat
     */
    public Vector2 Planar()
    {
        return planar;
    }

    /* WORLD POSITION
     * 1 - Give back where he stands with his height folded back in, as a full
     *     position in the world
     */
    public Vector3 WorldPosition()
    {
        return new Vector3(planar.x, height, planar.y);
    }
}
