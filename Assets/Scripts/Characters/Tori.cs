using UnityEngine;

public class Tori : Judoka
{
    //------------------Variables------------------//
    //References to uke on opponent
    private Uke uke;
    [SerializeField] private FootManager feet;

    private Vector2 move;
    private Vector2 pull;
    private AttackSM attackState;


    //------------------Unity Event Functions------------------//
    protected override void Start()
    {
        base.Start();
        uke = opponent.GetComponent<Uke>();
    }

    protected override void Update()
    {
        base.Update();


        attackState = InputReader.Instance.AttackState;

        switch (attackState)
        {
            case AttackSM.rightSweep:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                feet.Sweep(FootId.Right);
                break;
            case AttackSM.leftSweep:
                move = Vector2.zero;
                pull = InputReader.Instance.Pull;
                feet.Sweep(FootId.Left);
                break;
            default:
                move = InputReader.Instance.Move;
                pull = InputReader.Instance.Pull;
                feet.Free();
                break;
        }
        Move();
        Pull();
    }

    //------------------Private Functions------------------//
    private void Move()
    {
        float x = move.x;

        movement.SetTarget(opponent, x);
        movement.Move();
    }

    private void Pull()
    {
        uke.Pull(pull);
    }
}