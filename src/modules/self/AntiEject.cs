using HarmonyLib;
using System;

namespace HydraMenu.modules.self
{
	internal class AntiEject : Module
	{
		public AntiEject() : base("AntiEject") { }

		private static AntiEject Instance
		{
			get { return ModuleManager.antiEject; }
		}

		[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
		class OnVotingComplete
		{
			static void Prefix(ref MeetingHud.VoterState[] states, ref NetworkedPlayerInfo exiled, ref bool tie)
			{
				if(!Instance.Enabled || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

				if(exiled != null && exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
				{
					Hydra.Log.LogInfo("[AntiEject] Local player was voted out. Forcing tie and nullifying exile target.");
					exiled = null;
					tie = true;
					Hydra.notifications.Send("Anti-Eject", "Intercepted vote outcome! You survived being voted out.", 5);
				}
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
		class OnExiled
		{
			static bool Prefix(PlayerControl __instance)
			{
				if(!Instance.Enabled || PlayerControl.LocalPlayer == null) return true;

				if(__instance == PlayerControl.LocalPlayer)
				{
					Hydra.Log.LogInfo("[AntiEject] Blocked PlayerControl.Exiled execution for LocalPlayer.");
					return false;
				}

				return true;
			}
		}
	}
}
