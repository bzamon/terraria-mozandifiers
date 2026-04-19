using System.IO;
using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Common;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public sealed class ShiftingPlayer : ModPlayer
{
	internal ShiftingSimulationId ActiveSimulationId { get; private set; }
	internal ShiftingSimulationId PreviousSimulationId { get; private set; }
	internal ulong ShiftExpireTick { get; private set; }
	internal ulong CombatExpireTick { get; private set; }
	internal ShiftingWeaponMode ActiveWeaponMode { get; private set; }
	internal int ActiveItemType { get; private set; }

	public override void PostUpdate()
	{
		if (ActiveSimulationId == ShiftingSimulationId.None) {
			return;
		}

		if (!TryGetHeldShiftingWeaponMode(Player, out ShiftingWeaponMode heldMode)
			|| Player.HeldItem.type != ActiveItemType
			|| heldMode != ActiveWeaponMode) {
			ClearState(sync: IsAuthoritative());
			return;
		}

		ulong currentTick = Main.GameUpdateCount;
		if (currentTick > CombatExpireTick) {
			ClearState(sync: IsAuthoritative());
			return;
		}

		if (IsAuthoritative() && currentTick >= ShiftExpireTick) {
			RollNewSimulation(Player.HeldItem, heldMode, currentTick);
		}
	}

	internal void RegisterWeaponUse(Item item)
	{
		if (!IsAuthoritative()) {
			return;
		}

		RegisterCombatActivity(item);
	}

	internal void RegisterSuccessfulHit(Item item)
	{
		if (!IsAuthoritative()) {
			return;
		}

		RegisterCombatActivity(item);
	}

	internal bool TryGetActiveSimulation(Item item, out ShiftingSimulationId simulationId)
	{
		if (item == null
			|| item.IsAir
			|| item.prefix != ModContent.PrefixType<ShiftingPrefix>()
			|| ActiveSimulationId == ShiftingSimulationId.None
			|| ActiveItemType != item.type
			|| !ShiftingSimulationCatalog.TryGetWeaponMode(item, out ShiftingWeaponMode mode)
			|| mode != ActiveWeaponMode) {
			simulationId = ShiftingSimulationId.None;
			return false;
		}

		simulationId = ActiveSimulationId;
		return true;
	}

	internal void WriteSync(BinaryWriter writer)
	{
		writer.Write((byte)ActiveSimulationId);
		writer.Write((byte)PreviousSimulationId);
		writer.Write(ShiftExpireTick);
		writer.Write(CombatExpireTick);
		writer.Write((byte)ActiveWeaponMode);
		writer.Write(ActiveItemType);
	}

	internal void ApplySync(BinaryReader reader)
	{
		ShiftingSimulationId syncedSimulationId = (ShiftingSimulationId)reader.ReadByte();
		ShiftingSimulationId syncedPreviousSimulationId = (ShiftingSimulationId)reader.ReadByte();
		ulong syncedShiftExpireTick = reader.ReadUInt64();
		ulong syncedCombatExpireTick = reader.ReadUInt64();
		ShiftingWeaponMode syncedWeaponMode = (ShiftingWeaponMode)reader.ReadByte();
		int syncedItemType = reader.ReadInt32();

		ShiftingSimulationId oldSimulationId = ActiveSimulationId;
		ActiveSimulationId = syncedSimulationId;
		PreviousSimulationId = syncedPreviousSimulationId;
		ShiftExpireTick = syncedShiftExpireTick;
		CombatExpireTick = syncedCombatExpireTick;
		ActiveWeaponMode = syncedWeaponMode;
		ActiveItemType = syncedItemType;

		ShowShiftingCombatText(oldSimulationId, syncedSimulationId);
	}

	private void RegisterCombatActivity(Item item)
	{
		if (item == null
			|| item.IsAir
			|| item.prefix != ModContent.PrefixType<ShiftingPrefix>()
			|| !ShiftingSimulationCatalog.TryGetWeaponMode(item, out ShiftingWeaponMode weaponMode)) {
			return;
		}

		ulong currentTick = Main.GameUpdateCount;
		CombatExpireTick = currentTick + (ulong)ShiftingPrefix.CombatWindowTicks;

		if (ActiveItemType != item.type || ActiveWeaponMode != weaponMode) {
			ResetForNewItemContext(item.type, weaponMode);
		}

		if (ActiveSimulationId == ShiftingSimulationId.None || currentTick >= ShiftExpireTick) {
			RollNewSimulation(item, weaponMode, currentTick);
		}
	}

	private void RollNewSimulation(Item item, ShiftingWeaponMode weaponMode, ulong currentTick)
	{
		ShiftingSimulationId oldSimulationId = ActiveSimulationId;
		ShiftingSimulationId selectedSimulationId = ShiftingSimulationCatalog.SelectNextSimulation(
			weaponMode,
			PreviousSimulationId,
			currentTick,
			Player.whoAmI,
			item.type);

		ActiveSimulationId = selectedSimulationId;
		PreviousSimulationId = selectedSimulationId;
		ActiveWeaponMode = weaponMode;
		ActiveItemType = item.type;
		ShiftExpireTick = currentTick + (ulong)ShiftingPrefix.ShiftDurationTicks;
		CombatExpireTick = currentTick + (ulong)ShiftingPrefix.CombatWindowTicks;

		ShowShiftingCombatText(oldSimulationId, selectedSimulationId);
		SyncToOwner();
	}

	private void ResetForNewItemContext(int itemType, ShiftingWeaponMode weaponMode)
	{
		ActiveSimulationId = ShiftingSimulationId.None;
		PreviousSimulationId = ShiftingSimulationId.None;
		ShiftExpireTick = 0;
		CombatExpireTick = 0;
		ActiveWeaponMode = weaponMode;
		ActiveItemType = itemType;
	}

	private void ClearState(bool sync)
	{
		ShiftingSimulationId oldSimulationId = ActiveSimulationId;
		ActiveSimulationId = ShiftingSimulationId.None;
		ShiftExpireTick = 0;
		CombatExpireTick = 0;
		ActiveWeaponMode = ShiftingWeaponMode.None;
		ActiveItemType = 0;

		ShowShiftingCombatText(oldSimulationId, ActiveSimulationId);
		if (sync) {
			SyncToOwner();
		}
	}

	private void SyncToOwner()
	{
		if (Main.netMode != NetmodeID.Server) {
			return;
		}

		ModContent.GetInstance<Mozandifiers>().SendShiftingState(Player);
	}

	private void ShowShiftingCombatText(ShiftingSimulationId oldSimulationId, ShiftingSimulationId newSimulationId)
	{
		if (Player.whoAmI != Main.myPlayer || oldSimulationId == newSimulationId || newSimulationId == ShiftingSimulationId.None) {
			return;
		}

		string simulationName = ShiftingSimulationCatalog.GetDisplayName(Mod.Name, newSimulationId);
		if (string.IsNullOrEmpty(simulationName)) {
			return;
		}

		string text = Language.GetTextValue($"Mods.{Mod.Name}.Prefixes.ShiftingPrefix.CombatText", simulationName);
		CombatText.NewText(Player.Hitbox, new Color(195, 160, 255), text);
	}

	private static bool TryGetHeldShiftingWeaponMode(Player player, out ShiftingWeaponMode weaponMode)
	{
		Item heldItem = player.HeldItem;
		if (heldItem == null
			|| heldItem.IsAir
			|| heldItem.prefix != ModContent.PrefixType<ShiftingPrefix>()
			|| !ShiftingSimulationCatalog.TryGetWeaponMode(heldItem, out weaponMode)) {
			weaponMode = ShiftingWeaponMode.None;
			return false;
		}

		return true;
	}

	private static bool IsAuthoritative()
	{
		return Main.netMode != NetmodeID.MultiplayerClient;
	}
}
