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

    public void SetModifiedCircuit(TileEntityButtonPush cur = null)
    {
        for (int i = 0; i < GetPowerItem().Children.Count; i++)
        {
            var item = GetPowerItem().Children[i];
            if (item.TileEntity is TileEntityButtonPush btn) {
                btn.SetModifiedCircuit(cur);
                if (btn != cur) btn.SetModified();
            }
        }
        if (cur != this) SetModified();
    }

    public void HandleClientToggle()
    {
        if (this.PowerItem is PowerTrigger trigger && trigger.IsActive) {
            if (trigger.TriggerPowerDuration == PowerTrigger.TriggerPowerDurationTypes.Always) {
                this.ResetTrigger();
            } else {
                this.IsTriggered = true;
            }
        } else {
            this.IsTriggered = true;
        }
        this.SetModifiedCircuit();
    }


    public override void read(PooledBinaryReader _br, TileEntity.StreamModeRead _eStreamMode)
    {
        base.read(_br, _eStreamMode);
        if (_eStreamMode == TileEntity.StreamModeRead.FromClient)
        {
            bool hasConfigChange = false;
            TileEntityButtonPush root = GetPushButtonCircuitRoot();
            PowerTrigger item = root.PowerItem as PowerTrigger;
            var wasTriggerPowerDelay = item.TriggerPowerDelay;
            var wasTriggerPowerDuration = item.TriggerPowerDuration;
            item.TriggerPowerDelay = (PowerTrigger.TriggerPowerDelayTypes) _br.ReadByte();
            item.TriggerPowerDuration = (PowerTrigger.TriggerPowerDurationTypes) _br.ReadByte();
            if (item.TriggerPowerDelay != wasTriggerPowerDelay) hasConfigChange = true;
            if (item.TriggerPowerDuration != wasTriggerPowerDuration) hasConfigChange = true;
            bool hasClientReset = _br.ReadBoolean();
            bool hasClientToggle = _br.ReadBoolean();
            if (hasClientReset) root.ResetTrigger();
            else if (hasClientToggle) root.HandleClientToggle();
            else if (hasConfigChange) root.SetModifiedCircuit(this);
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
        bool wasReset = false;
        if (_eStreamMode == TileEntity.StreamModeWrite.ToServer) 
        {
            wasReset = this.ClientData.ResetTrigger;
            this.ClientData.ResetTrigger = false;
        }
        base.write(_bw, _eStreamMode);
        if (_eStreamMode == TileEntity.StreamModeWrite.ToServer) 
        {
            this.ClientData.ResetTrigger = false;
            _bw.Write(this.ClientData.Property1);
            _bw.Write(this.ClientData.Property2);
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
            _bw.Write(item.IsActive);
        }
    }

    protected override PowerItem CreatePowerItem() {
        return base.CreatePowerItem();
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
        // Handles disconnects
        UpdateEmissionColor(null);
    }

}
