using DMT;
using System.IO;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

public class BlockButtonPush : BlockPowered
{

  private BlockActivationCommand[] cmds = new BlockActivationCommand[3]
  {
	new BlockActivationCommand("light", "electric_switch", true),
	new BlockActivationCommand("options", "tool", true),
	new BlockActivationCommand("take", "hand", false)
  };

  public BlockButtonPush() => this.HasTileEntity = true;

  public override void Init() => base.Init();

  public override void OnBlockAdded(
    WorldBase _world,
    Chunk _chunk,
    Vector3i _blockPos,
    BlockValue _blockValue)
  {
    base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
    if (_world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntityPoweredTrigger) return;
    TileEntityPowered tileEntity = this.CreateTileEntity(_chunk);
    tileEntity.localChunkPos = World.toBlock(_blockPos);
    tileEntity.InitializePowerData();
    _chunk.AddTileEntity((TileEntity) tileEntity);
    (tileEntity as TileEntityButtonPush).UpdateEmissionColor(null);
  }

  public override void OnBlockEntityTransformAfterActivated(
    WorldBase _world,
    Vector3i _blockPos,
    int _cIdx,
    BlockValue _blockValue,
    BlockEntityData _ebcd)
  {
    base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
    if (_blockValue.ischild || !(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityButtonPush tileEntity)) return;
    tileEntity.UpdateEmissionColor(null);
  }

  public override TileEntityPowered CreateTileEntity(Chunk chunk)
  {
    TileEntityButtonPush entityPoweredTrigger = new TileEntityButtonPush(chunk);
    entityPoweredTrigger.PowerItemType =  (PowerItem.PowerItemTypes) 243;
    entityPoweredTrigger.TriggerType = PowerTrigger.TriggerTypes.Motion;
    return (TileEntityPowered) entityPoweredTrigger;
  }

  public override string GetActivationText(
    WorldBase _world,
    BlockValue _blockValue,
    int _clrIdx,
    Vector3i _blockPos,
    EntityAlive _entityFocusing)
  {
    TileEntityButtonPush tileEntity = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityButtonPush;
    PlayerActionsLocal playerInput = ((EntityPlayerLocal) _entityFocusing).playerInput;
    if (tileEntity == null) return "{No tile entitiy}";
	// tileEntity = RewindToPushButtonCircuitRoot(tileEntity);
    // if (!(tileEntity.PowerItem is PowerTrigger trigger)) return "{No power item}"; // trigger.IsActive
	return Localization.Get("ocbBlockPushPowerButton");
  }

  public TileEntityButtonPush RewindToPushButtonCircuitRoot(TileEntityButtonPush node)
  {
	  while (node != null)
	  {
		if (node.PowerItem != null && node.PowerItem.Parent != null) {
			if (node.PowerItem.Parent.TileEntity is TileEntityButtonPush btn) {
				node = btn;
				continue;
			}
		}
		break;
	  }
	  return node;
  }

  public override void OnBlockValueChanged(
    WorldBase _world,
    Chunk _chunk,
    int _clrIdx,
    Vector3i _blockPos,
    BlockValue _oldBlockValue,
    BlockValue _newBlockValue)
  {
	  base.OnBlockValueChanged(_world, _chunk, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
      // (tileEntity as TileEntityButtonPush).UpdateEmissionColor(null);
	  Log.Out("OnBlockValueChanged");
  }

	public void UpdateStates(WorldBase _world, int _cIdx, TileEntityButtonPush tileEntity, TileEntityButtonPush root = null)
	{
		if (root == null) root = RewindToPushButtonCircuitRoot(root);
		tileEntity.UpdateEmissionColor(root);
		for (int i = 0; i < tileEntity.PowerItem.Children.Count; i++) {
			PowerItem child = tileEntity.PowerItem.Children[i];
			Log.Out("HasChild " + child.PowerItemType);
			if (child is PowerTrigger trigger) {
				if (trigger.TileEntity is TileEntityButtonPush te) {
					UpdateStates(_world, _cIdx, te, root);
					Log.Out("Has child that is trigger");
				}
			}
		}
	}

  public void UpdateState(WorldBase _world, int _cIdx, TileEntityButtonPush tileEntity, bool flag1)
  {
    //BlockEntityData blockEntity = _world.ChunkClusters[_cIdx].GetBlockEntity(tileEntity.ToWorldPos());
    //if (blockEntity != null && (Object) blockEntity.transform != (Object) null && (Object) blockEntity.transform.gameObject != (Object) null)
    //{
	//	PowerTrigger item = tileEntity.PowerItem as PowerTrigger;
    //  Renderer[] componentsInChildren = blockEntity.transform.gameObject.GetComponentsInChildren<Renderer>();
    //  if (componentsInChildren != null)
    //  {
	//Log.Out("Has componentsInChildren " + componentsInChildren);
    //    for (int index = 0; index < componentsInChildren.Length; ++index)
    //    {
    //      if ((Object) componentsInChildren[index].material != (Object) componentsInChildren[index].sharedMaterial)
    //        componentsInChildren[index].material = new Material(componentsInChildren[index].sharedMaterial);
    //      componentsInChildren[index].material.SetColor("_EmissionColor", flag1 ? Color.green : Color.red);
    //      componentsInChildren[index].sharedMaterial = componentsInChildren[index].material;
    //      componentsInChildren[index].material.EnableKeyword("_EMISSION");
    //      // componentsInChildren[index].material.EnableKeyword("_COLOR");
    //    }
    //  }
    //}

  }


  public override bool OnBlockActivated(
    int cmd,
    WorldBase _world,
    int _cIdx,
    Vector3i _blockPos,
    BlockValue _blockValue,
    EntityAlive _player)
  {
    if (_blockValue.ischild)
    {
      Vector3i parentPos = Block.list[_blockValue.type].multiBlockPos.GetParentPos(_blockPos, _blockValue);
      BlockValue block = _world.GetBlock(parentPos);
      return this.OnBlockActivated(cmd, _world, _cIdx, parentPos, block, _player);
    }
    if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntityButtonPush tileEntity))
      return false;
	tileEntity = RewindToPushButtonCircuitRoot(tileEntity);
    if (cmd== 0)
    {
		PowerTrigger item = tileEntity.PowerItem as PowerTrigger;
		if (item.TriggerPowerDuration == PowerTrigger.TriggerPowerDurationTypes.Always) {
			if (item.IsActive) {
				tileEntity.ResetTrigger();
				UpdateStates(_world, _cIdx, tileEntity, tileEntity);
				return true;
			}
		}
		tileEntity.IsTriggered = !tileEntity.IsTriggered;
		UpdateStates(_world, _cIdx, tileEntity, tileEntity);
	}
	else if (cmd == 1)
	{
	    _player.AimingGun = false;
		Vector3i worldPos = tileEntity.ToWorldPos();
		_world.GetGameManager().TELockServer(_cIdx, worldPos, tileEntity.entityId, _player.entityId);
	}
	else if (cmd == 2)
	{
      this.TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
	}
	else {
		return false;
	}
	return true;
  }

  public override BlockActivationCommand[] GetBlockActivationCommands(
	WorldBase _world,
	BlockValue _blockValue,
	int _clrIdx,
	Vector3i _blockPos,
	EntityAlive _entityFocusing)
  {
	bool flag1 = _world.CanPlaceBlockAt(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
	bool flag2 = _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
	this.cmds[0].enabled = flag1;
	this.cmds[1].enabled = flag1;
	this.cmds[2].enabled = flag2 && (double) this.TakeDelay > 0.0;
	return this.cmds;
  }

}