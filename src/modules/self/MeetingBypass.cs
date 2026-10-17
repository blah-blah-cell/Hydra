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
		public bool AutoDismissUI { get; set; } = false;

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
				if(!Instance.Enabled) return;

				if(Instance.WalkDuringMeetings && PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}

				if(Instance.AutoDismissUI && __instance != null)
				{
					DismissLocalMeeting();
				}
			}
		}

		public static void DismissLocalMeeting()
		{
			if(MeetingHud.Instance == null) return;

			try
			{
				MeetingHud.Instance.Close();
				if(PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.moveable = true;
				}
				Hydra.notifications.Send("Meeting Bypass", "Meeting UI dismissed locally. You are free to move.", 3);
			}
			catch(Exception ex)
			{
				Hydra.Log.LogError($"[MeetingBypass] Error dismissing local meeting: {ex.Message}");
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
					DismissLocalMeeting();
					Hydra.notifications.Send("Meeting Bypass", "Vanilla server detected: dismissed locally only.", 4);
				}
			}
			catch(Exception ex)
			{
				Hydra.Log.LogError($"[MeetingBypass] Error ending meeting: {ex.Message}");
			}
		}
	}
}
