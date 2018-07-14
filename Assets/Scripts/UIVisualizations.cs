using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVisualizations : MonoBehaviour {

    public Text speed;

    private MainCamera mainCamera;

	// Use this for initialization
	void Start ()
    {
        mainCamera = Camera.main.GetComponent<MainCamera>();
	}
	
	// Update is called once per frame
	public void DrawUI (ForzaPacket packet)
    {
        if (mainCamera.IsFollowing())
        {
            speed.text = Mathf.RoundToInt(packet.VelocityZ * 2.237f).ToString();
        }
	}

    public void DrawUIAtIndex (int packetIndex)
    {
        ForzaPacket packet = DataPoints.GetPoint(packetIndex).GetPacket();

        speed.text = Mathf.RoundToInt(packet.VelocityZ * 2.237f).ToString();
    }

    public void ResetUIVisualizations ()
    {
        speed.text = "000";
    }
}
