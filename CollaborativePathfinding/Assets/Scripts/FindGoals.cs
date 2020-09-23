using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FindGoals : MonoBehaviour {

    public GridManagerScript gm;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
    [ExecuteInEditMode]
	void Update () {
		foreach(GameObject go in gm.nodeGrid)
        {
            if (go && go.GetComponent<NodeScript>().goal)
            {
                go.transform.localScale = new Vector3(10, 10, 10);
            }
        }
	}
}
