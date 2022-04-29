using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using GameCreator.Characters;

[RequireComponent(typeof(PhotonView), typeof(Character)), DisallowMultipleComponent]
public class Farmer : MonoBehaviourPunCallbacks 
{
    [PunRPC]
    public void RPC_Plant(int viewID)
    {
        Debug.Log("RPC_Plant recieved");
        GameObject toBePlanted = PhotonView.Find(viewID).gameObject;
        Debug.Log("Plant is " + toBePlanted.name + " with viewID " + viewID);
        if (toBePlanted == null) return;
        toBePlanted.GetComponent<Crop>().Plant(0f);
    }
}
