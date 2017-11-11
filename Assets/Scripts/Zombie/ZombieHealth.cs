using UnityEngine;
using System.Collections;
using Photon ;

public class ZombieHealth :  PunBehaviour{

	public int currentHP = 100;			//僵尸当前生命值
	public int maxHP = 100;				//僵尸满血生命值
	public int killScore = 5;			//击杀僵尸得分
	public AudioClip zombieHurtAudio;	//僵尸受伤音效

	//僵尸是否存活
	public bool IsAlive {
		get {
			return currentHP > 0;
		}
	}

	//僵尸受到攻击
	public void TakeDamage(int damage,PhotonPlayer attacker){
		if (!IsAlive)
			return;
		//僵尸生命值管理由MasterClient处理
		if (PhotonNetwork.isMasterClient) {
			currentHP -= damage;
			if (currentHP <= 0 && attacker!=null) {
				GameManager.gm.AddScore (killScore, attacker);
				currentHP = 0;
			}
			//使用RPC,更新所有客户端该僵尸的生命值
			photonView.RPC ("UpdateHP", PhotonTargets.All, currentHP);
			//使用RPC,让所有客户端播放僵尸受伤音效
			if (zombieHurtAudio != null)
				photonView.RPC ("PlayZombieHurtAudio", PhotonTargets.All);
		}
	}

	//RPC函数，更新僵尸生命值
	[PunRPC]
	void UpdateHP(int newHP)
	{
		currentHP = newHP;
	}

	//RPC函数，播放僵尸受伤音效
	[PunRPC]
	void PlayZombieHurtAudio()
	{
		AudioSource.PlayClipAtPoint (zombieHurtAudio, transform.position);
	}
}
