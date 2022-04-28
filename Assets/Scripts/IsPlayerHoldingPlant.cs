namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using Variables;
	[AddComponentMenu("")]
	public class IsPlayerHoldingPlant : ICondition
	{
		public TargetGameObject farmer;

		[Space, VariableFilter(Variable.DataType.GameObject)]
        public VariableProperty assignToVariable = new VariableProperty(
            Variable.VarType.GlobalVariable
        );

		public override bool Check(GameObject target)
		{
			Crop crop = farmer.GetGameObject(target).GetComponentInChildren<Crop>();
			if (crop == null) return false;
			assignToVariable.Set(crop.gameObject, target);
			return true;
		}
        
		#if UNITY_EDITOR
        public static new string NAME = "Custom/IsPlayerHoldingPlant";
		#endif
	}
}
