using UnityEngine;

[System.Serializable]
public class IKContext
{
    public Transform Root;
    //public Transform Middle;
    //public Transform End;
    //public Transform Pole;
    public Transform Target;
    //public Vector3 LiftPos;
    //public float Length;
}
[System.Serializable]
public struct Technique
{
    public string name;
    public string jap;
    [TextArea] public string description;
    [TextArea] public string[] steps;
}

public class TechniqueContext
{
    public Transform opponent;
    public JudokaBody body;
    public PolarMovement movement;
    public Balance balance;
    //public Gait gait;
}

public struct BodyPose
{
    public Vector2 planar;
    public float height;
    public Vector3 lean;
    public float yaw;
}

public struct LimbPose
{
    public Vector3 pos;
    public Quaternion rot;
}

public struct JudokaSnapshot
{
    public BodyPose body;
    public LimbPose leftLeg, rightLeg, leftArm, rightArm;
}

public struct ReplayFrame
{
    public float time;
    public JudokaSnapshot tori;
    public JudokaSnapshot uke;
}