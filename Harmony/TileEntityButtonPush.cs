using System.IO;
using System.Reflection;
using DMT;
using HarmonyLib;
using UnityEngine;

public class TileEntityButtonPush : TileEntityPoweredTrigger
{
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

  static public TileEntityButtonPush GetPushButtonCircuitRoot(TileEntityButtonPush node)
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

	// Direct children represent same state as `root`
    public virtual void UpdateEmissionColor(TileEntityButtonPush root = null)
    {
		if (root == null) root = GetPushButtonCircuitRoot(this);
        var pos = ToWorldPos();
        var _world = GameManager.Instance.World;
        Chunk chunk = (Chunk) _world.GetChunkFromWorldPos(pos);
        var _cIdx = chunk.ClrIdx;
        BlockEntityData blockEntity =
            _world.ChunkClusters[_cIdx].GetBlockEntity(pos);
        if (
            blockEntity != null &&
            (Object) blockEntity.transform != (Object) null &&
            (Object) blockEntity.transform.gameObject != (Object) null
        )
        {
            PowerTrigger item = root.PowerItem as PowerTrigger;
            Color color = item.IsActive ? Color.green : Color.red;
			float intensity = item.IsPowered ? 2f : .5f;
            if (!item.IsPowered) color = Color.yellow;
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
