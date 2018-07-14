using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public enum Direction { forward, backward };

    public Vector3 cameraFollowPosition = new Vector3(0, 5f, -10f);
    public GameObject car;
    public Visualizations visualizations;
    public UIVisualizations uiVisualizations;
    public CarVizAnchor graphAnchor, tractionCircleAnchor;

    private Camera mainCamera;
    private DataPoint lastPoint;
    private Vector3 lastMousePosition;

    private bool followMode = true;
    private int stepThroughMultiplier = 1;
    private int currentFollowIndex = 0;

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
            if (lastPoint != null)
            {
                int prevIndex = currentFollowIndex - stepThroughMultiplier;

                if (prevIndex >= 0)
                {
                    followMode = false;

                    FollowCurrentPoint(prevIndex, true, Direction.backward);
                    visualizations.DrawCarVisualizationsAtIndex(prevIndex);
                    uiVisualizations.DrawUIAtIndex(prevIndex);
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
            if (lastPoint != null)
            {
                int nextIndex = currentFollowIndex + stepThroughMultiplier;

                if (nextIndex <= DataPoints.GetLatestPacketIndex())
                {
                    followMode = false;

                    FollowCurrentPoint(nextIndex, true, Direction.forward);
                    visualizations.DrawCarVisualizationsAtIndex(nextIndex);
                    uiVisualizations.DrawUIAtIndex(nextIndex);
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
                int currentIndex = DataPoints.GetLatestPacketIndex();
                visualizations.DrawCarVisualizationsAtIndex(currentIndex);
                uiVisualizations.DrawUIAtIndex(currentIndex);
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

            //GameObject firstSibling = lastPoint.transform.parent.GetChild(0).gameObject;
            FollowCurrentPoint(0, true, Direction.forward);

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

            if (newRotation.x > 270 || newRotation.x < 90)
                gimbal.eulerAngles = newRotation;

            lastMousePosition = Input.mousePosition;
        }
    }

    public bool IsFollowing()
    {
        return followMode;
    }

    public void FollowCurrentPoint (int pointIndex, bool force = false, Direction dir = Direction.forward)
    {
        if (followMode || force)
        {
            DataPoint p = DataPoints.GetPoint(pointIndex);
            Transform t = mainCamera.transform.root;
            t.position = p.GetPosition();

            if (lastPoint != null)
            {
                Vector3 pathForwardDirection = p.GetPosition() - lastPoint.GetPosition();
                pathForwardDirection.y = 0;

                if (dir == Direction.backward)
                    pathForwardDirection *= -1;

                if (pathForwardDirection.magnitude > 0.01f)
                {
                    t.forward = pathForwardDirection;
                }
            }

            car.transform.position = p.GetPosition();
            car.transform.rotation = p.GetRotation();

            graphAnchor.UpdatePosition();
            tractionCircleAnchor.UpdatePosition();

            currentFollowIndex = pointIndex;
            lastPoint = p;
        }
    }

    public void GoToPoint (int pointIndex)
    {
        followMode = false;
        FollowCurrentPoint(pointIndex, true, Direction.forward);
    }

    public void ResetCamera ()
    {
        car.transform.position = Vector3.zero;
        car.transform.rotation = Quaternion.identity;

        Transform t = mainCamera.transform;

        t.root.position = Vector3.zero;
        t.root.forward = car.transform.forward;
        t.parent.localEulerAngles = cameraFollowPosition;
        mainCamera.fieldOfView = 60f;

        graphAnchor.UpdatePosition();
        tractionCircleAnchor.UpdatePosition();
    }
}
