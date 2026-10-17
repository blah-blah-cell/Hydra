using UnityEngine;

namespace HydraMenu.modules.visuals
{
	internal class DeadBodyAlert : Module
	{
		public DeadBodyAlert() : base("DeadBodyAlert")
		{
			base.Enabled = true;
		}

		private static DeadBodyAlert Instance
		{
			get { return ModuleManager.deadBodyAlert; }
		}

		public bool ShowKiller { get; set; } = true;

		private void OnPlayerMurder(PlayerControl murderer, PlayerControl victim, MurderResultFlags flags)
		{
			if(!Instance.Enabled || victim == null) return;

			string victimName = victim.Data?.PlayerName ?? "Crewmate";
			SystemTypes room = Utilities.GetRoomForPosition(victim.transform.position);
			string roomStr = (room != (SystemTypes)255) ? $" in {room}" : "";
			string killerStr = (ShowKiller && murderer != null && murderer.Data != null) ? $" | Killer: {murderer.Data.PlayerName}" : "";

			Hydra.Log.LogInfo($"[DeadBodyAlert] Murder detected: {victimName} killed{roomStr}{killerStr}");
			Hydra.notifications.Send("Body Alert", $"{victimName} was killed{roomStr}{killerStr}!", 6f);
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnPlayerMurder += OnPlayerMurder;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnPlayerMurder -= OnPlayerMurder;
		}
	}
}
