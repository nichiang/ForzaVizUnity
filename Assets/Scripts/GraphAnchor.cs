using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphAnchor : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdatePosition()
    {
        Transform t = Camera.main.transform.root;

        this.transform.position = t.position;
        this.transform.forward = t.forward;
    }
}
