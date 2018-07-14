using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class Graph : MonoBehaviour
{

    public LineRenderer lineRenderer;
    public UILineRenderer uiLineRenderer;
    public int visiblePointsCount = 20;

    private Queue<float> dataPoints = new Queue<float>();
    private float pointOffset;

    private float uiWidth;
    private float uiHeight;

    // Use this for initialization
    void Start ()
    {
        ResetGraph();
    }

    // Update is called once per frame
    void Update ()
    {
        if (lineRenderer != null)
            this.transform.forward = Camera.main.transform.forward;
    }

    public void AddPoint (float point)
    {
        dataPoints.Enqueue(point);

        if (dataPoints.Count > visiblePointsCount)
        {
            dataPoints.Dequeue();
        }

        Queue<float>.Enumerator pointsEnum = dataPoints.GetEnumerator();

        if (lineRenderer != null)
        {
            for (int i = visiblePointsCount - 1; i >= 0; i--)
            {
                pointsEnum.MoveNext();
                Vector3 newPoint = new Vector3(1f - i * pointOffset, pointsEnum.Current - 0.5f, 0);
                lineRenderer.SetPosition(i, newPoint);
            }
        }
        else if (uiLineRenderer != null)
        {
            for (int i = visiblePointsCount - 1; i >= 0; i--)
            {
                pointsEnum.MoveNext();
                Vector3 newPoint = new Vector3((1f - i * pointOffset) * uiWidth, pointsEnum.Current * uiHeight);
                uiLineRenderer.Points[i] = newPoint;
            }

            uiLineRenderer.Apply();
        }
    }

    public void AddPoints (List<float> points)
    {
        if (points.Count != visiblePointsCount)
            return;

        dataPoints.Clear();

        foreach (float p in points)
        {
            dataPoints.Enqueue(p);
        }

        Queue<float>.Enumerator pointsEnum = dataPoints.GetEnumerator();

        if (lineRenderer != null)
        {
            for (int i = 0; i < visiblePointsCount; i++)
            {
                pointsEnum.MoveNext();
                Vector3 newPoint = new Vector3(1f - i * pointOffset, pointsEnum.Current - 0.5f, 0);
                lineRenderer.SetPosition(i, newPoint);
            }
        }
        else if (uiLineRenderer != null)
        {
            for (int i = 0; i < visiblePointsCount; i++)
            {
                pointsEnum.MoveNext();
                Vector3 newPoint = new Vector3((1f - i * pointOffset) * uiWidth, pointsEnum.Current * uiHeight);
                uiLineRenderer.Points[i] = newPoint;
            }

            uiLineRenderer.Apply();
        }
    }

    public void ResetGraph ()
    {
        dataPoints.Clear();

        if (lineRenderer != null)
        {
            pointOffset = 2f / visiblePointsCount;
            lineRenderer.positionCount = visiblePointsCount;

            for (int i = 0; i < visiblePointsCount; i++)
            {
                dataPoints.Enqueue(0);
                lineRenderer.SetPosition(i, new Vector3(i * pointOffset - 1f, 0, 0));
            }
        }
        else if (uiLineRenderer != null)
        {
            pointOffset = 1f / visiblePointsCount;
            uiLineRenderer.Points = new Vector2[visiblePointsCount];

            uiWidth = uiLineRenderer.GetComponent<RectTransform>().rect.width;
            uiHeight = uiLineRenderer.GetComponent<RectTransform>().rect.height;

            for (int i = 0; i < visiblePointsCount; i++)
            {
                dataPoints.Enqueue(0);
                uiLineRenderer.Points[i] = new Vector2(i * pointOffset * uiWidth, 0);
            }

            uiLineRenderer.Apply();
        }
    }
}
