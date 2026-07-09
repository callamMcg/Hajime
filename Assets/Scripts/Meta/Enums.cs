public enum AttackSM
{
    standard, leftSweep, rightSweep, leftThrow, rightThrow, win
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
    Based,    // technique control: pinned, will not lift
    Held,     // technique control: seized by the opponent, placed where the technique says
    Limp      // technique control: not driven at all, the target rides its parent through a fall
}

public enum UkeState
{
    Fighting, // live: balance spring, step reflex, radius hold
    Swept,    // a technique owns the lean; the spring is bypassed
    Falling,  // the scripted fall is in flight
    Fallen    // flat on the mat, holding the final pose
}