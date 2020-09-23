using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvasionStrengthSlider : MonoBehaviour
{

    public GridManagerScript gridManager;
    public Slider slider;

    // Use this for initialization
    void Start()
    {
        if (gridManager)
        {
            if (slider)
            {
                slider.value = GridManagerScript.evasionStrength;
            }
        }
    }


    public void OnSliderValueChange()
    {
        GridManagerScript.evasionStrength = slider.value;
    }

}
