using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNodeGroup : MonoBehaviour
{
    public int overlappedTotal = 1;
    // Start is called before the first frame update
    void Start()
    {
        NodeScript[] nodes = GetComponentsInChildren<NodeScript>();
        foreach (var node in nodes)
        {
            node.overlappedTotalForArea = this;
        }
    }

    public void updateSections(){
    //attach yourself to all node child objects 
        NodeScript[] nodes = GetComponentsInChildren<NodeScript>();
        foreach (var node in nodes)
        {
            node.overlappedTotalForArea = this;
        }

    }

}
