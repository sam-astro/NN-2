using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class IsColliding : MonoBehaviour
{
	public bool isColliding;
	public bool failed;
	public bool canFail;

	public string tagName;

	public GameObject colObject;

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.tag == tagName)
		{
			isColliding = true;
			colObject = collision.gameObject;
		}
		else if (collision.gameObject.tag == "danger" && canFail)
		{
			failed = true;
			colObject = null;
		}
	}

	void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.tag == tagName)
		{
			isColliding = false;
			colObject = null;
		}
	}
    
    void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == tagName)
		{
			isColliding = true;
			colObject = collision.gameObject;
		}
		else if (collision.gameObject.tag == "danger" && canFail)
		{
			failed = true;
			colObject = null;
		}
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject.tag == tagName)
		{
			isColliding = false;
			colObject = null;
		}
	}
}