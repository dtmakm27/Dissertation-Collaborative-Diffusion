using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DiffusionCoefficientScript : MonoBehaviour {

    public GridManagerScript gridManager;
    public InputField inputField;
    public Slider slider;

    // Use this for initialization
    void Start()
    {

        if (gridManager)
        {
            if (inputField)
            {
                inputField.text = gridManager.diffusionCoeff.ToString();
            }

            if (slider)
            {
                slider.value = gridManager.diffusionCoeff * 1000;
            }
        }
    }

    public void OnInputFieldValueChange()
    {
        try
        {
            float floatValue = float.Parse(inputField.text);
            gridManager.diffusionCoeff = floatValue;
            slider.value = floatValue * 1000;

            if (gridManager.gridCreated)
            {
                gridManager.ResetDiffusion();
            }
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
        gridManager.diffusionCoeff = slider.value / 1000;
        inputField.text = gridManager.diffusionCoeff.ToString();

        if (gridManager.gridCreated)
        {
            gridManager.ResetDiffusion();
        }
    }
}
