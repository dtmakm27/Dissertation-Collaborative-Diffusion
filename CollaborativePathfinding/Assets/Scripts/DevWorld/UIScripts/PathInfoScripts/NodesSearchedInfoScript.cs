﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodesSearchedInfoScript : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Nodes searched: " + gridManager.GetNodesSearched();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Nodes searched: ";
        }
    }
}
