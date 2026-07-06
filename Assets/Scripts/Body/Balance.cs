using UnityEngine;

[RequireComponent(typeof(JudokaBody))]
public class Balance : MonoBehaviour
{
    //------------------Variables------------------//
    // Components
    private JudokaBody body;

    // Trackers
    private Vector3 lean; // current lean (pitch, yaw, roll)
    private Vector3 leanV; // lean velocity
    private Vector2 pull; // this frame's applied pull input
    private Vector3 limits;   // max lean

    // Stats
    [SerializeField] private float pullStrength;
    [SerializeField] private float recoveryStiffness;
    [SerializeField] private float recoveryDamping; // Over 2!
    [SerializeField] private float movementResistance;
    [SerializeField] private float stepThreshold; // 0-1
    [SerializeField] private float stepRecovery;

    //------------------Unity Functions------------------//
    private void Awake() { body = GetComponent<JudokaBody>(); }

    //------------------Public Functions------------------//

    // Store this frame's pull as a lean input
    public void SetBalance(Vector2 amount) => pull = amount;

    // Decrease lean by the movement of the opponent
    public void AddMovementPull(Vector2 amount) { if (movementResistance == 0) return; else  pull -= amount / movementResistance;}

    public void SetLimits(Vector3 newLimits) {limits = newLimits;}

    /* STEP
     * 1 - Sum the torques acting on the lean
     * 2 - Integrate velocity, then lean
     * 3 - Clamp at the limits, killing any speed that pushed past them
     * 4 - Hand the lean (pitch x, roll z) to the body
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

    //------------------Getters------------------//
    // True once side lean has been driven to its limit
    public bool IsPastLateralLimit(float limit = 0 ) { return Mathf.Abs(lean.z) >= limit;}

    // Lean as a fraction of the limit on each axis (y unused)
    public Vector3 NormalisedLean()
    {
        return new Vector3(
            limits.x > 0f ? lean.x / limits.x : 0f,
            0f,
            limits.z > 0f ? lean.z / limits.z : 0f);
    }
    public Vector3 GetLean() { return lean; }
    // How far into the "needs a step" zone the lean is, 0-1
    public float Rate() { return Mathf.InverseLerp(stepThreshold, 1f, NormalisedLean().magnitude);}

    // Adopt a lean as the current spring state, e.g. after a technique drove the body directly
    public void AdoptLean(Vector3 newLean) { lean = newLean; leanV = Vector3.zero; }
}
