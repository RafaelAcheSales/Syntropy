namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class IsPlayerInsideCondition : ICondition
	{
		public TargetCharacter character;
        public Collider isInsideWhat;

		public override bool Check(GameObject target)
		{
			return isInsideWhat.bounds.Intersects(character.GetCharacter(target).GetComponent<Collider>().bounds);
		}
        
		#if UNITY_EDITOR
        public static new string NAME = "Custom/IsPlayerInsideCondition";
		#endif
	}
}
