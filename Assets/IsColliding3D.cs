using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class IsColliding3D : MonoBehaviour
{
    public bool isColliding;
    public bool failed;

    public string tagName;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == tagName)
        {
            isColliding = true;
        }
        else if (collision.gameObject.tag == "danger")
        {
            isColliding = true;
            failed = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == tagName)
        {
            isColliding = false;
        }
    }
}