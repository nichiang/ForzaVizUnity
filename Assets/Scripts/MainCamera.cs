using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public enum Direction { forward, backward };

    public Vector3 cameraFollowPosition = new Vector3(0, 5f, -10f);
    public GameObject genericCar;

    private Camera mainCamera;
    private GameObject currentTarget;
    private Vector3 lastMousePosition;

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
                    FollowCurrentPoint(prevSibling, true, Direction.backward);
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
                    FollowCurrentPoint(prevSibling, true, Direction.forward);
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
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            followMode = false;

            GameObject firstSibling = currentTarget.transform.parent.GetChild(0).gameObject;
            FollowCurrentPoint(firstSibling, true, Direction.forward);

            prevStepCount = 0;
            nextStepCount = 0;
            stepThroughMultiplier = 1;
        }
        else
        {
            prevStepCount = 0;
            nextStepCount = 0;
            stepThroughMultiplier = 1;
        }

        if (Input.mouseScrollDelta != Vector2.zero)
        {
            float fov = mainCamera.fieldOfView;
            fov += Input.mouseScrollDelta.y * -2f;
            fov = Mathf.Clamp(fov, 60f, 120f);

            mainCamera.fieldOfView = fov;
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Transform gimbal = transform.parent;

            float yawDelta = Input.mousePosition.x - lastMousePosition.x;
            float pitchDelta = Input.mousePosition.y - lastMousePosition.y;

            Vector3 newRotation = new Vector3(
                gimbal.eulerAngles.x - pitchDelta,
                gimbal.eulerAngles.y + yawDelta, 
                0);

            gimbal.eulerAngles = newRotation;

            lastMousePosition = Input.mousePosition;
        }
    }

    public void FollowCurrentPoint (GameObject go, bool force = false, Direction dir = Direction.forward)
    {
        if (followMode || force)
        {
            Transform t = mainCamera.transform.root;
            t.position = go.transform.position;
            //t.eulerAngles = new Vector3(0, go.transform.eulerAngles.y, 0);

            if (currentTarget != null)
            {
                Vector3 pathForwardDirection = go.transform.position - currentTarget.transform.position;
                pathForwardDirection.y = 0;

                if (dir == Direction.backward)
                    pathForwardDirection *= -1;

                if (pathForwardDirection.magnitude > 0.01f)
                {
                    t.forward = pathForwardDirection;
                }
            }

            //t.position = go.transform.TransformPoint(cameraFollowPosition);
            //t.position = go.transform.position + cameraFollowPosition;
            //transform.LookAt(go.transform);

            DrawGenericCar(go.transform);

            currentTarget = go;
        }
    }

    void DrawGenericCar (Transform t)
    {
        genericCar.transform.position = t.position;
        genericCar.transform.rotation = t.rotation;
    }
}
