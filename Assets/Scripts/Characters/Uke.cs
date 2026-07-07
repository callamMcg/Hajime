using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(StepReflex))]

public class Uke : Judoka
{
    private StepReflex stepReflex;
    private float startingRadius;
    public void Pull(Vector2 pull)
    {
        Vector2 p = Vector2.right * pull.x;
        p += Vector2.up * Mathf.Abs(pull.x) / 4;
        balance.SetBalance(p);
    }

    protected override void Awake()
    {
        base.Awake();
        stepReflex = GetComponent<StepReflex>();
    }

    protected override void Start()
    {
        base.Start();
        startingRadius = Vector3.Distance(transform.position, opponent.transform.position);
    }

    protected override void Update()
    {
        base.Update();
        balance.Step();

        Vector2 pivot = new Vector2(opponent.transform.position.x, opponent.transform.position.z);
        Vector2 stepped = body.Planar() + stepReflex.GetStep() * Time.deltaTime;
        Vector2 dir = (stepped - pivot).normalized;
        body.SetPlanar(pivot + dir * startingRadius);
    }
}
