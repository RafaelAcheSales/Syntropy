using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Crop : MonoBehaviour
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
   

    private void Update() {
        progress += growthRate * Time.deltaTime;
        progress = Mathf.Clamp(progress, 0, 3);
        if (progress < 1) currentState = State.Seed;
        if (progress >= 1 && progress < 2) currentState = State.Small;
        if (progress >= 2 && progress < 3) currentState = State.Medium;
        if (progress >= 3) currentState = State.Ready;
        Vector3 newScale = Vector3.one * progress * sizeMultiplier + baseSize;
        transform.localScale = newScale;
    }

    

}
