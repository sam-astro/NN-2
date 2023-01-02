using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GrabberScript : MonoBehaviour
{
    public GameObject grabObject;
    public bool isGrabbing;
    [HideInInspector] public IsColliding isCollidingScript;
    SpriteRenderer spriteRenderer;
    [HideInInspector] public Animator anim;
    public Transform grabbedObjectPinLocation;
    public bool canMoveIt;

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
                if (canMoveIt)
                {
                    grabObject.transform.parent = transform;
                    grabObject.transform.position = grabbedObjectPinLocation.position;
                    grabObject.GetComponent<Rigidbody2D>().simulated = false;
                }

                isGrabbing = true;

                anim.SetBool("grabbing", true);

                //spriteRenderer.color = Color.green;

            }
    }

    public void Drop()
    {
        if (isGrabbing)
        {
            // Unparent grabobject so it no longer follows the grabber
            if (canMoveIt)
            {
                grabObject.transform.parent = transform.parent.parent;
                grabObject.GetComponent<Rigidbody2D>().simulated = true;
            }

            isGrabbing = false;

            anim.SetBool("grabbing", false);

            //spriteRenderer.color = Color.red;



            grabObject = null;
        }
    }
}
