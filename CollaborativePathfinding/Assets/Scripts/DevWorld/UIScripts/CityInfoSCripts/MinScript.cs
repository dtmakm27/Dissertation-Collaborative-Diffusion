using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinScript : MonoBehaviour
{
    public UnityEngine.UI.Text Name;
    public PeakingGroup manager;
    public void updateName(){
        Name.text = manager.groupName;
    }
}
