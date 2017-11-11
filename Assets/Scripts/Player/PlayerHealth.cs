using UnityEngine;
using System.Collections;
using Photon;

public class PlayerHealth : PunBehaviour{

	public int killScore = 10;				//玩家对象被击杀增加击杀者的分数
	public int maxHP = 100;					//玩家对象满血血量
	public GameObject gun;					//玩家对象的枪械对象
	public float respawnTime = 5.0f;		//玩家对象死亡后重生时间
	public float invincibleTime = 3.0f;		//玩家对象无敌时间

	[HideInInspector]public int team;			//玩家对象队伍
	[HideInInspector]public bool isAlive;		//玩家对象是否存活
	[HideInInspector]public int currentHP;		//玩家对象当前生命值
	[HideInInspector]public bool invincible;	//玩家对象是否无敌

	float timer;
	Animator anim;
	Rigidbody rigid;
	Collider colli;

	//初始化
	void Start () {
		init ();	//初始化玩家对象生命值相关属性
		anim = GetComponent<Animator> ();			//获取玩家对象动画控制器
		rigid = GetComponent<Rigidbody> ();			//获取玩家对象刚体组件
		colli = GetComponent<CapsuleCollider> ();	//获取玩家对象胶囊碰撞体
		if (!photonView.isMine) return;				//如果不是本地玩家对象，结束函数运行
		photonView.RPC ("UpdateHP", PhotonTargets.Others, currentHP);				//使用RPC，更新其他客户端中该玩家对象当前血量
		if (PhotonNetwork.player.customProperties ["Team"].ToString () == "Team1")	//设置玩家对象队伍
			team = 1;
		else
			team = 2;
		photonView.RPC ("SetTeam", PhotonTargets.Others, team);		//使用RPC，设置其他客户端中该玩家对象的队伍
	}

	//初始化玩家对象生命值相关属性
	void init(){
		currentHP = maxHP;
		isAlive = true;		
		timer = 0.0f;
		invincible = true;
	}

	//每帧执行一次，检查玩家无敌状态
	void Update () {
		if (!photonView.isMine)		//不是本地玩家对象，结束函数执行
			return;
		timer += Time.deltaTime;	//累加玩家对象的无敌时间
		if (timer > invincibleTime && invincible == true)					//使用RPC，设置所有客户端该玩家对象的无敌状态
			photonView.RPC ("SetInvincible", PhotonTargets.All, false);
		else if (timer <= invincibleTime && invincible == false)
			photonView.RPC ("SetInvincible", PhotonTargets.All, true);
	}

	//RPC函数，设置玩家的无敌状态
	[PunRPC]
	void SetInvincible(bool isInvincible){
		invincible = isInvincible;
	}

	//玩家扣血函数，只有MasterClient可以调用
	public void TakeDamage(int damage,PhotonPlayer attacker){
		if (!isAlive || invincible)				//玩家死亡或者无敌，不执行扣血函数
			return;
		if (PhotonNetwork.isMasterClient) {		//MasterClient调用
			currentHP -= damage;				//玩家扣血
			photonView.RPC ("UpdateHP", PhotonTargets.All, currentHP);	//更新所有客户端，该玩家对象的生命值
			if (currentHP <= 0 && attacker!=null) {					//如果玩家受到攻击后死亡
				GameManager.gm.AddScore (killScore, attacker);		//击杀者增加分数
			}
		}
	}

	//玩家加血函数
	public void requestAddHP(int value)
	{
		photonView.RPC ("AddHP", PhotonTargets.MasterClient, value);	//使用RPC,向MasterClient发起加血请求
	}

	//RPC函数，增加玩家血量
	[PunRPC]
	public void AddHP(int value)
	{
		if (!PhotonNetwork.isMasterClient)		//加血函数只能由MasterClient调用
			return;
		if (!isAlive || currentHP == maxHP)		//玩家已死亡，或者玩家满血，不执行加血逻辑
			return;
		currentHP += value;				//玩家加血
		if (currentHP > maxHP) {		//加血后，玩家生命值不能超过最大生命值
			currentHP = maxHP;
		}
		photonView.RPC ("UpdateHP", PhotonTargets.All, currentHP);	//使用RPC，更新所有客户端，该玩家对象的血量
	}


	//RPC函数，更新玩家血量
	[PunRPC]
	void UpdateHP(int newHP)
	{
		currentHP = newHP;		//更新玩家血量
		if (currentHP <= 0) {	//如果玩家已死亡
			isAlive = false;
			if (photonView.isMine) {					//如果是本地客户端
				anim.SetBool ("isDead", true);			//播放玩家死亡动画
				Invoke ("PlayerSpawn", respawnTime);	//使用invoke函数，复活玩家
			}
			rigid.useGravity = false;		//禁用玩家重力
			colli.enabled = false;			//禁用玩家碰撞体
			gun.SetActive (false);			//禁用玩家枪械
			anim.applyRootMotion = true;	//玩家位置与朝向受动画影响
			GetComponent<IKController> ().enabled = false;	//禁用IK
		}
	}

	//玩家复活函数
	void PlayerSpawn(){
		photonView.RPC ("PlayerReset", PhotonTargets.All);	//使用RPC，初始化复活时的玩家属性
		Transform spawnTransform;
		int rand = Random.Range (0, 4);						//随机获得玩家复活位置
		if (PhotonNetwork.player.customProperties ["Team"].ToString () == "Team1")
			spawnTransform = GameManager.gm.teamOneSpawnTransform [rand];
		else
			spawnTransform = GameManager.gm.teamTwoSpawnTransform [rand];
		transform.position = spawnTransform.position;		//玩家在随机位置复活
		transform.rotation = Quaternion.identity;
	}

	//RPC函数，初始化复活时的玩家属性
	[PunRPC]
	void PlayerReset(){
		init ();		//初始化玩家血量与无敌状态
		rigid.useGravity = true;			//启用玩家重力
		colli.enabled = true;				//启用玩家碰撞体
		gun.SetActive (true);				//启用玩家枪械
		anim.SetBool ("isDead", false);		//播放玩家停驻动画
		anim.applyRootMotion = false;		//玩家位置与朝向不受动画影响
		GetComponent<IKController> ().enabled = true;	//启用IK
	}

	//RPC函数，设置玩家队伍
	[PunRPC]
	void SetTeam(int newTeam){
		team = newTeam;
	}

}
