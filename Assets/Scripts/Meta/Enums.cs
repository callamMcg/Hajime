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
    Based     // technique control: pinned, will not lift
}