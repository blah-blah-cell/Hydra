using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	internal class SetRole : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			// When our client is NOT the host, incoming SetRole RPCs originate from the actual lobby host
			// Dropping these corrupts player role initialization and breaks lobby cosmetics/nameplates
			if(!AmongUsClient.Instance.AmHost)
			{
				return true;
			}

			// When OUR client is the host, only we should ever send SetRole
			// Any other client attempting to send SetRole is attempting unauthorized role manipulation
			if(player != PlayerControl.LocalPlayer)
			{
				Anticheat.Flag(player, $"{player.Data?.PlayerName ?? "Player"} attempted to set or modify player roles as non-host.");
				return false;
			}

			return true;
		}

		public override bool IsHostOnly()
		{
			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetRole;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(PlayerControl);
		}
	}
}
