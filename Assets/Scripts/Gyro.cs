using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Gyro : MonoBehaviour
{
    void Update()
    {
        transform.up = Vector3.up;
    }
}