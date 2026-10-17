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
			string authorityStatus = AmongUsClient.Instance.AmHost
				? "<color=green>[Host Mode: Wire Sync Active]</color>"
				: (!Utilities.IsAnticheatPresent()
					? "<color=cyan>[Modded/Local Lobby: Wire Sync Active]</color>"
					: "<color=yellow>[Vanilla Server: Client-Side Capability Emulation]</color>");

			GUILayout.Label($"Role Authority Status: {authorityStatus}");

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
			GUILayout.Label($"Role Selection (Current Target: {selectedRole}):");

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

			string applyBtnLabel = "Apply Role " + (AmongUsClient.Instance.AmHost || !Utilities.IsAnticheatPresent() ? "(Wire Sync)" : "(Client Emulation)");
			if(GUILayout.Button(applyBtnLabel))
			{
				UpdateRole(selectedRole);
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Unlock All Role Superpowers (Kill + Vent + Sabotage)"))
			{
				UnlockAllSuperpowers();
			}
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

			// If Host OR if in modded/host-authoritative/private lobby without anticheat, broadcast over wire!
			if(AmongUsClient.Instance.AmHost || !Utilities.IsAnticheatPresent())
			{
				Hydra.Log.LogInfo("Broadcasting SetRole RPC over the wire");
				if(PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.RpcSetRole(role, true);
				}
				Hydra.notifications.Send("Update Role", $"Role {role} broadcast to lobby over the wire.", 4);
			}
			else
			{
				Hydra.Log.LogInfo("Vanilla server detected: unlocking capabilities client-side");
				ApplyRoleCapabilities(role);
				Hydra.notifications.Send("Update Role", $"Role {role} emulated client-side with full abilities unlocked.", 4);
			}
		}

		public static void ApplyRoleCapabilities(RoleTypes role)
		{
			bool isImpostor = RoleManager.IsImpostorRole(role);

			if(isImpostor || role == RoleTypes.Shapeshifter || role == RoleTypes.Phantom || role == RoleTypes.Viper)
			{
				if(HudManager.Instance != null && HudManager.Instance.KillButton != null)
				{
					HudManager.Instance.KillButton.gameObject.SetActive(true);
				}
				ModuleManager.noKillChecks.Enabled = true;
				ModuleManager.ventAsCrewmate.Enabled = true;
				ModuleManager.moveInVents.Enabled = true;
				ModuleManager.unlockSabotageButton.SabotageAsCrewmate = true;
			}
			else if(role == RoleTypes.Engineer)
			{
				ModuleManager.ventAsCrewmate.Enabled = true;
				ModuleManager.moveInVents.Enabled = true;
			}

			if(HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null && ModuleManager.ventAsCrewmate.Enabled)
			{
				HudManager.Instance.ImpostorVentButton.gameObject.SetActive(true);
			}

			if(HudManager.Instance != null && HudManager.Instance.SabotageButton != null && ModuleManager.unlockSabotageButton.SabotageAsCrewmate)
			{
				HudManager.Instance.SabotageButton.gameObject.SetActive(true);
			}
		}

		public static void UnlockAllSuperpowers()
		{
			ModuleManager.noKillChecks.Enabled = true;
			ModuleManager.noKillChecks.KillOtherImpostors = true;
			ModuleManager.ventAsCrewmate.Enabled = true;
			ModuleManager.moveInVents.Enabled = true;
			ModuleManager.unlockSabotageButton.SabotageAsCrewmate = true;
			ModuleManager.unlockSabotageButton.SabotageInVents = true;
			ModuleManager.noSabotageCooldown.Enabled = true;

			if(HudManager.Instance != null)
			{
				if(HudManager.Instance.KillButton != null) HudManager.Instance.KillButton.gameObject.SetActive(true);
				if(HudManager.Instance.ImpostorVentButton != null) HudManager.Instance.ImpostorVentButton.gameObject.SetActive(true);
				if(HudManager.Instance.SabotageButton != null) HudManager.Instance.SabotageButton.gameObject.SetActive(true);
			}

			Hydra.notifications.Send("Superpowers Unlocked", "Kill, Vent, and Sabotage controls are now fully active!", 4);
		}
	}
}