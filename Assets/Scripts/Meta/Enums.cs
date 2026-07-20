public enum AttackSM
{
    standard, leftSweep, rightSweep, leftThrow, rightThrow, doubleThrow, win
}
public enum TechniquePhase
{
    Inactive, // idle, ready to Begin
    Reaching, // moving into position - can still be cancelled
    Executing // committed - plays out to a Score or a Fail
}
public enum FootId
{
    Left, Right
}

public enum FootState
{
    Planted,  // holding a world point, load bearing
    Swinging, // mid step, timed by the gait phase
    Hovering, // cannot reach the floor, dangling beneath the hip
    Swept,    // technique control: chasing the opponent's leg
    Reaping,  // technique control: chasing a hip throw's throwTarget
    Based,    // technique control: pinned, will not lift
    Held,     // technique control: seized by the opponent, placed where the technique says
    Limp,     // technique control: not driven at all, the target rides its parent through a fall
    Placing,  // technique control: stepping through the air to a commanded point, then bases there
    Settling, // technique control: easing back to the standing home under the body, then plants
}

public enum UkeState
{
    Fighting, // live: balance spring, step reflex, radius hold
    Swept,    // a technique owns the lean; the spring is bypassed
    Pressed,  // a technique pulls while the spring still runs; feet pinned, no stepping
    Falling,  // the scripted topple is in flight
    Vaulting, // a technique turns uke's whole body over a pivot (hip throws)
    Fallen    // flat on the mat, holding the final pose
}