using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecayRateScript : MonoBehaviour {

    public GridManagerScript gridManager;
    public InputField inputField;
    public Slider slider;

	// Use this for initialization
	void Start () {

		if(gridManager)
        {
            if(inputField)
            {
                inputField.text = gridManager.decayRate.ToString();
            }

            if(slider)
            {
                slider.value = gridManager.decayRate * 1000;
            }
        }
	}
	
    public void OnInputFieldValueChange ()
    {
        try
        {
            float floatValue = float.Parse(inputField.text);
            gridManager.decayRate = floatValue;
            slider.value = floatValue * 1000;
        }
        catch (FormatException)
        {
            inputField.text = inputField.text + " is not in a valid format.";
        }
        catch (OverflowException)
        {
            inputField.text = inputField.text + " is outside a float's range.";
        }
    }

    public void OnSliderValueChange()
    {
        gridManager.decayRate = slider.value/1000;
        inputField.text = gridManager.decayRate.ToString();
    }

}
