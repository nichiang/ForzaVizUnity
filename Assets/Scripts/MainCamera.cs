using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public enum Direction { forward, backward };

    public Vector3 cameraFollowPosition = new Vector3(0, 5f, -10f);
    public GameObject car;
    public Visualizations visualizations;
    public CarVizAnchor graphAnchor, tractionCircleAnchor;

    private Camera mainCamera;
    private GameObject currentNode;
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

        // Go backwards
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            if (currentNode != null)
            {
                int prevIndex = currentNode.transform.GetSiblingIndex() - stepThroughMultiplier;

                if (prevIndex >= 0)
                {
                    followMode = false;

                    GameObject prevSibling = currentNode.transform.parent.GetChild(prevIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true, Direction.backward);
                    visualizations.DrawCarVisualizationsAtIndex(prevIndex);
                }

                prevStepCount++;

                if (prevStepCount > 100) stepThroughMultiplier = 2;
                else if (prevStepCount > 200) stepThroughMultiplier = 4;
                else if (prevStepCount > 400) stepThroughMultiplier = 8;
            }
        }

        // Go forward
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            if (currentNode != null)
            {
                int nextIndex = currentNode.transform.GetSiblingIndex() + stepThroughMultiplier;

                if (nextIndex < currentNode.transform.parent.childCount)
                {
                    followMode = false;

                    GameObject prevSibling = currentNode.transform.parent.GetChild(nextIndex).gameObject;
                    FollowCurrentPoint(prevSibling, true, Direction.forward);
                    visualizations.DrawCarVisualizationsAtIndex(nextIndex);
                }

                nextStepCount++;

                if (nextStepCount > 100) stepThroughMultiplier = 2;
                else if (nextStepCount > 200) stepThroughMultiplier = 4;
                else if (nextStepCount > 400) stepThroughMultiplier = 8;
            }
        }

        // Toggle follow mode
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            followMode = !followMode;

            if (followMode)
            {
                int currentIndex = visualizations.CurrentPoint().transform.GetSiblingIndex();
                visualizations.DrawCarVisualizationsAtIndex(currentIndex);
                visualizations.ShowElevationLines(true);
            }
            else
            {
                visualizations.ShowElevationLines(false);
            }
        }

        // Go to first position
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            followMode = false;

            GameObject firstSibling = currentNode.transform.parent.GetChild(0).gameObject;
            FollowCurrentPoint(firstSibling, true, Direction.forward);

            prevStepCount = 0;
            nextStepCount = 0;
            stepThroughMultiplier = 1;
        }

        // Reset camera orientation and zoom
        else if (Input.GetKeyDown(KeyCode.R))
        {
            mainCamera.transform.parent.localEulerAngles = cameraFollowPosition;
            mainCamera.fieldOfView = 60f;
        }

        else
        {
            prevStepCount = 0;
            nextStepCount = 0;
            stepThroughMultiplier = 1;
        }

        // Camera zoom
        if (Input.mouseScrollDelta != Vector2.zero)
        {
            float fov = mainCamera.fieldOfView;
            fov += Input.mouseScrollDelta.y * -2f;
            fov = Mathf.Clamp(fov, 60f, 120f);

            mainCamera.fieldOfView = fov;
        }

        // Orbit camera
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

    public bool IsFollowing()
    {
        return followMode;
    }

    public void FollowCurrentPoint (GameObject node, bool force = false, Direction dir = Direction.forward)
    {
        if (followMode || force)
        {
            Transform t = mainCamera.transform.root;
            t.position = node.transform.position;

            if (currentNode != null)
            {
                Vector3 pathForwardDirection = node.transform.position - currentNode.transform.position;
                pathForwardDirection.y = 0;

                if (dir == Direction.backward)
                    pathForwardDirection *= -1;

                if (pathForwardDirection.magnitude > 0.01f)
                {
                    t.forward = pathForwardDirection;
                }
            }

            car.transform.position = node.transform.position;
            car.transform.rotation = node.transform.rotation;

            graphAnchor.UpdatePosition();
            tractionCircleAnchor.UpdatePosition();

            currentNode = node;
        }
    }

    public void GoToPoint (GameObject node)
    {
        followMode = false;
        FollowCurrentPoint(node, true, Direction.forward);
    }
}
