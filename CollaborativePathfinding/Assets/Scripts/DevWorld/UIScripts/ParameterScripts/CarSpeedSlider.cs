using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarSpeedSlider : MonoBehaviour
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
                slider.value = GridManagerScript.secondsPerSecond;
                slider.maxValue = GridManagerScript.maxSecondsPerSecond;
            }
        }
    }


    public void OnSliderValueChange()
    {
        GridManagerScript.secondsPerSecond = (int)slider.value;
      
    }

}