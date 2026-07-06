using UnityEngine;

[System.Serializable]
public class IKContext
{
    public Transform Root;
    public Transform Middle;
    public Transform End;
    public Transform Pole;
    public Transform Target;
    public Vector3 LiftPos;
    public float Length;
}
[System.Serializable]
public struct Technique
{
    public string name;
    public string jap;
    [TextArea] public string description;
    [TextArea] public string[] steps;
}