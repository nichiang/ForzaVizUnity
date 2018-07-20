using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControlPanel : MonoBehaviour {

    public RectTransform controlPanelRect;
    public ReadPCAP readPCAP;

    private bool controlPanelOpen = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ToggleControlPanel ()
    {
        if (controlPanelOpen)
        {
            controlPanelRect.anchoredPosition = new Vector2(0, -50f);
        }
        else
        {
            controlPanelRect.anchoredPosition = new Vector2(0, 50f);
        }

        controlPanelOpen = !controlPanelOpen;
    }

    public void SaveCSV ()
    {
        readPCAP.SaveCSV();
        ToggleControlPanel();
    }

    public void LoadCSV ()
    {
        readPCAP.LoadCSV();
        ToggleControlPanel();
    }
}
