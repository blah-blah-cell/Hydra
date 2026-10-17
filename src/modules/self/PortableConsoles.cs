using UnityEngine;

namespace HydraMenu.modules.self
{
	internal class PortableConsoles : Module
	{
		public PortableConsoles() : base("PortableConsoles") { }

		private static PortableConsoles Instance
		{
			get { return ModuleManager.portableConsoles; }
		}

		public static void ToggleAdminMap()
		{
			if(HudManager.Instance == null)
			{
				Hydra.notifications.Send("Portable Consoles", "Not currently in-game.", 3);
				return;
			}

			try
			{
				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.CountOverlay });
			}
			catch(System.Exception ex)
			{
				Hydra.Log.LogError($"[PortableConsoles] Error toggling Admin Map: {ex.Message}");
			}
		}

		public static void ToggleSabotageMap()
		{
			if(HudManager.Instance == null)
			{
				Hydra.notifications.Send("Portable Consoles", "Not currently in-game.", 3);
				return;
			}

			try
			{
				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Sabotage });
			}
			catch(System.Exception ex)
			{
				Hydra.Log.LogError($"[PortableConsoles] Error toggling Sabotage Map: {ex.Message}");
			}
		}

		public static void ToggleNormalMap()
		{
			if(HudManager.Instance == null)
			{
				Hydra.notifications.Send("Portable Consoles", "Not currently in-game.", 3);
				return;
			}

			try
			{
				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Normal });
			}
			catch(System.Exception ex)
			{
				Hydra.Log.LogError($"[PortableConsoles] Error toggling Map: {ex.Message}");
			}
		}
	}
}
