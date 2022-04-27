namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class Plant : IAction
	{
		public TargetGameObject plant;

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            plant.GetGameObject(target).GetComponent<Crop>()?.Plant(0f);
            return true;
        }

		#if UNITY_EDITOR
        public static new string NAME = "Custom/Plant";
		#endif
	}
}
