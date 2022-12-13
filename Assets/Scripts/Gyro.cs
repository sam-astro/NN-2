using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Gyro : MonoBehaviour
{
    public Transform parent;
    void Update()
    {
        transform.up = Vector3.up;
        if (parent != null)
            transform.position = parent.position;
    }
}