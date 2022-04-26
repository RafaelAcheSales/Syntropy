namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class PlantState : ICondition
	{
		public Crop crop;
		public Crop.State state;

		public override bool Check(GameObject target)
		{
			return crop.currentState == state;
		}
        
		#if UNITY_EDITOR
        public static new string NAME = "Custom/PlantState";
		#endif
	}
}
