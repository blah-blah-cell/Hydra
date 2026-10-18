using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace HydraMenu.modules.visuals
{
	internal class LightImmunity : Module
	{
		public LightImmunity() : base("LightImmunity")
		{
			base.Enabled = true;
		}

		private static LightImmunity Instance
		{
			get { return ModuleManager.lightImmunity; }
		}

		public bool ImpostorVision { get; set; } = false;
		public float CustomVisionMod { get; set; } = 1.0f;

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
		class OnCalculateLightRadius
		{
			static void Postfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
			{
				if(!Instance.Enabled || player == null || PlayerControl.LocalPlayer == null || player.PlayerId != PlayerControl.LocalPlayer.PlayerId) return;

				if(GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) return;

				try
				{
					float crewVision = __instance.MaxLightRadius * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);
					float impVision = __instance.MaxLightRadius * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);

					float targetVision = Instance.ImpostorVision ? Mathf.Max(crewVision, impVision) : crewVision;

					targetVision *= Instance.CustomVisionMod;

					// If electrical sabotage is active or vision dropped below target baseline, preserve visibility
					if(__result < targetVision)
					{
						__result = targetVision;
					}
				}
				catch(System.Exception ex)
				{
					Hydra.Log.LogError($"[LightImmunity] Error calculating light radius: {ex.Message}");
				}
			}
		}
	}
}
