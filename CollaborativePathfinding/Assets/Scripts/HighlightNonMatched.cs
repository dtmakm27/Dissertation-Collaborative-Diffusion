using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[ExecuteInEditMode]
public class HighlightNonMatched : MonoBehaviour
{

    public GridManagerScript manager;
    public Material noNeighbour;
    public Material withNeighbours;

    // Update is called once per frame
    void Update()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            foreach (GameObject node in manager.nodeGrid)
            {
                if (node != null)
                {
                    node.GetComponent<Renderer>().material = node.GetComponent<NodeScript>().neighbours.Length > 0 ? withNeighbours : noNeighbour;
                }
            }
        }
    }
}
