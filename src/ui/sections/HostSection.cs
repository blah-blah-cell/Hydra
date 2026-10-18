using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HydraMenu.modules;
using HydraMenu.modules.host;
using HydraMenu.network;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HydraMenu.ui.sections
{
	internal class HostSection : Section
	{
		public HostSection() : base("Host") { }

		private byte selectedMap = 0;
		private Controls.PlayerColors selectedColor = 0;
		private int selectedRolePlayerIndex = 0;
		private RoleTypes selectedPlayerRole = RoleTypes.Impostor;

		private float hostCustomSpeed = 1.0f;
		private float hostCustomKillCooldown = 30.0f;
		private int hostCustomDiscussionTime = 15;
		private int hostCustomVotingTime = 60;

		public static readonly Queue<InnerNetObject> lobbyList = new Queue<InnerNetObject>();
		public static readonly Queue<InnerNetObject> shipList = new Queue<InnerNetObject>();

		public override void Render()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				GUILayout.Label("You are not currently in a game, these options will not work.");
			}
			else if(!AmongUsClient.Instance.AmHost)
			{
				GUILayout.Label("You are not the host of the current lobby. Using these options will either do nothing or get you banned by the anticheat");
			}

			ModuleManager.banMidGame.Enabled = GUILayout.Toggle(ModuleManager.banMidGame.Enabled, "Be able to ban players mid-game");

			ModuleManager.flipSkeld.Enabled = GUILayout.Toggle(ModuleManager.flipSkeld.Enabled, "Use Flipped Skeld Map");

			ModuleManager.disableGameEnd.Enabled = GUILayout.Toggle(ModuleManager.disableGameEnd.Enabled, "Disable Game End");
			ModuleManager.disableVentClean.Enabled = GUILayout.Toggle(ModuleManager.disableVentClean.Enabled, "Disable Vent Clean");

			ModuleManager.fakeShapeshiftBubble.Enabled = GUILayout.Toggle(ModuleManager.fakeShapeshiftBubble.Enabled, "Fake Shapeshift Bubble");

			GUILayout.BeginHorizontal();
			ModuleManager.blockLowLevels.Enabled = GUILayout.Toggle(ModuleManager.blockLowLevels.Enabled, $"Kick players with less than {ModuleManager.blockLowLevels.MinLevel} levels");
			ModuleManager.blockLowLevels.MinLevel = (uint)GUILayout.HorizontalSlider(ModuleManager.blockLowLevels.MinLevel, 0, 100);
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Force Start Game"))
			{
				ForceStartGame();
			}

			if(GUILayout.Button("Kill Everyone"))
			{
				KillAllPlayers();
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Force Crewmate Victory"))
			{
				// Just in case the user has this enabled
				ModuleManager.disableGameEnd.Enabled = false;

				GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
				Hydra.notifications.Send("Game Finished", "You ended the game with a crewmate victory.", 5);
			}

			if(GUILayout.Button("Force Imposter Victory"))
			{
				// Just in case the user has this enabled
				ModuleManager.disableGameEnd.Enabled = false;

				GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
				Hydra.notifications.Send("Game Finished", "You ended the game with an imposter victory.", 5);
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Temp Ban All Players"))
			{
				ModuleManager.tempBanAll.Enabled = true;
			}

			GUILayout.Space(5);
			GUILayout.Label("Map Spawner:");

			GUILayout.Label($"Selected map: {(MapNames)selectedMap}");
			selectedMap = (byte)GUILayout.HorizontalSlider(selectedMap, 0, 5);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Despawn Map"))
			{
				DespawnMap();
			}

			if(GUILayout.Button("Spawn Map"))
			{
				AmongUsClient.Instance.StartCoroutine(SpawnMap(selectedMap).WrapToIl2Cpp());
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Despawn Lobby"))
			{
				DespawnLobby();
			}

			if(GUILayout.Button("Spawn Lobby"))
			{
				SpawnLobby();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Host Role Manager & Live Assignment:");
			ModuleManager.assignRoles.Enabled = GUILayout.Toggle(ModuleManager.assignRoles.Enabled, "Enable Custom Role Enforcer (Auto-applies at round start)");

			if(PlayerControl.AllPlayerControls != null && PlayerControl.AllPlayerControls.Count > 0)
			{
				if(selectedRolePlayerIndex < 0 || selectedRolePlayerIndex >= PlayerControl.AllPlayerControls.Count)
				{
					selectedRolePlayerIndex = 0;
				}

				PlayerControl currentTarget = PlayerControl.AllPlayerControls[selectedRolePlayerIndex];

				GUILayout.Label("Lobby Players (Click to select target):");
				// Draw player selection buttons in rows of 2
				for(int i = 0; i < PlayerControl.AllPlayerControls.Count; i += 2)
				{
					GUILayout.BeginHorizontal();
					for(int c = 0; c < 2 && (i + c) < PlayerControl.AllPlayerControls.Count; c++)
					{
						int idx = i + c;
						PlayerControl p = PlayerControl.AllPlayerControls[idx];
						if(p == null || p.Data == null) continue;

						bool isSelected = (selectedRolePlayerIndex == idx);
						string queuedTag = "";
						if(ModuleManager.assignRoles.CustomPlayerRoles.TryGetValue(p.PlayerId, out RoleTypes qr))
						{
							queuedTag = $" -> [{qr}]";
						}

						string btnLabel = $"{(isSelected ? "[*] " : "")}{p.Data.PlayerName} ({(p == PlayerControl.LocalPlayer ? "Self" : p.Data.RoleType.ToString())}){queuedTag}";
						
						GUI.color = isSelected ? Color.cyan : (queuedTag != "" ? Color.yellow : Color.white);
						if(GUILayout.Button(btnLabel, GUILayout.ExpandWidth(true)))
						{
							selectedRolePlayerIndex = idx;
						}
					}
					GUILayout.EndHorizontal();
				}
				GUI.color = Color.white;

				string targetDisplayName = (currentTarget == PlayerControl.LocalPlayer)
					? $"{currentTarget.Data?.PlayerName ?? "Host"} (Self)"
					: $"{currentTarget.Data?.PlayerName ?? "Player"}";

				GUILayout.Space(4);
				GUILayout.Label($"Selected Target: {targetDisplayName} | Role to Assign: {selectedPlayerRole}");

				// Quick Role Grid (No clumsy slider!)
				RoleTypes[] quickRoles = new RoleTypes[]
				{
					RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Shapeshifter, RoleTypes.Engineer,
					RoleTypes.Scientist, RoleTypes.Phantom, RoleTypes.Tracker, RoleTypes.Noisemaker,
					RoleTypes.Detective, RoleTypes.Viper, RoleTypes.Judge, RoleTypes.GuardianAngel
				};

				for(int r = 0; r < quickRoles.Length; r += 4)
				{
					GUILayout.BeginHorizontal();
					for(int c = 0; c < 4 && (r + c) < quickRoles.Length; c++)
					{
						RoleTypes role = quickRoles[r + c];
						bool isChosen = (selectedPlayerRole == role);
						GUI.color = isChosen ? Color.green : Color.white;
						if(GUILayout.Button(role.ToString()))
						{
							selectedPlayerRole = role;
						}
					}
					GUILayout.EndHorizontal();
				}
				GUI.color = Color.white;

				GUILayout.BeginHorizontal();
				if(GUILayout.Button("Queue for Next Round"))
				{
					if(currentTarget == PlayerControl.LocalPlayer)
					{
						ModuleManager.assignRoles.AssignedRole = selectedPlayerRole;
					}
					ModuleManager.assignRoles.SetPlayerRole(currentTarget.PlayerId, selectedPlayerRole);
					Hydra.notifications.Send("Role Queued", $"Queued {selectedPlayerRole} for {currentTarget.Data?.PlayerName ?? "player"} next round.", 4);
				}

				if(GUILayout.Button("Apply Live Now (In-Game)"))
				{
					AssignRoles.ApplyRole(currentTarget, selectedPlayerRole);
					Hydra.notifications.Send("Role Applied", $"Changed {currentTarget.Data?.PlayerName ?? "player"}'s role to {selectedPlayerRole}.", 4);
				}

				if(GUILayout.Button("Clear for Target"))
				{
					ModuleManager.assignRoles.RemovePlayerRole(currentTarget.PlayerId);
					Hydra.notifications.Send("Cleared", $"Reset custom role for {currentTarget.Data?.PlayerName ?? "player"}.", 3);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(3);
				GUILayout.Label("Lobby Presets & Batch Controls:");
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("Everyone Crewmate"))
				{
					foreach(PlayerControl pc in PlayerControl.AllPlayerControls)
					{
						if(pc != null && pc.Data != null)
						{
							ModuleManager.assignRoles.SetPlayerRole(pc.PlayerId, RoleTypes.Crewmate);
							if(AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
							{
								AssignRoles.ApplyRole(pc, RoleTypes.Crewmate);
							}
						}
					}
					Hydra.notifications.Send("Preset Applied", "All players set to Crewmate.", 3);
				}

				if(GUILayout.Button("Host Impostor / Others Crew"))
				{
					foreach(PlayerControl pc in PlayerControl.AllPlayerControls)
					{
						if(pc != null && pc.Data != null)
						{
							RoleTypes r = (pc == PlayerControl.LocalPlayer) ? RoleTypes.Impostor : RoleTypes.Crewmate;
							ModuleManager.assignRoles.SetPlayerRole(pc.PlayerId, r);
							if(AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
							{
								AssignRoles.ApplyRole(pc, r);
							}
						}
					}
					Hydra.notifications.Send("Preset Applied", "Host is Impostor, all other players Crewmates.", 3);
				}

				if(GUILayout.Button("Clear All Queues"))
				{
					ModuleManager.assignRoles.ClearCustomRoles();
					Hydra.notifications.Send("Roles Cleared", "Cleared all custom role assignments.", 3);
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Label($"Role to assign (Self): {ModuleManager.assignRoles.AssignedRole}");
				ModuleManager.assignRoles.AssignedRole = Controls.HorizontalRoleSlider(ModuleManager.assignRoles.AssignedRole);
			}

			GUILayout.Space(5);
			GUILayout.Label("Meeting Controls:");
			ModuleManager.disableMeetings.Enabled = GUILayout.Toggle(ModuleManager.disableMeetings.Enabled, "Disable Meetings");
			Hydra.routines.reportBodySpam.Enabled = GUILayout.Toggle(Hydra.routines.reportBodySpam.Enabled, "Spam Report Bodies");
			ModuleManager.voteImmune.Enabled = Controls.PlayerSpecificToggle("Vote Immune", PlayerControl.LocalPlayer, ModuleManager.voteImmune.targets);
			Hydra.routines.voteSpammer.Enabled = Controls.GlobalPlayerSpecificToggle("Spam Votes", Hydra.routines.voteSpammer.targets);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Close Meeting"))
			{
				CloseMeeting();
			}

			if(GUILayout.Button("Force Skip Vote (Tie Meeting)"))
			{
				ForceSkipMeeting();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Shapeshift Controls:");
			if(GUILayout.Button("Shapeshift Everyone Into Me"))
			{
				AmongUsClient.Instance.StartCoroutine(ShapeshiftAll(PlayerControl.LocalPlayer).WrapToIl2Cpp());
			}

			if(GUILayout.Button("Shapeshift Everyone Into Random"))
			{
				PlayerControl target = Utilities.GetRandomPlayer(false, false, false, false);
				AmongUsClient.Instance.StartCoroutine(ShapeshiftAll(target).WrapToIl2Cpp());
			}

			if(GUILayout.Button("Revert All Shapeshifts"))
			{
				AmongUsClient.Instance.StartCoroutine(RevertAllShapeshift().WrapToIl2Cpp());
			}

			GUILayout.Space(5);
			GUILayout.Label("Color Controls:");

			GUILayout.Label($"Change everyone's color to: {selectedColor}");
			selectedColor = Controls.HorizontalColorSlider(selectedColor);

			if(GUILayout.Button("Change Colors"))
			{
				BatchedMessage batch = new BatchedMessage();

				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					batch.QueueSetColor(player, (byte)selectedColor);
				}

				batch.FinishBatch();
			}

			Hydra.routines.discoHost.Enabled = Controls.GlobalPlayerSpecificToggle("Disco Party", Hydra.routines.discoHost.targets);

			GUILayout.Label($"Color randomization delay: {Hydra.routines.discoHost.RandomizationDelay:F2}s");
			Hydra.routines.discoHost.RandomizationDelay = GUILayout.HorizontalSlider(Hydra.routines.discoHost.RandomizationDelay, 0.1f, 2.0f);

			GUILayout.Space(5);
			GUILayout.Label("Live Lobby Settings Modifier (Host Wire Sync):");
			GUILayout.Label($"Player Speed: {hostCustomSpeed:F2}x");
			hostCustomSpeed = GUILayout.HorizontalSlider(hostCustomSpeed, 0.5f, 3.0f);

			GUILayout.Label($"Kill Cooldown: {hostCustomKillCooldown:F0}s");
			hostCustomKillCooldown = GUILayout.HorizontalSlider(hostCustomKillCooldown, 10f, 60f);

			GUILayout.Label($"Discussion Time: {hostCustomDiscussionTime}s");
			hostCustomDiscussionTime = (int)GUILayout.HorizontalSlider(hostCustomDiscussionTime, 0f, 120f);

			GUILayout.Label($"Voting Time: {hostCustomVotingTime}s");
			hostCustomVotingTime = (int)GUILayout.HorizontalSlider(hostCustomVotingTime, 15f, 120f);

			if(GUILayout.Button("Sync Settings to Lobby (Over Wire)"))
			{
				SyncLiveGameOptions();
			}

			GUILayout.Space(5);
			GUILayout.Label("Mass Teleportation (Host Authority):");
			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Teleport All to Me"))
			{
				if(PlayerControl.LocalPlayer != null)
				{
					Teleporter.TeleportAllTo(PlayerControl.LocalPlayer.transform.position);
					Hydra.notifications.Send("Teleporter", "Teleported all players to your position.", 3);
				}
			}

			if(GUILayout.Button("Teleport All to Cafeteria"))
			{
				Vector2 targetPos = (ShipStatus.Instance != null) ? ShipStatus.Instance.InitialSpawnCenter : Vector2.zero;
				Teleporter.TeleportAllTo(targetPos);
				Hydra.notifications.Send("Teleporter", "Teleported all players to Cafeteria spawn.", 3);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Ship Cleanup:");
			if(GUILayout.Button("Despawn All Dead Bodies"))
			{
				CleanAllDeadBodies();
			}
		}

		private void ForceStartGame()
		{
			// Local lobbies are the only lobbies where we can start the game without host
			if(AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Start Game", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			// The vanilla anticheat prevents players from sending a ClientReady message more than once
			// If we attempt to start the game twice, then all players will send another ClientReady message, and the entire lobby will be kicked
			if(AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && Utilities.IsAnticheatPresent())
			{
				Hydra.notifications.Send("Start Game", "The game has already been started.");
				return;
			}

			AmongUsClient.Instance.StartGame();

			// PlayerControl::RpcSetRole has checks against playing the intro cutscene in Freeplay
			// To avoid a blackscreen in Freeplay, we force the intro cutscene to start
			if(AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
			{
				HudManager.Instance.StartCoroutine(HudManager.Instance.CoShowIntro());
			}
		}

		private static void KillAllPlayers()
		{
			bool hasAnticheat = Utilities.IsAnticheatPresent();

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Hydra.notifications.Send("Murder Player", "This feature can only be used once the game has started.");
				return;
			}

			if(hasAnticheat && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Murder Player", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			// On +25 modded protocol lobbies, we are able to send SetColor RPCs as non-host
			// however we are still affected by message packing limits
			int packingLimit = AmongUsClient.Instance.GetMaxMessagePackingLimit();
			BatchedMessage batch = new BatchedMessage();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(batch.msgCount >= packingLimit)
				{
					batch.FinishBatch();
					batch = new BatchedMessage();
				}

				batch.QueueMurderPlayer(PlayerControl.LocalPlayer, player, MurderResultFlags.Succeeded);
			}

			batch.FinishBatch();
		}

		private static void SpawnLobby()
		{
			Hydra.Log.LogInfo($"Attempting to spawn in lobby");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Spawn Lobby", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			InnerNetObject lobbyPrefab = AmongUsClient.Instance.NonAddressableSpawnableObjects.First((obj) => obj.SpawnId == (uint)SpawnType.LobbyBehavior);
			if(lobbyPrefab == null)
			{
				Hydra.Log.LogError($"Failed to find LobbyBehavior prefab in NonAddressableSpawnableObjects");
				return;
			}

			LobbyBehaviour lobby = UnityEngine.Object.Instantiate(lobbyPrefab).Cast<LobbyBehaviour>();

			BatchedMessage batch = new BatchedMessage();
			batch.QueueSpawn(lobby, -2, SpawnFlags.None);
			batch.FinishBatch();

			Hydra.notifications.Send("Spawn Lobby", "A new instance of the lobby has been spawned.", 5);
		}

		private static void DespawnLobby()
		{
			Hydra.Log.LogInfo($"Attempting to despawn lobby");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Despawn Lobby", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(lobbyList.Count == 0)
			{
				Hydra.notifications.Send("Despawn Lobby", "The lobby map has already been despawned.", 10);
				return;
			}

			InnerNetObject lobby = lobbyList.Dequeue();
			lobby.Despawn();

			Hydra.notifications.Send("Despawn Lobby", "The lobby map has been despawned.", 5);
		}

		public static IEnumerator SpawnMap(byte mapId)
		{
			Hydra.Log.LogInfo($"Attempting to spawn in map id {mapId}");

			// The Utilities::IsAnticheatPresent function does not work perfectly here
			// We are able to spawn in any maps we want without host in local, or even skeld.net lobbies
			// however +25 modded protocol lobbies, while having much of their anticheat checks disabled, still has checks against non-hosts sending spawn messages
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Spawn Map", "This feature can only be used if you are the host of the lobby.");
				yield break;
			}

			AsyncOperationHandle<GameObject> asyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync(null, false);
			yield return asyncHandle;

			ShipStatus ship = asyncHandle.Result.GetComponent<ShipStatus>();

			BatchedMessage batch = new BatchedMessage();
			batch.QueueSpawn(ship, -2, SpawnFlags.None);
			batch.FinishBatch();

			Hydra.notifications.Send("Spawn Map", $"{(MapNames)mapId} has been spawned.", 5);
		}

		private static void DespawnMap()
		{
			Hydra.Log.LogInfo($"Attempting to despawn map");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Despawn Map", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(shipList.Count == 0)
			{
				Hydra.notifications.Send("Despawn Map", "The current map has already been despawned.", 10);
				return;
			}

			InnerNetObject ship = shipList.Dequeue();
			ship.Despawn();

			Hydra.notifications.Send("Despawn Map", "The current map has been despawned.", 5);
		}

		private void CloseMeeting()
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Close Meeting", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(MeetingHud.Instance == null)
			{
				Hydra.notifications.Send("Close Meeting", "This option can only be during a meeting.");
				return;
			}

			BatchedMessage batch = new BatchedMessage();
			batch.QueueVotingComplete(Array.Empty<MeetingHud.VoterState>(), null, false, false, 0);
			batch.QueueCloseMeeting();
			batch.FinishBatch();
		}

		private static IEnumerator ShapeshiftAll(PlayerControl target)
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Shapeshift Player", "You need to be the host of the lobby in order to use this feature.");
				yield break;
			}

			PlayerControl[] allPlayers = PlayerControl.AllPlayerControls.ToArray();
			foreach(PlayerControl player in allPlayers)
			{
				if(player == target || player.shapeshiftTargetPlayerId == target.PlayerId) continue;

				Utilities.ShapeshiftPlayer(player, target);

				// This function can send up to 42 reliable messages at once, so we need to implement a delay to avoid getting disconnected
				yield return Effects.Wait(0.05f);
			}
		}

		private static IEnumerator RevertAllShapeshift()
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Shapeshift Player", "You need to be the host of the lobby in order to use this feature.");
				yield break;
			}

			PlayerControl[] allPlayers = PlayerControl.AllPlayerControls.ToArray();
			foreach(PlayerControl player in allPlayers)
			{
				if(player.shapeshiftTargetPlayerId == -1) continue;

				Utilities.ShapeshiftPlayer(player, player);

				yield return Effects.Wait(0.05f);
			}
		}

		private void SyncLiveGameOptions()
		{
			if(!AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Game Options", "Must be the lobby host to modify options over wire.", 4);
				return;
			}

			if(GameManager.Instance == null || GameManager.Instance.LogicOptions == null || GameManager.Instance.LogicOptions.currentGameOptions == null)
			{
				Hydra.notifications.Send("Game Options", "Options not initialized yet.", 3);
				return;
			}

			try
			{
				IGameOptions options = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				options.SetFloat(FloatOptionNames.PlayerSpeedMod, hostCustomSpeed);
				options.SetFloat(FloatOptionNames.KillCooldown, hostCustomKillCooldown);
				options.SetInt(Int32OptionNames.DiscussionTime, hostCustomDiscussionTime);
				options.SetInt(Int32OptionNames.VotingTime, hostCustomVotingTime);

				GameManager.Instance.LogicOptions.SetGameOptions(options);
				GameOptions.SendGameOptionsToClient(options, (int)OwnerIds.Everyone);
				Hydra.notifications.Send("Game Options", "Synchronized updated game settings to all players!", 4);
			}
			catch(Exception ex)
			{
				Hydra.Log.LogError($"[HostSection] Error syncing options: {ex.Message}");
			}
		}

		private void ForceSkipMeeting()
		{
			if(!AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Close Meeting", "This feature can only be used if you are the host of the lobby.", 4);
				return;
			}

			if(MeetingHud.Instance == null)
			{
				Hydra.notifications.Send("Close Meeting", "This option can only be used during a meeting.", 4);
				return;
			}

			BatchedMessage batch = new BatchedMessage();
			batch.QueueVotingComplete(Array.Empty<MeetingHud.VoterState>(), null, true, false, 0);
			batch.QueueCloseMeeting();
			batch.FinishBatch();
			Hydra.notifications.Send("Meeting", "Forced meeting conclusion with a tie.", 4);
		}

		private static void CleanAllDeadBodies()
		{
			DeadBody[] bodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
			int count = bodies.Length;
			foreach(DeadBody body in bodies)
			{
				if(body != null && body.gameObject != null)
				{
					UnityEngine.Object.Destroy(body.gameObject);
				}
			}
			Hydra.notifications.Send("Ship Cleanup", $"Despawned {count} dead bodies from the ship.", 4);
		}
	}
}