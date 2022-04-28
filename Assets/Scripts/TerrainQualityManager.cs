using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainQualityManager : MonoBehaviour
{
    List<Soil> soils = new List<Soil>();
    
    public void GetSoilsFromChildren()
    {
        foreach (Transform child in transform)
        {
            Soil soil = child.GetComponent<Soil>();
            if (soil != null)
            {
                soils.Add(soil);
            }
        }
    }

    
}
