using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    float desiredAspect;
    float cameraSize;

    private void Awake()
    {
        desiredAspect = 16f / 9f;
        cameraSize = Camera.main.orthographicSize;
    }

    void Update()
    {
        //Debug.Log(aspect);
        Camera.main.orthographicSize = cameraSize * desiredAspect / Camera.main.aspect;
    }
}
