using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberOfCarsExitingInfoScript : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Exiting: " + gridManager.avatarsExiting.ToString();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Exiting: ";
        }
    }
}
