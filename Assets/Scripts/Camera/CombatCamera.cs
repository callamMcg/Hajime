using UnityEngine;

public class CombatCamera : MonoBehaviour
{
    //------------------Variables------------------//
    //References
    [SerializeField] private Transform player;
    [SerializeField] private Transform opponent;
    private Transform lookingAt;
    private Vector3 lookTarget;
    //Positioning
    [SerializeField] private Vector3 leftShoulderOffset = new Vector3(-0.5f, 0.3f, -0.4f);
    [SerializeField] private Vector3 rightShoulderOffset = new Vector3(0.5f, 0.3f, -0.4f);

    //Stats
    [SerializeField] private float lookHeight = 1.5f;
    [SerializeField] private float followSmoothing = 5f;
    [SerializeField] private float shoulderSwapSmoothing = 3f;

    //Trackers
    private Vector3 targetShoulderOffset;
    private Vector3 currentShoulderOffset;
    private float movementThreshold = 0.1f;

    //------------------Unity Functions------------------//
    private void Awake()
    {
        currentShoulderOffset = rightShoulderOffset;
        targetShoulderOffset = rightShoulderOffset;
        lookingAt = opponent;
    }


    private void LateUpdate()
    {
        if (player == null || opponent == null) return;

        Vector2 move = InputReader.Instance.Move;
        Vector2 pull = InputReader.Instance.Pull;

        if (
            InputReader.Instance.AttackState == AttackSM.rightThrow ||
            InputReader.Instance.AttackState == AttackSM.leftSweep
            )
        {
            return;

            UpdateTargetShoulder(-Vector2.right);
            ChangeTarget(player);
        }
        else if (
            InputReader.Instance.AttackState == AttackSM.leftThrow ||
            InputReader.Instance.AttackState == AttackSM.rightSweep
            )
        {
            return;
            UpdateTargetShoulder(Vector2.right);
            ChangeTarget(player);
        }
        else
        {
            if (InputReader.Instance.AttackState == AttackSM.standard)
                ChangeTarget(opponent);
            UpdateTargetShoulder(move);
            UpdateTargetShoulder(pull);
        }
        currentShoulderOffset = Vector3.Lerp(currentShoulderOffset, targetShoulderOffset, Time.deltaTime * shoulderSwapSmoothing);

        Vector3 targetPos = player.position + player.TransformDirection(currentShoulderOffset);
        targetPos.y = currentShoulderOffset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothing);

        lookTarget = Vector3.Lerp(lookTarget, lookingAt.position + Vector3.up * lookHeight, 5 * Time.deltaTime);
        transform.LookAt(lookTarget);
    }

    //------------------Custom Functions------------------//

    //// Updates the target shoulder offset based on input direction
    private void UpdateTargetShoulder(Vector2 move)
    {
        if (Mathf.Abs(move.x) > movementThreshold)
        {
            if (move.x > 0)
                targetShoulderOffset = rightShoulderOffset;
            else
                targetShoulderOffset = leftShoulderOffset;
        }
    }

    //Allows runtime adjustment of shoulder offsets for tuning
    public void SetShoulderOffsets(Vector3 left, Vector3 right)
    {
        leftShoulderOffset = left;
        rightShoulderOffset = right;
    }

    // Force camera to a specific shoulder immediately
    public void SetShoulder(bool isRight)
    {
        targetShoulderOffset = isRight ? rightShoulderOffset : leftShoulderOffset;
        currentShoulderOffset = targetShoulderOffset;
    }

    private void ChangeTarget(Transform newTarget)
    {
        lookingAt = newTarget;
    }
}
