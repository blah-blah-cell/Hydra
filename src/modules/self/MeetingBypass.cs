using HarmonyLib;
using HydraMenu.network;
using System;
using UnityEngine;

namespace HydraMenu.modules.self
{
	internal class MeetingBypass : Module
	{
		public MeetingBypass() : base("MeetingBypass") { }

		private static MeetingBypass Instance
		{
			get { return ModuleManager.meetingBypass; }
		}

		public bool WalkDuringMeetings { get; set; } = true;
		public static bool IsMeetingHidden { get; private set; } = false;

		[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
		class OnMeetingUpdate
		{
			static void Postfix()
			{
				if(!Instance.Enabled || PlayerControl.LocalPlayer == null) return;

				if(Instance.WalkDuringMeetings)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}
			}
		}

		[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
		class OnMeetingStart
		{
			static void Postfix(MeetingHud __instance)
			{
				IsMeetingHidden = false;
				if(!Instance.Enabled) return;

				if(Instance.WalkDuringMeetings && PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}
			}
		}

		[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
		class OnMeetingClose
		{
			static void Prefix()
			{
				IsMeetingHidden = false;
				if(MeetingHud.Instance != null && MeetingHud.Instance.transform != null)
				{
					MeetingHud.Instance.transform.localScale = Vector3.one;
				}
			}
		}

		public static void ToggleMeetingScreenVisibility()
		{
			if(MeetingHud.Instance == null || MeetingHud.Instance.transform == null)
			{
				Hydra.notifications.Send("Meeting Bypass", "No active meeting to toggle.", 3);
				return;
			}

			try
			{
				IsMeetingHidden = !IsMeetingHidden;
				MeetingHud.Instance.transform.localScale = IsMeetingHidden ? Vector3.zero : Vector3.one;

				if(PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}

				string status = IsMeetingHidden ? "hidden (walkthrough mode)" : "restored";
				Hydra.notifications.Send("Meeting Bypass", $"Meeting screen {status}.", 3);
			}
			catch(Exception ex)
			{
				Hydra.Log.LogError($"[MeetingBypass] Error toggling meeting screen: {ex.Message}");
			}
		}

		public static void ForceEndMeetingWire()
		{
			if(MeetingHud.Instance == null) return;

			try
			{
				if(AmongUsClient.Instance.AmHost || !Utilities.IsAnticheatPresent())
				{
					BatchedMessage batch = new BatchedMessage();
					batch.QueueVotingComplete(Array.Empty<MeetingHud.VoterState>(), null, false, false, 0);
					batch.FinishBatch();
					Hydra.notifications.Send("Meeting Bypass", "Voting completed sent to all players on wire.", 4);
				}
				else
				{
					ToggleMeetingScreenVisibility();
					Hydra.notifications.Send("Meeting Bypass", "Vanilla server: meeting screen hidden locally (safe).", 4);
				}
			}
			catch(Exception ex)
			{
				Hydra.Log.LogError($"[MeetingBypass] Error ending meeting: {ex.Message}");
			}
		}
	}
}
