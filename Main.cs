using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using UnityEngine;

namespace ModernWarfare;
public static class Main
{
    private static ManualLogSource? modLogger;
    private static bool doStuff = false;
    public static void Load(ManualLogSource logger)
    {
        PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
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
                if(tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
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
                if(tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
                {
                    gameState.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("createoil"), tileData2.coordinates, false));
                }
                else if(!tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(EnumCache<ResourceData.Type>.GetType("oil")))
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
                if(tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(ResourceData.Type.Game))
                {
                    state.ActionStack.Add(new BuildAction(__instance.PlayerId, EnumCache<ImprovementData.Type>.GetType("createoil"), tileData2.coordinates, false));
                }
                else if(!tribeData.tribeAbilities.Contains(EnumCache<TribeAbility.Type>.GetType("oilrefiner")) && tileData2.HasResource(EnumCache<ResourceData.Type>.GetType("oil")))
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
        if(unit.UnitData.movement == 0)
            __result = new();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
	private static void GameLogicData_CanBuild(ref bool __result, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
	{
        if(improvement.HasAbility(ImprovementAbility.Type.Limited))
        {
            UnitData.Type unitType = improvement.CreatesUnit();
            if(unitType != UnitData.Type.None)
            {
                if(gameState.GameLogicData.TryGetData(unitType, out UnitData unitData))
                {
                    if(unitData.HasAbility(EnumCache<UnitAbility.Type>.GetType("warfarebuild")))
                    {
                        if(tile.rulingCityCoordinates != new WorldCoordinates(-1, -1))
                        {
                            TileData cityTile = gameState.Map.GetTile(tile.rulingCityCoordinates);
                            Il2CppSystem.Collections.Generic.List<TileData> cityAreaSorted = ActionUtils.GetCityAreaSorted(gameState, cityTile);
                            cityAreaSorted.Reverse();
                            for (int j = 0; j < cityAreaSorted.Count; j++)
                            {
                                TileData tileData = cityAreaSorted[j];
                                if(tileData.unit != null)
                                {
                                    if(tileData.unit.UnitData.type == unitType)
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
        if(__instance.UnitState.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            foreach (SkinVisualsReference.VisualPart visualPart in __instance.skinVisuals.visualParts)
            {
                if(visualPart != null){
                    if(visualPart.renderer != null){
                        if(visualPart.renderer.spriteRenderer != null){
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
	private static void ExecuteDefault(AttackCommand __instance, GameState gameState)
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
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
	private static void HealAction_ExecuteDefault(HealAction __instance, GameState gameState)
	{
		TileData tile = gameState.Map.GetTile(__instance.Coordinates);
		if (tile == null)
		{
			return;
		}
		UnitState unit = tile.unit;
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
        return;
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttack), typeof(UnitState), typeof(GameState))]
	public static void UnitDataExtensions_GetAttack(ref int __result, UnitState unitState, GameState gameState)
	{
        if(unitState.HasEffect(EnumCache<UnitEffect>.GetType("ionized")))
        {
            __result = __result / 2;
        }
	}
}