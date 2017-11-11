using UnityEngine;
using System.Collections;
using Photon;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerShoot : PunBehaviour {

	public int shootingDamage = 10;				//射击伤害
	public float shootingRange = 50.0f;			//射击范围
	public float timeBetweenShooting = 0.2f;	//射击间隔

	public Transform gunTransform;			//枪械对象的Transform属性
	public Transform gunBarrelEnd;			//枪口对象的Transform属性
	public GameObject gunShootingEffect;	//枪口射击效果
	public GameObject bulletEffect;			//子弹爆炸效果
	public AudioClip shootingAudio;			//枪械射击音效
	[HideInInspector]
	public Vector3 shootingPosition 		//枪械射击位置
		= new Vector3(-0.238f,1.49f,0.065f);

	PlayerHealth playerHealth;
	bool isShooting;
	Ray ray;
	RaycastHit hitInfo;
	float timer;

	//初始化
	void Start(){ 
		playerHealth = GetComponent<PlayerHealth> ();	//获取玩家PlayerHealth组件
		timer = 0.0f;									//初始化玩家与上次射击的间隔
	}

	//每帧执行，处理射击逻辑
	void Update () {
		if (!photonView.isMine || !playerHealth.isAlive)	//如果玩家对象不属于本地客户端，或者玩家已死亡，解说函数执行
			return;
		timer += Time.deltaTime;	//更新与上次射击的间隔
		if (CrossPlatformInputManager.GetButton("Fire1") && timer >= timeBetweenShooting) {	//玩家按下射击键，与上次射击的间隔超过射击间隔
			timer = 0.0f;			//将玩家与上次射击的间隔清零
			if (GameManager.gm.state == GameManager.GameState.Playing)						//如果当前游戏状态处于游戏进行中
				photonView.RPC ("Shoot", PhotonTargets.MasterClient, PhotonNetwork.player);	//使用RPC,调用MasterClient的Shoot函数，函数参数为发起射击的玩家
		}
	}

	/**RPC函数，执行射击逻辑
	 * 该函数只能由MasterClient调用
	 */
	[PunRPC]
	void Shoot(PhotonPlayer attacker){
		if (!PhotonNetwork.isMasterClient)		//如果玩家不是MasterClient，结束函数的执行
			return;
		ray.origin = shootingPosition+transform.position;	//设置射击射线的起始端点
		ray.direction = gunTransform.forward;				//设置射击射线的方向
		Vector3 bulletEffectPosition;						//子弹爆炸效果的位置

		//发出射击射线，判断是否击中物体
		if (Physics.Raycast (ray, out hitInfo, shootingRange)) {	//如果射线击中游戏对象
			GameObject go = hitInfo.collider.gameObject;			//获取被击中的游戏对象
			if (go.tag == "Player") {								//如果击中玩家
				PlayerHealth playerHealth = go.GetComponent<PlayerHealth> ();
				if (playerHealth.team != GetComponent<PlayerHealth> ().team) {	//如果被击中玩家队伍与攻击者玩家队伍不同
					playerHealth.TakeDamage (shootingDamage, attacker);			//被击中玩家扣血
				}
			} else if (go.tag == "Zombie") {						//如果击中僵尸
				ZombieHealth zh = go.GetComponent<ZombieHealth> ();
				if (zh != null) {
					zh.TakeDamage (shootingDamage, attacker);		//僵尸扣血
				}
			}
			bulletEffectPosition = hitInfo.point;					//击中游戏对象，子弹爆炸效果的位置在击中点
		}else
			bulletEffectPosition = ray.origin + shootingRange * ray.direction;	//如果未击中，子弹爆炸效果为子弹失效的位置
		photonView.RPC ("ShootEffect", PhotonTargets.All, bulletEffectPosition);//使用RPC，调用所有玩家对象的ShootEffect函数，显示射击效果
	}

	//RPC函数，显示射击效果
	[PunRPC]
	void ShootEffect(Vector3 bulletEffectPosition){
		AudioSource.PlayClipAtPoint (shootingAudio, transform.position);	//播放射击音效
		if (gunShootingEffect != null && gunBarrelEnd != null) {			//播放枪口射击效果				
			(Instantiate (gunShootingEffect, 
				gunBarrelEnd.position, 
				gunBarrelEnd.rotation) as GameObject).transform.parent = gunBarrelEnd;
		}
		if (bulletEffect != null) {											//播放子弹爆炸效果			
			Instantiate (bulletEffect, 
				bulletEffectPosition, 
				Quaternion.identity);
		}
	}
}
