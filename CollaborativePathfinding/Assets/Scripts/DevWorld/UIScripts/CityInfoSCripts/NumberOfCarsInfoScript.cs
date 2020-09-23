using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberOfCarsInfoScript : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Number of Steam Vechicles Driving: " + gridManager.avatars.Count.ToString();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Driving: ";
        }
    }
}
