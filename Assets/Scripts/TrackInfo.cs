using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackInfo : MonoBehaviour {

    public Visualizations visualizations;
    public float searchDistance = 20f;

    private MainCamera mainCamera;
    private List<int> lapStartingPoints = new List<int>();

    private float currentLapIndex = 0;
    private float lastDistance = float.MaxValue;
    private bool startingLap = true;

    void Start ()
    {
        mainCamera = Camera.main.GetComponent<MainCamera>();
    }

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (lapStartingPoints.Count > 0)
            {
                for (int i = lapStartingPoints.Count - 1; i >= 0; i--)
                {
                    if (DataPoints.GetLatestPacketIndex() > lapStartingPoints[i])
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
                    if (DataPoints.GetLatestPacketIndex() < lapStartingPoints[i])
                    {
                        mainCamera.GoToPoint(lapStartingPoints[i]);
                        goToEnd = false;
                        break;
                    }
                }

                if (goToEnd)
                {
                    mainCamera.GoToPoint(DataPoints.GetLatestPacketIndex());
                }
            }
        }
    }

    public int CheckNewLap (int packetIndex)
    {
        if (lapStartingPoints.Count == 0 && DataPoints.GetLatestPacketIndex() >= 0)
        {
            lapStartingPoints.Add(0);
        }

        Vector3 firstPointPosition = DataPoints.GetPoint(0).GetPosition();
        float distance = Vector3.Distance(firstPointPosition, DataPoints.GetPoint(packetIndex).GetPosition());

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
                Debug.Log("Lap recorded, node position: " + packetIndex);
                lapStartingPoints.Add(packetIndex);
                lastDistance = float.MaxValue;
                startingLap = true;
                currentLapIndex += 1f;
            }
        }

        return lapStartingPoints.Count;
    }

    // lapNum is zero index
    public int GetGhostPosition (int lapNum)
    {
        if (lapNum < 0 || lapNum >= lapStartingPoints.Count)
        {
            return -1;
        }
        else
        {
            return lapStartingPoints[lapNum] + DataPoints.GetLatestPacketIndex() - lapStartingPoints[lapStartingPoints.Count - 1];
        }
    }

    public List<int> GetGhostPositionsAtIndex (int index)
    {
        List<int> indices = new List<int>();
        int indexLap = (int)DataPoints.GetPoint(index).GetPacket().LapNum;

        for (int i = 0; i < lapStartingPoints.Count; i++)
        {
            if (i == indexLap)
            {
                indices.Add(index);
            }
            else
            {
                int position = lapStartingPoints[i] + index - lapStartingPoints[indexLap];

                indices.Add(DataPoints.IsValidIndex(position) ? position : -1);
            }
        }

        return indices;
    }

    public int CurrentLap ()
    {
        return lapStartingPoints.Count;
    }
}
