using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.FastLineRenderer;

public class Graph : MonoBehaviour
{

    public LineRenderer lineRenderer;
    public int visiblePointsCount = 20;

    private Queue<float> dataPoints;
    private float pointOffset;

    // Use this for initialization
    void Start()
    {
        dataPoints = new Queue<float>();
        lineRenderer.positionCount = visiblePointsCount;
        pointOffset = 2f / visiblePointsCount;

        for (int i = 0; i < visiblePointsCount; i++)
        {
            dataPoints.Enqueue(0);
            lineRenderer.SetPosition(i, new Vector3(i * pointOffset - 1f, 0, 0));
        }
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.forward = Camera.main.transform.forward;
    }

    public void AddPoint(float point)
    {
        dataPoints.Enqueue(point);

        if (dataPoints.Count > visiblePointsCount)
        {
            dataPoints.Dequeue();
        }

        Queue<float>.Enumerator pointsEnum = dataPoints.GetEnumerator();

        for (int i = visiblePointsCount - 1; i >= 0; i--)
        {
            pointsEnum.MoveNext();
            Vector3 newPoint = new Vector3(1f - i * pointOffset, pointsEnum.Current - 0.5f, 0);
            lineRenderer.SetPosition(i, newPoint);
        }
    }

    public void AddPoints(List<float> points)
    {
        if (points.Count != visiblePointsCount)
            return;

        dataPoints.Clear();

        foreach (float p in points)
        {
            dataPoints.Enqueue(p);
        }

        Queue<float>.Enumerator pointsEnum = dataPoints.GetEnumerator();

        for (int i = 0; i < visiblePointsCount; i++)
        {
            pointsEnum.MoveNext();
            Vector3 newPoint = new Vector3(1f - i * pointOffset, pointsEnum.Current - 0.5f, 0);
            lineRenderer.SetPosition(i, newPoint);
        }
    }
}
