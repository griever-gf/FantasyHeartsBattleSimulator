using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingIcon : MonoBehaviour
{
    public float lifeTime = 0.8f;
    float speed = 0.7f;
    float startTime;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;
        if (Time.time - startTime > lifeTime)
            Destroy(gameObject);
    }
}
