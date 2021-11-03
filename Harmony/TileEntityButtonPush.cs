using System.IO;
using System.Reflection;
using DMT;
using HarmonyLib;
using UnityEngine;

public class TileEntityButtonPush : TileEntityPowered
{

	public TileEntityPoweredTrigger.ClientTriggerData ClientData = new TileEntityPoweredTrigger.ClientTriggerData();

	public bool hasToggle = false;

	public bool serverTriggered = false;

    public TileEntityButtonPush(Chunk _chunk) :
        base(_chunk)
    {
    }

	// public bool Activate(bool activated, bool isOn)
	// {
	// 	World world = GameManager.Instance.World;
	// 	BlockValue block = this.chunk.GetBlock(this.localChunkPos);
	// 	return Block.list[block.type].ActivateBlock((WorldBase) world, this.GetClrIdx(), this.ToWorldPos(), block, isOn, activated);
	// }

    public override TileEntityType GetTileEntityType()
    {
        // really just an arbitrary number
        // I tend to use number above 241
  // return TileEntityType.Trigger;
        return (TileEntityType) 243;
    }


	public TileEntityButtonPush GetPushButtonCircuitRoot2()
	{
		TileEntityButtonPush node = this;
		while (node != null)
		{
			if (node.PowerItem == null) {
				//node.PowerItem = node.CreatePowerItemForTileEntity
				//	((ushort) node.chunk.GetBlock(node.localChunkPos).type);
			}
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

  static public TileEntityButtonPush GetPushButtonCircuitRoot(TileEntityButtonPush node)
  {
	  while (node != null)
	  {
		if (node.GetPowerItem() != null && node.GetPowerItem().Parent != null) {
			if (node.GetPowerItem().Parent.TileEntity is TileEntityButtonPush btn) {
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
		if (root == null) root = GetPushButtonCircuitRoot(root);
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
		TileEntityButtonPush root = GetPushButtonCircuitRoot(this);
		root.IsTriggered = !root.IsTriggered;
		UpdateStates(root, root);

	}

	public void HandleServerToggle()
	{
		TileEntityButtonPush root = GetPushButtonCircuitRoot(this);
	}

	public override void read(PooledBinaryReader _br, TileEntity.StreamModeRead _eStreamMode)
	{
		base.read(_br, _eStreamMode);
		Log.Out("Read Tile Entity Push Btn " + _eStreamMode);
		if (_eStreamMode == TileEntity.StreamModeRead.FromClient)
		{
			if (this.PowerItem == null)
				this.PowerItem = this.CreatePowerItemForTileEntity((ushort) this.chunk.GetBlock(this.localChunkPos).type);
			TileEntityButtonPush root = GetPushButtonCircuitRoot2();
			PowerTrigger item = root.PowerItem as PowerTrigger;
			item.TriggerPowerDelay = (PowerTrigger.TriggerPowerDelayTypes) _br.ReadByte();
			item.TriggerPowerDuration = (PowerTrigger.TriggerPowerDurationTypes) _br.ReadByte();
			if (_br.ReadBoolean()) {}; // Handle reset
			if (_br.ReadBoolean()) HandleClientToggle();
		}
		else if (_eStreamMode == TileEntity.StreamModeRead.FromServer) {
			this.ClientData.Property1 = _br.ReadByte();
			this.ClientData.Property2 = _br.ReadByte();
			serverTriggered = _br.ReadBoolean();
			UpdateEmissionColor();
		}
	}

	public override void write(PooledBinaryWriter _bw, TileEntity.StreamModeWrite _eStreamMode)
	{
		base.write(_bw, _eStreamMode);
		if (_eStreamMode == TileEntity.StreamModeWrite.ToServer) 
		{
			_bw.Write(this.ClientData.Property1);
			_bw.Write(this.ClientData.Property2);
			_bw.Write(this.ClientData.ResetTrigger);
			this.ClientData.ResetTrigger = false;
			_bw.Write(hasToggle);
			hasToggle = false;
		}
		else if (_eStreamMode == TileEntity.StreamModeWrite.ToClient) 
		{
			TileEntityButtonPush root = GetPushButtonCircuitRoot2();
			PowerTrigger item = root.PowerItem as PowerTrigger;
			_bw.Write((byte) item.TriggerPowerDelay);
			_bw.Write((byte) item.TriggerPowerDuration);
			_bw.Write(item.IsActive);
		}
	}

  public bool IsTriggered
  {
    get => ((PowerTrigger) this.PowerItem).IsTriggered;
    set
    {
      PowerTrigger powerItem = this.PowerItem as PowerTrigger;
      powerItem.IsTriggered = value;
    }
  }

  public override PowerItem CreatePowerItem() {
		// var item = base.CreatePowerItem();
		Log.Out("Create Power Item ");
		return (PowerItem) new PowerTrigger()
		{
			TriggerType = PowerTrigger.TriggerTypes.Motion
		};
  } 

	// Direct children represent same state as `root`
    public virtual void UpdateEmissionColor(TileEntityButtonPush root = null)
    {
		if (root == null) root = GetPushButtonCircuitRoot(this);
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

  public byte Property1
  {
    get
    {
      if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        return this.ClientData.Property1;
      return (byte) (this.PowerItem as PowerTrigger).TriggerPowerDelay;
    }
    set
    {
      if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
      {
          (this.PowerItem as PowerTrigger).TriggerPowerDelay = (PowerTrigger.TriggerPowerDelayTypes) value;
      }
      else
        this.ClientData.Property1 = value;
    }
  }

  public byte Property2
  {
    get
    {
      if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        return this.ClientData.Property2;
      return (byte) (this.PowerItem as PowerTrigger).TriggerPowerDuration;
    }
    set
    {
      if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
      {
          (this.PowerItem as PowerTrigger).TriggerPowerDuration = (PowerTrigger.TriggerPowerDurationTypes) value;
      }
      else
        this.ClientData.Property2 = value;
    }
  }

  public bool ShowTriggerOptions
  {
    get
    {
		return true;
    }
  }

	protected override void setModified()
	{
		base.setModified();
		UpdateEmissionColor(null);
	}

  public class ClientTriggerData
  {
    public byte Property1;
    public byte Property2;
    public int TargetType = 3;
    public bool ShowTriggerOptions;
    public bool ResetTrigger;
    public bool HasChanges;
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
