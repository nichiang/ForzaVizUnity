using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackInfo : MonoBehaviour {

    public Visualizations visualizations;
    public float searchDistance = 20f;

    private MainCamera mainCamera;
    private Transform dataPointsRoot;
    private List<GameObject> lapStartingPoints;

    private float currentLapIndex = 0;
    private float lastDistance = float.MaxValue;
    private bool startingLap = true;

    void Start ()
    {
        mainCamera = Camera.main.GetComponent<MainCamera>();
        lapStartingPoints = new List<GameObject>();
        dataPointsRoot = visualizations.gameObject.transform;
    }

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (lapStartingPoints.Count > 0)
            {
                for (int i = lapStartingPoints.Count - 1; i >= 0; i--)
                {
                    if (visualizations.CurrentPoint().transform.GetSiblingIndex() > lapStartingPoints[i].transform.GetSiblingIndex())
                    {
                        mainCamera.GoToPoint(lapStartingPoints[i]);
                        break;
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (lapStartingPoints.Count > 0)
            {
                bool goToEnd = true;

                for (int i = 0; i < lapStartingPoints.Count; i++)
                {
                    if (visualizations.CurrentPoint().transform.GetSiblingIndex() < lapStartingPoints[i].transform.GetSiblingIndex())
                    {
                        mainCamera.GoToPoint(lapStartingPoints[i]);
                        goToEnd = false;
                        break;
                    }
                }

                if (goToEnd)
                {
                    mainCamera.GoToPoint(dataPointsRoot.GetChild(dataPointsRoot.childCount - 1).gameObject);
                }
            }
        }
    }

    public void FindLap (GameObject go)
    {
        if (lapStartingPoints.Count == 0 && dataPointsRoot.childCount > 0)
        {
            lapStartingPoints.Add(dataPointsRoot.GetChild(0).gameObject);
        }

        Vector3 firstPointPosition = dataPointsRoot.GetChild(0).position;
        float distance = Vector3.Distance(firstPointPosition, go.transform.position);

        if (distance >= searchDistance)
        {
            startingLap = false;
        }

        if (distance < searchDistance && !startingLap)
        {
            if (distance < lastDistance)
            {
                lastDistance = distance;
            }
            else
            {
                Debug.Log("Lap recorded, node position: " + go.transform.position);
                lapStartingPoints.Add(go);
                lastDistance = float.MaxValue;
                startingLap = true;
                currentLapIndex += 1f;
            }
        }
    }
}
