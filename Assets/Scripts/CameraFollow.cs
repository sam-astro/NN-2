/*
Copyright (c) Sam Hastings
*/

using UnityEngine;
using UnityEngine.U2D;

public class CameraFollow : MonoBehaviour
{
    #region Variables
    public Transform target;
    public Transform backupTarget;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    public bool matchX = true;
    public bool matchY = true;
    #endregion

    private void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 newTargetPos = Vector3.zero;
            if (matchX)
                newTargetPos.x = target.position.x;
            if (matchY)
                newTargetPos.y = target.position.y;
            Vector3 desiredPosition = newTargetPos + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
        else if(backupTarget!= null)
        {
            Vector3 newTargetPos = Vector3.zero;
            if (matchX)
                newTargetPos.x = backupTarget.position.x;
            if (matchY)
                newTargetPos.y = backupTarget.position.y;
            Vector3 desiredPosition = newTargetPos + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}