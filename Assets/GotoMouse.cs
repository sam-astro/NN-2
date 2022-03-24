using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GotoMouse : MonoBehaviour
{
    
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            transform.position = Vector2.Lerp(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition), Time.deltaTime);
        }
    }
}
