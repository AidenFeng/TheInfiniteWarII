using UnityEngine;
using System.Collections;

/// <summary>
/// Makes a scene object pickup-able. Needs a PhotonView which belongs to the scene.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PickUpItemSimple_Potion : Photon.MonoBehaviour
{
	public int HealthPoint = 20;			//收集血瓶后玩家加血量
	
	public float SecondsBeforeRespawn = 10;	//血瓶收集后重现的时间
	public bool PickupOnCollide;
	public bool SentPickup;

	//使用触发器Trigger检测是否有游戏对象进入血瓶的收集范围
	public void OnTriggerEnter(Collider other)
	{
		// we only call Pickup() if "our" character collides with this PickupItem.
		// note: if you "position" remote characters by setting their translation, triggers won't be hit.
		if(other.tag !="Player")return;								//如果进入触发器范围的游戏对象标签Tag不是"Player"，使用return结束函数的执行
		PhotonView otherpv = other.GetComponent<PhotonView>();		//获取进入触发器范围的游戏对象的PhotonView组件
		if (this.PickupOnCollide && otherpv != null && otherpv.isMine)	//进入血瓶触发器范围的玩家对象是本地玩家时
		{
			//Debug.Log("OnTriggerEnter() calls Pickup().");
			this.Pickup();	//调用Pickup函数，执行收集逻辑
		}
	}
		
	public void Pickup()
	{
		if (this.SentPickup)
		{
			// skip sending more pickups until the original pickup-RPC got back to this client
			return;
		}

		this.SentPickup = true;
		this.photonView.RPC("PunPickupSimple", PhotonTargets.AllViaServer);	//使用RPC,调用所有玩家的RPC函数PunPickSimple
	}

	//实现玩家加血、血瓶收集的效果
	[PunRPC]
	public void PunPickupSimple(PhotonMessageInfo msgInfo)
	{
		// one of the messages might be ours
		// note: you could check "active" first, if you're not interested in your own, failed pickup-attempts.
		if (this.SentPickup && msgInfo.sender.isLocal)		//本地玩家调用GameManager类的localPlayerAddHealth脚本，执行本地玩家加血逻辑
		{
			if (this.gameObject.GetActive())
			{
				// picked up! yay.
				GameManager.gm.localPlayerAddHealth(HealthPoint);
			}
			else
			{
				// pickup failed. too late (compared to others)
			}
		}

		this.SentPickup = false;

		if (!this.gameObject.GetActive())
		{
			Debug.Log("Ignored PUN RPC, cause item is inactive. " + this.gameObject);
			return;
		}

		// how long it is until this item respanws, depends on the pickup time and the respawn time
		double timeSinceRpcCall = (PhotonNetwork.time - msgInfo.timestamp);
		float timeUntilRespawn = SecondsBeforeRespawn - (float)timeSinceRpcCall;
		//Debug.Log("msg timestamp: " + msgInfo.timestamp + " time until respawn: " + timeUntilRespawn);

		if (timeUntilRespawn > 0)	
		{
			// this script simply disables the GO for a while until it respawns. 
			this.gameObject.SetActive(false);			//禁用血瓶对象
			Invoke("RespawnAfter", timeUntilRespawn);	//使用Invoke函数，经过timeUntilRespawn时间后调用函数RespawnAfter函数
		}
	}

	//血瓶再生
	public void RespawnAfter()
	{
		if (this.gameObject != null)
		{
			this.gameObject.SetActive(true);	//启用血瓶对象
		}
	}
}
