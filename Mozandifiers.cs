using System.IO;
using Mozandifiers.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers
{
	public class Mozandifiers : Mod
	{
		private enum MessageType : byte
		{
			SyncShiftingState
		}

		internal void SendShiftingState(Player player)
		{
			if (Main.netMode != NetmodeID.Server || player == null || !player.active) {
				return;
			}

			ModPacket packet = GetPacket();
			packet.Write((byte)MessageType.SyncShiftingState);
			packet.Write((byte)player.whoAmI);
			player.GetModPlayer<ShiftingPlayer>().WriteSync(packet);
			packet.Send(player.whoAmI);
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			MessageType messageType = (MessageType)reader.ReadByte();
			switch (messageType) {
				case MessageType.SyncShiftingState:
					int playerIndex = reader.ReadByte();
					if (playerIndex >= 0 && playerIndex < Main.maxPlayers) {
						Main.player[playerIndex].GetModPlayer<ShiftingPlayer>().ApplySync(reader);
					}
					break;
			}
		}
	}
}
