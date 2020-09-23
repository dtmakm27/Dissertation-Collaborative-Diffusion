using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GroupVariablesChange : MonoBehaviour {

    public PeakingGroup groupManager;
    public InputField text;
    public InputField rushHourIN;
    public InputField avrgStayTimeIn;
    public InputField standartDeviationIn;  // standard deviation for leave time/spawn rate
    public InputField redirChance;
    public Button apply;
    public InputField spawnNumberIn;//spawn rate
    public Slider reservedSlider; //reserved spawnrate


    private string groupName = "new Group"; // text to write upon
    private float rushHour = 12.0f; // peak time
    private int avrgStayTime = 1; // average stay time of group
    private float standartDeviation = 1;  // standard deviation for leave time/spawn rate

    private int spawnNumber = 0; // spawn rate
    private int numberAlreadySpawned = 0;

    private float reservedSpawnRate = 0f; // spawn rate for avatars with a reserved space
    private float signRedirectionChance = 0.5f; //chance for the next sign placed to redirect

    

    // Use this for initialization
    void Start()
    {
        //innit vals
        text.text = groupManager.groupName;
        rushHourIN.text = (groupManager.rushHour).ToString();
        avrgStayTimeIn.text = (groupManager.avrgStayTime).ToString() ;
        standartDeviationIn.text = groupManager.standartDeviation.ToString();
        redirChance.text = groupManager.signRedirectionChance.ToString();
        spawnNumberIn.text = groupManager.spawnNumber.ToString();
        if (reservedSlider)
        {
            reservedSlider.value = 0;
        }
        //add listeners
        avrgStayTimeIn.onValueChanged.AddListener((val) => AvrgStayChanged(val));
        rushHourIN.onValueChanged.AddListener((val) => RushHourChanged(val));
        standartDeviationIn.onValueChanged.AddListener((val) => StandartDeviationChanged(val));
        redirChance.onEndEdit.AddListener((val) => RedirChanceChanged(val));
        spawnNumberIn.onValueChanged.AddListener((val) => SpawnNumChanged(val));

        apply.onClick.AddListener(() => applyChanges());

    }

    public void TextChanged(string newText) {
        this.groupName = newText;
    }

    public void RushHourChanged(string newText)
    {
        float rsh = float.Parse(newText);
        this.rushHour = rsh<0||rsh>=24? 0:rsh;
        rushHourIN.text = this.rushHour.ToString();
    }

    public void AvrgStayChanged(string newText)
    {
        this.avrgStayTime = int.Parse(newText);
    }

    public void StandartDeviationChanged(string newText)
    {
        this.standartDeviation = float.Parse(newText);
    }



    public void SpawnNumChanged(string newText)
    {
        this.spawnNumber = int.Parse(newText);
    }

    public void OnReservedSliderValueChange()
    {
        this.reservedSpawnRate = reservedSlider.value;
    }

    public void RedirChanceChanged(string newText)
    {
        this.signRedirectionChance = float.Parse(newText);
    }


    public void applyChanges() {
        groupManager.groupName = this.groupName;
        groupManager.rushHour = this.rushHour;
        groupManager.avrgStayTime = this.avrgStayTime;
        groupManager.standartDeviation = this.standartDeviation;
        groupManager.spawnNumber = this.spawnNumber;
        groupManager.reservedSpawnRate = this.reservedSpawnRate;
        groupManager.signRedirectionChance = this.signRedirectionChance;
        groupManager.setupSpawnersDistribution();
    }
}
