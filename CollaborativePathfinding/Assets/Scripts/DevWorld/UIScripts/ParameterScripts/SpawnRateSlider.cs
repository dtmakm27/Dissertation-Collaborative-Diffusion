using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnRateSlider : MonoBehaviour {

    public GridManagerScript gridManager;
    public Slider slider;

    // Use this for initialization
    void Start()
    {
        if (gridManager)
        {
            if (slider)
            {
                slider.value = gridManager.diffusionAvatarSpawnRate;
            }
        }
    }


    public void OnSliderValueChange()
    {
        gridManager.diffusionAvatarSpawnRate = slider.value;
    }
}
