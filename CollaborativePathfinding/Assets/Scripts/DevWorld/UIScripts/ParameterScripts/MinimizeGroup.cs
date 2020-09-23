using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimizeGroup : MonoBehaviour
{
    // Start is called before the first frame update
    public UnityEngine.UI.Text btn;
    public GameObject max;
    public GameObject min;
    bool isMin = false;
    string minTxt = "+";
    string maxTxt = "-";

    Vector3 originalSize;
    bool setSize = false;

    public void onMinimizeClick(){
        isMin = !isMin;
        min.SetActive(isMin);
        max.SetActive(!isMin);
        btn.text = isMin? minTxt : maxTxt;
        if(!setSize){
            setSize = true;
            originalSize = new Vector3(gameObject.transform.localScale.x,gameObject.transform.localScale.y,gameObject.transform.localScale.z);
        }
        gameObject.transform.localScale = isMin? 
         new Vector3(originalSize.x/2,originalSize.y/4,originalSize.z) : originalSize;
    }
}
