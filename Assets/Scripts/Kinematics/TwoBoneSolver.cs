using UnityEngine;

// Solve after JudokaBody (0) and FootManager (50) have finished the frame, so
// the bones pose against this frame's final targets rather than last frame's
[DefaultExecutionOrder(60)]
public class TwoBoneIK : MonoBehaviour
{

    //------------------Variables------------------//
    //Const
    private const float e = 1e-4f;
    //References
    [SerializeField] private Transform rootJoint;
    [SerializeField] private Transform middleJoint;
    [SerializeField] private Transform endEffector;
    [SerializeField] private Transform endTarget;
    [SerializeField] private Transform planePole;

    //------------------Unity Functions------------------//
    private void LateUpdate()
    {
        Solve(rootJoint, middleJoint, endEffector, endTarget.position, planePole.position, endTarget.rotation);
    }

    //------------------Private Functions------------------//
    /* SOLVE
     * 1 - Calculate the target position within the limits of the limb
     *  a - Get positions
     *  b - Calculate lengths and limits
     *  c - Use direction and distance to calculate target within limits
     * 2 - Calculate the joint placement
     *  a - Calculate adj and hyp of triangle
     *  b - Calculate position of  plane
     *  c - Calculate the target joints  position on the plane using the triangle
     * 3 - Position the joints
     *  a - Point the root towards the joint target
     *  b - Point the joint towards the end target
     *  c - Angle the end effector to match the rotation
     */
    private static void Solve(Transform upper, Transform mid, Transform end, Vector3 target, Vector3 pole, Quaternion endRotation)
    {
        //1
        //a
        Vector3 rootPos = upper.position;
        Vector3 midPos = mid.position;
        Vector3 endPos = end.position;

        //b
        float upperLen = Vector3.Distance(rootPos, midPos);
        float lowerLen = Vector3.Distance(midPos, endPos);
        float maxReach = upperLen + lowerLen - e;
        float minReach = Mathf.Abs(upperLen - lowerLen) + e;

        //c
        Vector3 toTarget = target - rootPos;
        float dist = Mathf.Clamp(toTarget.magnitude, minReach, maxReach);
        Vector3 dir = toTarget.sqrMagnitude > e ? toTarget.normalized : upper.forward;
        Vector3 effectiveTarget = rootPos + dir * dist;

        //2
        //a
        float x = (dist * dist + upperLen * upperLen - lowerLen * lowerLen) / (2f * dist);
        float h = Mathf.Sqrt(Mathf.Max(0f, upperLen * upperLen - x * x));

        //b
        Vector3 poleDir = pole - rootPos;
        Vector3 bend = poleDir - dir * Vector3.Dot(poleDir, dir);
        if (bend.sqrMagnitude < e)
        {
            bend = Vector3.Cross(dir, Vector3.up);
            if (bend.sqrMagnitude < e) bend = Vector3.Cross(dir, Vector3.right);
        }
        bend.Normalize();

        //c
        Vector3 knee = rootPos + dir * x + bend * h;

        //3
        //a
        Vector3 currentUpperDir = mid.position - upper.position;
        upper.rotation = Quaternion.FromToRotation(currentUpperDir, knee - rootPos) * upper.rotation;

        //b
        Vector3 currentLowerDir = end.position - mid.position;
        mid.rotation = Quaternion.FromToRotation(currentLowerDir, effectiveTarget - mid.position) * mid.rotation;

        //c
        end.rotation = endRotation;
    }
}