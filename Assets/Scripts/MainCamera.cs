using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public Vector3 cameraFollowPosition = new Vector3(0, 5f, -10f);

    private Camera mainCamera;
    private GameObject currentTarget;

    private bool followMode = true;
    private int stepThroughMultiplier = 1;

    private int prevStepCount = 0;
    private int nextStepCount = 0;

	// Use this for initialization
	void Start () {
        mainCamera = GetComponent<Camera>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKey(KeyCode.DownArrow))
        {
            if (currentTarget != null)
            {
                int prevIndex = currentTarget.transform.GetSiblingIndex() - stepThroughMultiplier;

                if (prevIndex >= 0)
                {
                    followMode = false;

                    GameObject prevSibling = currentTarget.transform.parent.GetChild(prevIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true);
                }

                prevStepCount++;

                if (prevStepCount > 100) stepThroughMultiplier = 2;
                else if (prevStepCount > 200) stepThroughMultiplier = 4;
                else if (prevStepCount > 400) stepThroughMultiplier = 8;
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            if (currentTarget != null)
            {
                int nextIndex = currentTarget.transform.GetSiblingIndex() + stepThroughMultiplier;

                if (nextIndex < currentTarget.transform.parent.childCount)
                {
                    followMode = false;

                    GameObject prevSibling = currentTarget.transform.parent.GetChild(nextIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true);
                }

                nextStepCount++;

                if (nextStepCount > 100) stepThroughMultiplier = 2;
                else if (nextStepCount > 200) stepThroughMultiplier = 4;
                else if (nextStepCount > 400) stepThroughMultiplier = 8;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            followMode = !followMode;
        }
        else
        {
            prevStepCount = 0;
            nextStepCount = 0;
            stepThroughMultiplier = 1;
        }
    }

    public void FollowCurrentPoint (GameObject go, bool force = false)
    {
        if (followMode || force)
        {
            Transform t = mainCamera.transform;

            t.position = go.transform.TransformPoint(cameraFollowPosition);
            transform.LookAt(go.transform);

            currentTarget = go;
        }
    }
}
