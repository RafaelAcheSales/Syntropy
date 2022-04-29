namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class CollectCrop : IAction
	{
		public TargetGameObject farmer = new TargetGameObject();

		public TargetGameObject crop = new TargetGameObject();

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            
            return true;
        }

		#if UNITY_EDITOR
        public static new string NAME = "Custom/CollectCrop";
		#endif
	}
}
