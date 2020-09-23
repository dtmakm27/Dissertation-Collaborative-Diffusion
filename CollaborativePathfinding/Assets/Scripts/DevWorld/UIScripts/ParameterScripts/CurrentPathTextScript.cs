using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentPathTextScript : MonoBehaviour {

    public GridManagerScript GridManager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if(!GridManagerScript.usingAStarOnly){
            if(GridManagerScript.usingAugmentedDiffusion){
                GetComponent<UnityEngine.UI.Text>().text = "Using Collaborative Diffusion";
            }else
            {
                GetComponent<UnityEngine.UI.Text>().text = "Using Non-collaborative Pathfinding";
            }
        }else {
            GetComponent<UnityEngine.UI.Text>().text = "Using A*";
        }

        // if (GridManager.useDijkstra)
        // {
        //     GetComponent<UnityEngine.UI.Text>().text = "Current Pathfinding: Dijkstra";
        // }
        // else if(GridManager.pureHeuristic)
        // {
        //     GetComponent<UnityEngine.UI.Text>().text = "Current Pathfinding: Pure Heuristic";
        // }
        // else
        // {
        //     GetComponent<UnityEngine.UI.Text>().text = "Current Pathfinding: A*";
        // }
    }
}
