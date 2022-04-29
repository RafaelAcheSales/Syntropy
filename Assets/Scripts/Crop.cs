using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using NJG.PUN;
[RequireComponent(typeof(Collider))]
public class Crop : MonoBehaviourPun
{
    public enum State { Seed, Small, Medium, Ready, Harvested }
    public State currentState;

    [SerializeField] private int baseValue;
    [SerializeField] private int sellValue;
    [SerializeField] private float baseGrowthRate;
    [SerializeField] private float growthRate;
    [SerializeField] private float sizeMultiplier;
    [SerializeField] private Vector3 baseSize;

    private float progress = 0.1f;
    private bool planted = false;
   

    private void Update() {
        if (!planted) return;
        progress += growthRate * Time.deltaTime;
        progress = Mathf.Clamp(progress, 0, 3);
        CheckState();
        Vector3 newScale = Vector3.one * progress * sizeMultiplier + baseSize;
        transform.localScale = newScale;
    }
    private void CheckState() {
        if (progress < 1) currentState = State.Seed;
        if (progress >= 1 && progress < 2) currentState = State.Small;
        if (progress >= 2 && progress < 3) currentState = State.Medium;
        if (progress >= 3) currentState = State.Ready;
    }
    public void Plant(float conditions) {
        planted = true;
        growthRate = baseGrowthRate + conditions;
        
        Debug.Log("Planted");
    }



}
