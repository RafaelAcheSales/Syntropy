using System;
using System.Collections.Generic;
using GameCreator.Core;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    [AddComponentMenu("")]
    public class RPCNetwork : MonoBehaviour
    {
        private IgniterPhotonRPC[] igniters;
        private Dictionary<string, IgniterPhotonRPC> cache = new Dictionary<string, IgniterPhotonRPC>();

        private void Awake()
        {
            igniters = GetComponentsInChildren<IgniterPhotonRPC>();
        }

        [PunRPC]
        public void RPCHandler(string rpcName, object[] data, PhotonMessageInfo info)
        {
            bool executed = false;
            IgniterPhotonRPC target = null;
            if (cache.TryGetValue(rpcName, out target))
            {
                target.RPCHandler(data, info);
                executed = true;
            }
            else
            {
                foreach (IgniterPhotonRPC igniter in igniters)
                {
                    if (igniter.rpcName.Equals(rpcName))
                    {
                        if(!cache.ContainsKey(rpcName)) cache.Add(rpcName, igniter);
                        igniter.RPCHandler(data, info);
                        executed = true;
                        break;
                    }
                }
            }
            // Debug.LogWarningFormat(gameObject, "RPCHandler rpcName: {0} sender: {1} executed: {2}", rpcName, info.Sender, executed);
        }
    }
}