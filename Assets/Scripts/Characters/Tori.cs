using UnityEngine;

public class Tori : Judoka
{
    //------------------Variables------------------//
    //References to uke on opponent
    private Uke uke;

    private Vector2 move;
    private Vector2 pull;

    //------------------Unity Event Functions------------------//
    protected override void Start()
    {
        base.Start();
        uke = opponent.GetComponent<Uke>();
    }

    protected override void Update()
    {
        base.Update();
        
        move = InputReader.Instance.Move;
        pull = InputReader.Instance.Pull;

        Move();
        Pull();
    }

    //------------------Private Functions------------------//
    private void Move()
    {
        float x = move.x;
        float y = move.y;

        movement.SetTarget(opponent, x);
        movement.Move();
    }

    private void Pull()
    {
        uke.Pull(pull);
    }
}
