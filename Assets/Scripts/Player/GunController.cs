using UnityEngine;
using System.Collections;
using Photon;

public class GunController : PunBehaviour {

	Vector3 m_position;
	Quaternion m_rotation;
	float lerpSpeed = 10.0f;	//内插速度

	//初始化玩家位置与朝向
	void Start(){
		m_position = transform.position;	
		m_rotation = transform.rotation;
	}

	//序列化发送、获取数据
	void OnPhotonSerializeView(PhotonStream stream,PhotonMessageInfo info){
		if (stream.isWriting) 						//本地玩家发送数据
		{
			stream.SendNext (transform.position);
			stream.SendNext (transform.rotation);
		} 
		else 										//远程玩家接收数据
		{
			m_position = (Vector3)stream.ReceiveNext();
			m_rotation = (Quaternion)stream.ReceiveNext();
		}
	}

	void Update () {
		if (!photonView.isMine) 	//如果玩家对象不属于本地玩家，需要根据接收的数据更新玩家对象的位置与朝向
		{
			transform.position = Vector3.Lerp 		
				(transform.position, m_position, Time.deltaTime * lerpSpeed);	//使用Lerp函数实现玩家的平滑移动
			transform.rotation = Quaternion.Lerp 
				(transform.rotation, m_rotation, Time.deltaTime * lerpSpeed);	//使用Lerp函数实现玩家的平滑转动
		}
	}

}