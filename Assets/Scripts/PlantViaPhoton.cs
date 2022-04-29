namespace GameCreator.Core
{
	using Variables;
	using Characters;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using Photon.Realtime;
    using Photon.Pun;
	using NJG.PUN;

    [AddComponentMenu("")]
	public class PlantViaPhoton : IAction
	{
		[VariableFilter(Variable.DataType.GameObject)]
		public VariableProperty plant = new VariableProperty(
			Variable.VarType.LocalVariable
		);
		public TargetGameObject farmer;

		
        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
			Debug.Log("PlantViaPhoton");
            GameObject toBePlanted = this.plant.Get(target) as GameObject;
			if (toBePlanted == null) return false;
			//sends photonview id via rpc

			PhotonView photonView = toBePlanted.GetComponent<PhotonView>();

			Debug.Log("Plant is " + toBePlanted.name + " with viewID " + photonView.ViewID);
			if (photonView == null) return false;
			PhotonView farmerPhotonView = farmer.GetGameObject(target).GetComponent<PhotonView>();
			farmerPhotonView.RPC("RPC_Plant", RpcTarget.All, photonView.ViewID);
			Debug.Log("FARMER PHOTONVIEW ID: " + farmerPhotonView.ViewID + " PLANT PHOTONVIEW ID: " + photonView.ViewID);
			return true;
        }



		#if UNITY_EDITOR
        public static new string NAME = "Custom/PlantViaPhoton";
		#endif
	}
}
