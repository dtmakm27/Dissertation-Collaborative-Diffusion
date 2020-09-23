using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentHeuristicTextScript : MonoBehaviour {

    public GridManagerScript GridManager;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (GridManager.useDijkstra)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Current Heuristic: N/A";
        }
        else
        {
            switch (GridManager.heuristic)
            {
                case GridManagerScript.Heuristic.PYTHAGORAS:
                    GetComponent<UnityEngine.UI.Text>().text = "Current Heuristic: Pythagoras";
                    break;
                case GridManagerScript.Heuristic.MANHATTAN:
                    GetComponent<UnityEngine.UI.Text>().text = "Current Heuristic: Manhattan";
                    break;
                case GridManagerScript.Heuristic.COLLABORATIVEDIFF:
                    GetComponent<UnityEngine.UI.Text>().text = "Current Heuristic: Diffusion";
                    break;
            }
        }
	}
}
