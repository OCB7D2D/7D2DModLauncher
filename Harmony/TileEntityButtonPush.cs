using System.IO;
using System.Reflection;
using DMT;
using HarmonyLib;
using UnityEngine;

public class TileEntityButtonPush : TileEntityPoweredTrigger
{

	public bool hasToggle = false;

	public bool serverTriggered = false;

    public TileEntityButtonPush(Chunk _chunk) :
        base(_chunk)
    {
    }

    public override TileEntityType GetTileEntityType()
    {
        // really just an arbitrary number
        // I tend to use number above 241
        return (TileEntityType) 243;
    }


	public TileEntityButtonPush GetPushButtonCircuitRoot()
	{
		TileEntityButtonPush node = this;
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

	public void UpdateStates(TileEntityButtonPush tileEntity, TileEntityButtonPush root = null)
	{
		if (tileEntity == null || tileEntity.GetPowerItem() == null) return;
		if (root == null) root = GetPushButtonCircuitRoot();
		for (int i = 0; i < tileEntity.GetPowerItem().Children.Count; i++) {
			PowerItem child = tileEntity.GetPowerItem().Children[i];
			if (child.TileEntity is TileEntityButtonPush te) {
				UpdateStates(te, root);
			}
		}
		tileEntity.SetModified();
	}

	public void HandleClientToggle()
	{
		TileEntityButtonPush root = GetPushButtonCircuitRoot();
		if (root.PowerItem is PowerTrigger trigger && trigger.IsActive) {
			if (trigger.TriggerPowerDuration == PowerTrigger.TriggerPowerDurationTypes.Always) {
				root.ResetTrigger();
				UpdateStates(root, root);
			} else {
				root.IsTriggered = true;
				UpdateStates(root, root);
			}
		} else {
			root.IsTriggered = true;
			UpdateStates(root, root);
		}

	}

	public void UpdateTriggerGroupForClients(TileEntityButtonPush cur)
	{
		// if (reset) ResetTrigger();
		for (int i = 0; i < GetPowerItem().Children.Count; i++)
		{
			var item = GetPowerItem().Children[i];
			if (item.TileEntity is TileEntityButtonPush btn) {
				btn.UpdateTriggerGroupForClients(cur);
				if (btn != cur) btn.SetModified();
			}
		}
		if (cur != this) SetModified();
	}

	public override void read(PooledBinaryReader _br, TileEntity.StreamModeRead _eStreamMode)
	{
		Log.Out("Read Tile Entity Push Btn " + _eStreamMode);

		base.read(_br, _eStreamMode);
		// _br.ReadByte();
		// _br.ReadString();
		Log.Out("Read Tile Entity Push Btn " + _eStreamMode);
		if (_eStreamMode == TileEntity.StreamModeRead.FromClient)
		{
			TileEntityButtonPush root = GetPushButtonCircuitRoot();
			PowerTrigger item = root.PowerItem as PowerTrigger;
			bool hasChanged = false;
			var wasTriggerPowerDelay = item.TriggerPowerDelay;
			var wasTriggerPowerDuration = item.TriggerPowerDuration;
			item.TriggerPowerDelay = (PowerTrigger.TriggerPowerDelayTypes) _br.ReadByte();
			item.TriggerPowerDuration = (PowerTrigger.TriggerPowerDurationTypes) _br.ReadByte();
			if (item.TriggerPowerDelay != wasTriggerPowerDelay) hasChanged = true;
			if (item.TriggerPowerDuration != wasTriggerPowerDuration) hasChanged = true;
			Log.Out("Read from Client " + item.TriggerPowerDuration);
			bool wasReset = _br.ReadBoolean();
			bool wasToggle = _br.ReadBoolean();
			// On Toggle, we should check if something changes from inactive to active
			// or from active to inactive, meaning all must be updated. Otherwise nothing
			// must be done ...
			if (wasToggle) HandleClientToggle();
			// In case of reset, we should check if not already inactive.
			else if (wasReset) root.ResetTrigger();
			// Otherwise only update everybody with new config options
			else if (hasChanged) root.UpdateTriggerGroupForClients(this);
		}
		else if (_eStreamMode == TileEntity.StreamModeRead.FromServer) {
			this.ClientData.Property1 = _br.ReadByte();
			this.ClientData.Property2 = _br.ReadByte();
			Log.Out("Read from server " + this.ClientData.Property2);
			serverTriggered = _br.ReadBoolean();
			UpdateEmissionColor();
		}
	}

	public override void write(PooledBinaryWriter _bw, TileEntity.StreamModeWrite _eStreamMode)
	{
		Log.Out("+Write");
		bool wasReset = false;
		if (_eStreamMode == TileEntity.StreamModeWrite.ToServer) 
		{
			wasReset = this.ClientData.ResetTrigger;
			this.ClientData.ResetTrigger = false;
		}
		base.write(_bw, _eStreamMode);
		// _bw.Write((byte) this.TriggerType);
//    if (this.TriggerType == PowerTrigger.TriggerTypes.Motion)
//      _bw.Write(this.ownerID);
		Log.Out("-Write");
		if (_eStreamMode == TileEntity.StreamModeWrite.ToServer) 
		{
			this.ClientData.ResetTrigger = false;
			_bw.Write(this.ClientData.Property1);
			_bw.Write(this.ClientData.Property2);
			Log.Out("Write To Server " + this.ClientData.Property2);
			_bw.Write(wasReset);
			_bw.Write(hasToggle);
			hasToggle = false;
		}
		else if (_eStreamMode == TileEntity.StreamModeWrite.ToClient) 
		{
			TileEntityButtonPush root = GetPushButtonCircuitRoot();
			PowerTrigger item = root.PowerItem as PowerTrigger;
			_bw.Write((byte) item.TriggerPowerDelay);
			_bw.Write((byte) item.TriggerPowerDuration);
			Log.Out("Write to client " + item.TriggerPowerDuration);
			_bw.Write(item.IsActive);
		}
	}

  protected override PowerItem CreatePowerItem() {
	  var item = base.CreatePowerItem();
	  Log.Out("Create Power Item " + item);
	  return item;
  } 

	// Direct children represent same state as `root`
    public virtual void UpdateEmissionColor(TileEntityButtonPush root = null)
    {
		if (root == null) root = GetPushButtonCircuitRoot();
        var pos = ToWorldPos();
        var _world = GameManager.Instance.World;
		if (_world == null) return;
        Chunk chunk = (Chunk) _world.GetChunkFromWorldPos(pos);
		if (chunk == null) return;
        BlockEntityData blockEntity = chunk.GetBlockEntity(pos);
        if (blockEntity != null &&
            blockEntity.transform != null &&
            blockEntity.transform.gameObject != null)
        {
            PowerTrigger item = root.PowerItem as PowerTrigger;
			bool hasPower = item != null ? item.IsPowered : IsPowered;
			bool hasTrigger = item != null ? item.IsActive : serverTriggered;

            Color color = hasTrigger ? Color.green : Color.red;
			float intensity = hasPower ? 2f : .5f;
            if (!hasPower) color = Color.yellow;

            Renderer[] componentsInChildren =
                blockEntity
                    .transform
                    .gameObject
                    .GetComponentsInChildren<Renderer>();
            if (componentsInChildren != null)
            {
                for (int index = 0; index < componentsInChildren.Length; ++index)
                {
                    if (
                        (Object) componentsInChildren[index].material !=
                        (Object) componentsInChildren[index].sharedMaterial
                    ) {
                        componentsInChildren[index].material =
                            new Material(componentsInChildren[index]
                                    .sharedMaterial);
					}
					// Only enable emission color on specific tags
					// No idea how this is done in e.g. vanilla power switch
					if (componentsInChildren[index].tag != "T_Deco") continue;
                    componentsInChildren[index].sharedMaterial = componentsInChildren[index].material;
                    componentsInChildren[index].material.SetColor("_EmissionColor", color * intensity);
                    componentsInChildren[index].material.SetColor("_Color", color);
                    componentsInChildren[index].material.EnableKeyword("_EMISSION");
                }
            }
        }
    }

	protected override void setModified()
	{
		base.setModified();
		UpdateEmissionColor(null);
	}

    /*
  public override void HandleDisconnectChildren()
  {
    base.HandleDisconnectChildren();
    Log.Out("HandleDisconnectChildren");
  }

  public override void HandleDisconnect()
  {
    base.HandleDisconnect();
    Log.Out("HandleDisconnect");
  }
*/
}
