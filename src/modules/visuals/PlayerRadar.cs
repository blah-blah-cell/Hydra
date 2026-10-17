using AmongUs.GameOptions;
using HydraMenu.ui;
using UnityEngine;

namespace HydraMenu.modules.visuals
{
	internal class PlayerRadar : Module
	{
		public PlayerRadar() : base("PlayerRadar")
		{
			base.Enabled = false;
		}

		private static PlayerRadar Instance
		{
			get { return ModuleManager.playerRadar; }
		}

		public bool ShowDead { get; set; } = true;
		public bool ShowRooms { get; set; } = true;

		public static void RenderRadarOverlay()
		{
			if(Instance == null || !Instance.Enabled) return;
			if(PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) return;

			int playerCount = PlayerControl.AllPlayerControls.Count;
			float width = 300f * MainUI.scale;
			float itemHeight = 18f * MainUI.scale;
			float height = 30f + (playerCount * itemHeight);

			Rect radarRect = new Rect(Screen.width - width - 15f, 15f, width, height);

			GUI.Box(radarRect, $"Player Radar ({playerCount} Players)", Styles.MainBox);

			GUILayout.BeginArea(new Rect(radarRect.x + 8f, radarRect.y + 24f, radarRect.width - 16f, radarRect.height - 28f));

			for(int i = 0; i < playerCount; i++)
			{
				PlayerControl p = PlayerControl.AllPlayerControls[i];
				if(p == null || p.Data == null) continue;

				bool isDead = p.Data.IsDead || p.Data.Disconnected;
				if(isDead && !Instance.ShowDead) continue;

				RoleTypes role = p.Data.RoleType;
				string roleColor = RoleManager.IsImpostorRole(role) ? "#FF5555" : (role == RoleTypes.Engineer ? "#FFAA00" : (role == RoleTypes.Scientist ? "#00FFFF" : (role == RoleTypes.Shapeshifter ? "#FF55FF" : "#55FF55")));
				string statusTag = isDead ? "<color=#888888>[DEAD]</color> " : "";
				string roleTag = $"<color={roleColor}>[{role}]</color>";

				string roomStr = "";
				if(Instance.ShowRooms && !isDead)
				{
					SystemTypes room = Utilities.GetRoomForPlayer(p);
					if(room != (SystemTypes)255)
					{
						roomStr = $" <color=#AAAAAA>({room})</color>";
					}
				}

				string selfTag = (p == PlayerControl.LocalPlayer) ? " <color=#00FFAA>(You)</color>" : "";
				GUILayout.Label($"{statusTag}{roleTag} {p.Data.PlayerName}{selfTag}{roomStr}");
			}

			GUILayout.EndArea();
		}
	}
}
