using AmongUs.GameOptions;
using HydraMenu.modules;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class RolesSection : Section
	{
		public RolesSection() : base("Roles") { }

		private RoleTypes selectedRole = RoleTypes.Crewmate;

		private static readonly RoleTypes[] quickRoles = new RoleTypes[]
		{
			RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Shapeshifter, RoleTypes.Engineer,
			RoleTypes.Scientist, RoleTypes.Phantom, RoleTypes.Tracker, RoleTypes.Noisemaker,
			RoleTypes.Detective, RoleTypes.Viper, RoleTypes.Judge, RoleTypes.GuardianAngel
		};

		public override void Render()
		{
			ModuleManager.ventAsCrewmate.Enabled = GUILayout.Toggle(ModuleManager.ventAsCrewmate.Enabled, "Vent As Crewmate");
			ModuleManager.moveInVents.Enabled = GUILayout.Toggle(ModuleManager.moveInVents.Enabled, "Move In Vents");

			ModuleManager.unlockSabotageButton.SabotageAsCrewmate = GUILayout.Toggle(ModuleManager.unlockSabotageButton.SabotageAsCrewmate, "Sabotage As Crewmate");
			ModuleManager.unlockSabotageButton.SabotageInVents = GUILayout.Toggle(ModuleManager.unlockSabotageButton.SabotageInVents, "Allow Sabotaging In Vents As Imposter");
			ModuleManager.noSabotageCooldown.Enabled = GUILayout.Toggle(ModuleManager.noSabotageCooldown.Enabled, "No Sabotage Cooldown");

			ModuleManager.noShapeshiftAnimation.Enabled = GUILayout.Toggle(ModuleManager.noShapeshiftAnimation.Enabled, "Disable Shapeshift Animation");

			GUILayout.Space(5);
			GUILayout.Label("No Kill Checks:");
			ModuleManager.noKillChecks.Enabled = GUILayout.Toggle(ModuleManager.noKillChecks.Enabled, "Enabled");
			ModuleManager.noKillChecks.KillOtherImpostors = GUILayout.Toggle(ModuleManager.noKillChecks.KillOtherImpostors, "Kill Other Impostors");
			ModuleManager.noKillChecks.KillAsPhantom = GUILayout.Toggle(ModuleManager.noKillChecks.KillAsPhantom, "Kill While Vanished");
			ModuleManager.noKillChecks.NoKillCooldown = GUILayout.Toggle(ModuleManager.noKillChecks.NoKillCooldown, "No Kill Cooldown (Host-only)");
			ModuleManager.noKillChecks.KillGhosts = GUILayout.Toggle(ModuleManager.noKillChecks.KillGhosts, "Kill Ghosts (Host-only)");

			GUILayout.Space(5);
			GUILayout.Label($"Role Selection (Target: {selectedRole}):");

			// Quick Role Grid (Rows of 4)
			for(int r = 0; r < quickRoles.Length; r += 4)
			{
				GUILayout.BeginHorizontal();
				for(int c = 0; c < 4 && (r + c) < quickRoles.Length; c++)
				{
					RoleTypes role = quickRoles[r + c];
					bool isChosen = (selectedRole == role);
					GUI.color = isChosen ? Color.green : Color.white;
					if(GUILayout.Button(role.ToString()))
					{
						selectedRole = role;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUI.color = Color.white;

			GUILayout.Space(4);
			GUILayout.BeginHorizontal();
			selectedRole = Controls.HorizontalRoleSlider(selectedRole);

			if(GUILayout.Button("Update Role"))
			{
				UpdateRole(selectedRole);
			}
			GUILayout.EndHorizontal();
		}

		public static void UpdateRole(RoleTypes role)
		{
			Hydra.Log.LogInfo($"Updating role to {role}");

			bool isGhost = RoleManager.IsGhostRole(role);

			if(HudManager.Instance != null && HudManager.Instance.ReportButton != null)
			{
				HudManager.Instance.ReportButton.gameObject.SetActive(!isGhost);
			}

			if(RoleManager.Instance != null && PlayerControl.LocalPlayer != null)
			{
				RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, role);
			}

			if(AmongUsClient.Instance.AmHost)
			{
				Hydra.Log.LogInfo("Since we are host, we can send the SetRole RPC to sync the new role to the server");
				if(PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.RpcSetRole(role, true);
				}
			}

			Hydra.notifications.Send("Update Role", $"Your role has been updated to {role}.");
		}
	}
}