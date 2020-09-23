using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberOfCarsEnteringInfoScript : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Entering: " + gridManager.avatarsEntering.ToString();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Entering: ";
        }
    }
}
