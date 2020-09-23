using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTakenInfoScript : MonoBehaviour {

    public GridManagerScript gridManager;
    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(gridManager)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Time Taken: " + gridManager.GetPathTime().ToString() + "ms";
        }
        else
        {

            GetComponent<UnityEngine.UI.Text>().text = "Time Taken: ";
        }
    }
}
