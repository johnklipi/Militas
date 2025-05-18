using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Steamworks;
using UnityEngine;

namespace ModernWarfare;

public static class Main
{
    private static ManualLogSource? modLogger;
    private static bool doStuff = false;
    public static void Load(ManualLogSource logger)
    {
        PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
        EnumCache<SkinType>.AddMapping("warfare", (SkinType)PolyMod.Registry.autoidx);
        EnumCache<SkinType>.AddMapping("warfare", (SkinType)PolyMod.Registry.autoidx);
        PolyMod.Registry.autoidx++;
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.addStartingResourcesToCapital))]
    private static void MapGenerator_AddStartingResourcesToCapital(MapData map, GameState gameState, PlayerState player, ResourceData startingResource, int minResourcesCount = 2)
    {
        TileData tile = gameState.Map.GetTile(player.startTile);
        PlayerState playerState;
        gameState.TryGetPlayer(player.Id, out playerState);
        if (gameState.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
        {
            Il2CppSystem.Collections.Generic.List<TileData> cityAreaSorted = ActionUtils.GetCityAreaSorted(gameState, tile);
            cityAreaSorted.Reverse();
            for (int j = 0; j < cityAreaSorted.Count; j++)
            {
                TileData tileData2 = cityAreaSorted[j];
                if (tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
                {
                    tileData2.resource = new ResourceState
                    {
                        type = EnumCache<ResourceData.Type>.GetType("oil")
                    };
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CaptureCityAction), nameof(CaptureCityAction.ExecuteDefault))]
    private static void CaptureCityAction_ExecuteDefault(CaptureCityAction __instance, GameState gameState)
    {
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        PlayerState playerState;
        gameState.TryGetPlayer(__instance.PlayerId, out playerState);
        if (gameState.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
        {
            Il2CppSystem.Collections.Generic.List<TileData> cityAreaSorted = ActionUtils.GetCityAreaSorted(gameState, tile);
            cityAreaSorted.Reverse();
            for (int j = 0; j < cityAreaSorted.Count; j++)
            {
                TileData tileData2 = cityAreaSorted[j];
                if (tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
                {
                    gameState.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("createoil"), tileData2.coordinates, false));
                }
                else if (!tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(EnumCache<ResourceData.Type>.GetType("oil")))
                {
                    gameState.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("creategame"), tileData2.coordinates, false));
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ExpandCityAction), nameof(ExpandCityAction.ExecuteDefault))]
    private static void ExpandCityAction_ExecuteDefault(ExpandCityAction __instance, GameState state)
    {
        TileData tile = state.Map.GetTile(__instance.Coordinates);
        PlayerState playerState;
        state.TryGetPlayer(__instance.PlayerId, out playerState);
        if (state.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
        {
            Il2CppSystem.Collections.Generic.List<TileData> cityAreaSorted = ActionUtils.GetCityAreaSorted(state, tile);
            cityAreaSorted.Reverse();
            for (int j = 0; j < cityAreaSorted.Count; j++)
            {
                TileData tileData2 = cityAreaSorted[j];
                if (tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
                {
                    state.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("createoil"), tileData2.coordinates, false));
                }
                else if (!tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(EnumCache<ResourceData.Type>.GetType("oil")))
                {
                    state.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("creategame"), tileData2.coordinates, false));
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetMoveOptions))]
    private static void PathFinder_GetMoveOptions(ref Il2CppSystem.Collections.Generic.List<WorldCoordinates> __result, GameState gameState, WorldCoordinates start, int maxCost, UnitState unit)
    {
        if (unit.UnitData.movement == 0)
            __result = new();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    private static void GameLogicData_CanBuild(ref bool __result, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
    {
        if (improvement.HasAbility(ImprovementAbility.Type.Limited))
        {
            UnitData.Type unitType = improvement.CreatesUnit();
            if (unitType != UnitData.Type.None)
            {
                if (gameState.GameLogicData.TryGetData(unitType, out UnitData unitData))
                {
                    if (unitData.HasAbility(EnumCache<UnitAbility.Type>.GetType("warfarebuild")))
                    {
                        if (tile.rulingCityCoordinates != new WorldCoordinates(-1, -1))
                        {
                            TileData cityTile = gameState.Map.GetTile(tile.rulingCityCoordinates);
                            Il2CppSystem.Collections.Generic.List<TileData> cityAreaSorted = ActionUtils.GetCityAreaSorted(gameState, cityTile);
                            cityAreaSorted.Reverse();
                            for (int j = 0; j < cityAreaSorted.Count; j++)
                            {
                                TileData tileData = cityAreaSorted[j];
                                if (tileData.unit != null)
                                {
                                    if (tileData.unit.UnitData.HasAbility(EnumCache<UnitAbility.Type>.GetType("warfarebuild")))
                                    {
                                        __result = false;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Unit), nameof(Unit.UpdateObject), typeof(SkinVisualsTransientData))]
    private static void Unit_UpdateObject_Postfix(Unit __instance, SkinVisualsTransientData transientSkinData)
    {
        if (__instance.UnitState.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            foreach (SkinVisualsReference.VisualPart visualPart in __instance.skinVisuals.visualParts)
            {
                if (visualPart != null)
                {
                    if (visualPart.renderer != null)
                    {
                        if (visualPart.renderer.spriteRenderer != null)
                        {
                            var materialBlock = new UnityEngine.MaterialPropertyBlock();
                            visualPart.renderer.spriteRenderer.GetPropertyBlock(materialBlock);
                            materialBlock.SetColor("_OverlayColor", new Color(1.0f, 0.6f, 0.2f, 1f));
                            materialBlock.SetFloat("_OverlayStrength", 0.5f);
                            visualPart.renderer.spriteRenderer.SetPropertyBlock(materialBlock);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static void AttackCommand_ExecuteDefault(AttackCommand __instance, GameState gameState)
    {
        UnitState unitState;
        gameState.TryGetUnit(__instance.UnitId, out unitState);
        if (unitState.HasAbility(UnitAbility.Type.Splash, gameState) && unitState.HasAbility(EnumCache<UnitAbility.Type>.GetType("ionize"), gameState) && unitState.GetRange(gameState) > 1)
        {
            foreach (TileData tileData in gameState.Map.GetTileNeighbors(__instance.Target))
            {
                if (tileData.unit != null)
                {
                    tileData.unit.AddEffect(EnumCache<UnitEffect>.GetType("ionized"));
                }
            }
            TileData targetTile = gameState.Map.GetTile(__instance.Target);
            gameState.TryGetUnit(targetTile.unit.id, out UnitState targetUnit);
            targetUnit.AddEffect(EnumCache<UnitEffect>.GetType("ionized"));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HealAction), nameof(HealAction.ExecuteDefault))]
    private static void HealAction_ExecuteDefault(HealAction __instance, GameState gameState)
    {
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        if (tile == null)
        {
            return;
        }
        DeionizeUnit(tile);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HealOthersAction), nameof(HealOthersAction.Execute))]
    private static void HealOthersAction_Execute(HealOthersAction __instance, GameState state)
    {
        foreach (TileData tileData in state.Map.GetTile(__instance.Coordinates).GetHealOptions(__instance.PlayerId, state, false))
        {
            DeionizeUnit(tileData);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RecoverAction), nameof(RecoverAction.Execute))]
    private static void RecoverAction_Execute(RecoverAction __instance, GameState state)
    {
        TileData tile = state.Map.GetTile(__instance.Coordinates);
        if (tile == null)
        {
            return;
        }
        DeionizeUnit(tile);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttack), typeof(UnitState), typeof(GameState))]
    public static void UnitDataExtensions_GetAttack(ref int __result, UnitState unitState, GameState gameState)
    {
        if (unitState.UnitData.HasAbility(EnumCache<UnitAbility.Type>.GetType("turbinedrag")))
        {
            TileData tile = gameState.Map.GetTile(unitState.coordinates);
            if (tile.IsWater)
            {
                Console.Write(__result);
                __result = Math.Max(0, __result - 70);
                Console.Write(__result);
            }
        }
        if (unitState.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            __result /= 2;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TileDataExtensions), nameof(TileDataExtensions.GetHealOptions))]
    private static void TileDataExtensions_GetHealOptions(ref Il2CppSystem.Collections.Generic.List<TileData> __result, TileData tileState, byte playerId, GameState state, bool includeCenter = false)
    {
        Il2CppSystem.Collections.Generic.List<TileData> area = state.Map.GetArea(tileState.coordinates, 1, true, includeCenter);
        Il2CppSystem.Collections.Generic.List<TileData> finalArea = new();
        for (int i = 0; i < area.Count; i++)
        {
            TileData tileData = area[i];
            if (tileData.unit != null && tileData.unit.owner == playerId && tileData.unit.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
            {
                finalArea.Add(tileData);
            }
        }
        foreach (TileData data in __result)
        {
            if (!finalArea.Contains(data))
            {
                finalArea.Add(data);
            }
        }
        __result = finalArea;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.GetSelectedSkin))]
    private static void GameSettings_GetSelectedSkin(ref SkinType __result, GameSettings __instance, TribeData.Type tribeType)
    {
        if (tribeType == EnumCache<TribeData.Type>.GetType("warfare") && __result == SkinType.Default)
        {
            __result = EnumCache<SkinType>.GetType("warfare");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectTribePopup), nameof(SelectTribePopup.Button_OnClicked))]
    private static bool Button_OnClicked(SelectTribePopup __instance, ref SkinType type, UIRoundButton button)
    {
        if (__instance.tribeData.type == EnumCache<TribeData.Type>.GetType("warfare") && type == SkinType.Default)
        {
            type = EnumCache<SkinType>.GetType("warfare");
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SelectTribePopup), nameof(SelectTribePopup.SetOpinionButton))]
	private static void SetOpinionButton(SelectTribePopup __instance)
	{
		if (__instance.SkinType == EnumCache<SkinType>.GetType("warfare"))
		{
			__instance.uiTextButton.gameObject.SetActive(false);
		}
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.CanRecover))]
    public static void UnitDataExtensions_CanRecover(ref bool __result, UnitState unitState, GameState state)
    {
        if (!__result && unitState.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            __result = !unitState.HasLeader() && unitState.CanMove() && unitState.CanAttack();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitPopup), nameof(UnitPopup.AddStatsRow), typeof(string), typeof(string))]
    private static bool AddStatsRow(UnitPopup __instance, string name, ref string value)
    {
        if (name == "world.unit.attack")
        {
            __instance.attackBonus = (float)__instance.unit.UnitState.GetAttack(GameManager.GameState) / (float)__instance.unit.UnitData.GetAttack();
            value = (__instance.attackBonus != 1f) ? string.Format("{0} (x{1})", (float)__instance.data.attack * 0.1f, __instance.attackBonus) : ((float)__instance.data.attack * 0.1f).ToString();
        }
        return true;
    }

    private static void DeionizeUnit(TileData tileData)
    {
        if (tileData == null)
        {
            return;
        }
        UnitState unit = tileData.unit;
        if (unit == null)
        {
            return;
        }
        if (unit.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            unit.RemoveEffect(EnumCache<UnitEffect>.GetType("ionized"));
            UnitState passengerUnit = unit.passengerUnit;
            if (passengerUnit != null)
            {
                passengerUnit.RemoveEffect(EnumCache<UnitEffect>.GetType("ionized"));
            }
        }
    }
}