using System;
using UnityEngine;

[DefaultExecutionOrder(51)]

public class Gait : MonoBehaviour
{
    Vector3 earlyPos;
    float velocity;
    float elapsed;
    float y = 0;
    [SerializeField] float a;
    [SerializeField] float f;
    private void Update()
    {
        earlyPos = transform.position;
    }
    private void LateUpdate()
    {
        velocity = Vector3.Distance(transform.position, earlyPos) / Time.deltaTime;
    }

    public float Wave()
    {
        if (velocity < 0.2f)
        {
            elapsed = 0;
            y = Mathf.Lerp(y, -a/3, 9 * Time.deltaTime);
        }

        y = Mathf.Lerp(y, (Mathf.Sin(elapsed) * a) - a/2, 18 * Time.deltaTime);
        elapsed += Time.deltaTime * f;
        return y;
    }
}
