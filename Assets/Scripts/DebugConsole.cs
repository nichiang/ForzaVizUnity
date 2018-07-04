using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour {

    static Text debugText;

    void Awake ()
    {
        debugText = GetComponent<Text>();
    }

    public static void Write (string s)
    {
        debugText.text = s;
    }

    public static void Append (string s)
    {
        debugText.text = debugText.text + s;
    }
}
