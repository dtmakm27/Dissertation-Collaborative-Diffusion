using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDistanceInfoScript : MonoBehaviour {

    public GridManagerScript gridManager;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (gridManager)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Path Distance: " + gridManager.GetPathDistance();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Path Distance: ";
        }
    }
}
