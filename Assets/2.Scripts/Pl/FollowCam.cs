using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [SerializeField] Transform targetTr;

    [Range(2.0f, 20.0f)]
    public float distance = 10.0f;

    [Range(0.0f, 10.0f)]
    public float height = 2.0f;

    public float damping = 10.0f;

    public float targetOffset = 2.0f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        Vector3 pos = targetTr.position + (-targetTr.forward * distance) + (Vector3.up * height);

        transform.position = Vector3.SmoothDamp(transform.position,pos,ref velocity,damping);       
        transform.LookAt(targetTr.position + (targetTr.up * targetOffset));
    }
}