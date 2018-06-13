using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public Vector3 cameraFollowPosition = new Vector3(0, 5f, -10f);

    private Camera mainCamera;
    private GameObject currentTarget;

    private bool followMode = true;

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
                int prevIndex = currentTarget.transform.GetSiblingIndex() - 1;

                if (prevIndex >= 0)
                {
                    followMode = false;

                    GameObject prevSibling = currentTarget.transform.parent.GetChild(prevIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true);
                }
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            if (currentTarget != null)
            {
                int nextIndex = currentTarget.transform.GetSiblingIndex() + 1;

                if (nextIndex < currentTarget.transform.parent.childCount)
                {
                    followMode = false;

                    GameObject prevSibling = currentTarget.transform.parent.GetChild(nextIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            followMode = true;
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
