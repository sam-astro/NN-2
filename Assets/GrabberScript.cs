using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GrabberScript : MonoBehaviour
{
    public GameObject grabObject;
    public bool isGrabbing;
    IsColliding isCollidingScript;
    SpriteRenderer spriteRenderer;
    Animator anim;
    public Transform grabbedObjectPinLocation;

    private void Awake()
    {
        isCollidingScript = GetComponent<IsColliding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    public void Grab()
    {
        if (!isGrabbing)
            if (isCollidingScript.isColliding)
            {
                grabObject = isCollidingScript.colObject;

                // Parent grabobject so it follows the grabber
                grabObject.transform.parent = transform;
                grabObject.transform.position = grabbedObjectPinLocation.position;

                isGrabbing = true;

                anim.SetBool("grabbing", true);

                //spriteRenderer.color = Color.green;

                grabObject.GetComponent<Rigidbody2D>().simulated = false;
            }
    }

    public void Drop()
    {
        if (isGrabbing)
        {
            // Unparent grabobject so it no longer follows the grabber
            grabObject.transform.parent = transform.parent.parent;

            isGrabbing = false;

            anim.SetBool("grabbing", false);

            //spriteRenderer.color = Color.red;

            grabObject.GetComponent<Rigidbody2D>().simulated = true;


            grabObject = null;
        }
    }
}
