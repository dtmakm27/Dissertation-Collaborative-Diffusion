using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayControls : MonoBehaviour {

    public GridManagerScript gridManager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (gridManager && !gridManager.diffusionMode)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Controls:\nS: Toggle diffusion visibility\nD: Use Djikstra pathfinding\n" +
                                                       "P: Use a pure heuristic search\nH: Cycle heuristics\nM: Change to diffusion avatar mode\n" +
                                                       "Space: Create a blocking node\nLeft - click: Create / remove start node\nRight - click: Create / remove end node";
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Controls:\nS: Toggle diffusion visibility\nM: Change to Pathfinding mode\n" +
                                                       "Space: Create a blocking node\nLeft - click: Create avatar\nRight - click: Create / remove end node";
        }
    }
}
