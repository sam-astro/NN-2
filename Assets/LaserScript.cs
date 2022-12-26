using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class LaserScript : MonoBehaviour
{
    private Vector3 originalPosition;
    public bool moving;
    public float speed = 1f;

    void Start()
    {
        originalPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (moving)
            transform.position += new Vector3(Time.fixedDeltaTime*speed, 0);
    }

    public void ResetPosition()
    {
        transform.position = originalPosition;
    }
}
