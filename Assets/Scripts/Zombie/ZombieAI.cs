using UnityEngine;
using System.Collections;
using Photon;
public class ZombieAI : PunBehaviour {

	public enum FSMState	//僵尸的有限状态机枚举
	{
		Wander,	//游荡
		Track,	//追踪
		Attack,	//攻击
		Dying,	//死亡
		Dead	//死亡后
	}

	public float currentSpeed = 0.0f;		//僵尸当前速度
	public float wanderSpeed = 0.9f;		//僵尸游荡速度
	public float trackingSpeed = 4.0f;		//僵尸追踪速度
	public float wanderScope = 15.0f;		//游荡状态下，僵尸随机选择游荡目标的位置
	public float attackRange = 1.5f;		//僵尸攻击距离，当玩家与僵尸距离小于该值时，僵尸攻击玩家
	public float attackFieldOfView = 60.0f;	//僵尸攻击夹角
	public float attackInterval = 0.8f;		//僵尸攻击间隔
	public int attackDamage = 10;			//僵尸攻击伤害
	public float disappearTime = 3.0f;		//僵尸死亡后的消失时间

	public FSMState curState;				//僵尸当前状态
	public AudioClip zombieAttackAudio;

	private Vector3 previousPos = Vector3.zero;	//僵尸上一次停留的位置
	private float stopTime = 0;					//僵尸的停留时间
	private float attackTime = 0.0f;			//僵尸攻击计时器
	private float disappearTimer = 0.0f;		//僵尸尸体消失计时器
	private bool disappeared = false;			//僵尸尸体是否消失

	private Transform zombieTransform;			//僵尸的Transform组件
	private Animator animator;					//动画控制器组件
	private NavMeshAgent agent;					//导航代理组件
	private ZombieHealth zombieHealth;			//僵尸的生命值管理组件
	private ZombieSoundSensor zombieSoundSensor;//僵尸感知器组件
	private ZombieRender zombieRender;			//僵尸渲染器控制组件

	private Transform targetPlayer;				//僵尸感知范围内的玩家

	//初始化
	void OnEnable()
	{
		zombieTransform = transform;					//获取Transform组件
		animator = GetComponent<Animator>();			//获取动画管理器组件
		agent = GetComponent<NavMeshAgent>();			//获取NavMeshAgent组件
		//agent.updateRotation = false;
		//agent.updatePosition = false;

		zombieHealth = GetComponent<ZombieHealth> ();	//获取僵尸生命值管理组件
		zombieSoundSensor = GetComponentInChildren<ZombieSoundSensor> ();	//获取僵尸感知器组件
		zombieRender = GetComponent<ZombieRender>();	//获取僵尸渲染器控制组件

		targetPlayer = null;							//僵尸初始化时，未发现玩家，该值设为null
		curState = FSMState.Wander;						//初始化僵尸状态
	}

	//僵尸死亡后，发起RPC请求，禁用所有客户端的僵尸对象
	public void requestDisable(){
		photonView.RPC ("DisableZombie", PhotonTargets.All);
	}
	//RPC函数，禁用所有客户端的将是对象
	[PunRPC]
	void DisableZombie()
	{
		zombieTransform.gameObject.SetActive (false);
	}
		
	//定时更新僵尸状态机
	void FixedUpdate()
	{
		//僵尸的行为由MasterClient处理
		if (PhotonNetwork.isMasterClient) 
		{
			FSMUpdate ();
		}
	}

	//根据僵尸当前的状态调用相应的状态处理函数
	void FSMUpdate()
	{
		//如果当前状态不是游戏进行状态，且僵尸状态处于攻击和追踪状态，将僵尸状态切换为游荡状态
		if (GameManager.gm.state != GameManager.GameState.Playing) {
			if (curState == FSMState.Attack || curState == FSMState.Track) {
				curState = FSMState.Wander;
				animator.SetBool ("isAttack", false);
			}
		}
		switch (curState)
		{
		case FSMState.Wander: 
			UpdateWanderState();
			break;
		case FSMState.Track:
			UpdateTrackState();
			break;
		case FSMState.Attack:
			UpdateAttackState();
			break;
		case FSMState.Dying:
			UpdateDyingState();
			break;
		case FSMState.Dead:
			UpdateDeadState ();
			break;
		}

		//僵尸死亡，进入死亡状态
		if (curState != FSMState.Dead && curState != FSMState.Dying && !zombieHealth.IsAlive) 
		{
			curState = FSMState.Dying;
		}
	}

	//判断僵尸是否在一次导航中到达了目的地
	protected bool AgentDone()
	{
		return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
	}

	//RPC函数，僵尸狂暴化，设置僵尸的外观，使僵尸泛红
	[PunRPC]
	void ZombieSetCrazy(){
		zombieRender.SetCrazy ();
	}
	//RPC函数，僵尸回到正常状态，设置僵尸的外观恢复正常
	[PunRPC]
	void ZombieSetNormal(){
		zombieRender.SetNormal ();
	}

	//僵尸游荡状态处理函数
	void UpdateWanderState()
	{
		//在游戏进行状态，感知到周围有活着的玩家，进入追踪状态
		targetPlayer = zombieSoundSensor.getNearestPlayer ();
		if (targetPlayer != null && GameManager.gm.state == GameManager.GameState.Playing) {
			curState = FSMState.Track;
			agent.ResetPath ();
			return;
		}

		//如果没有目标位置，那么随机选择一个目标位置
		if (AgentDone ()) {
			Vector3 randomRange = new Vector3 ((Random.value - 0.5f) * 2 * wanderScope, 0, (Random.value - 0.5f) * 2 * wanderScope);
			Vector3 nextDestination = zombieTransform.position + randomRange;

			agent.destination = nextDestination;

		} 
		//如果在一个地方停留太久（某一次随机得到的目标位置无法到达，导致僵尸卡住），那么选择僵尸背后的一个位置当做下一个目标
		else if(stopTime > 1.0f)
		{
			Vector3 nextDestination = zombieTransform.position - zombieTransform.forward * (Random.value) * wanderScope;

			agent.destination = nextDestination;

		}

		//限制游荡的速度
		Vector3 targetVelocity = Vector3.zero;
		if (agent.desiredVelocity.magnitude > wanderSpeed) {
			targetVelocity = agent.desiredVelocity.normalized * wanderSpeed;
		} else {
			targetVelocity = agent.desiredVelocity;
		}
		agent.velocity = targetVelocity;
		currentSpeed = agent.velocity.magnitude;

		//设置动画状态
		animator.SetFloat("Speed", currentSpeed);

		//计算僵尸在某个位置附近的停留时间
		if (previousPos == Vector3.zero) 
		{
			previousPos = zombieTransform.position;
		}
		else 
		{
			Vector3 posDiff = zombieTransform.position - previousPos;
			if (posDiff.magnitude > 0.5) {
				previousPos = zombieTransform.position;
				stopTime = 0.0f;
			} else {
				stopTime += Time.deltaTime;
			}
		}

		//僵尸回到正常状态，使用PRC函数设置所有客户端该僵尸的外观
		if (zombieRender != null && zombieRender.isCrazy == true)
			photonView.RPC ("ZombieSetNormal", PhotonTargets.All);
	}

	//僵尸追踪状态处理函数
	void UpdateTrackState()
	{
		//如果僵尸周围没有玩家，僵尸进入游荡状态
		targetPlayer = zombieSoundSensor.getNearestPlayer ();
		if (targetPlayer == null) {
			curState = FSMState.Wander;
			agent.ResetPath ();
			return;
		}
		//如果玩家到僵尸的距离小于攻击距离，进入攻击状态
		if (Vector3.Distance(targetPlayer.position, zombieTransform.position)<=attackRange) {
			curState = FSMState.Attack;
			agent.ResetPath ();
			return;
		}

		//设置移动目标为玩家
		agent.SetDestination (targetPlayer.position);

		Vector3 targetVelocity = Vector3.zero;
		if (agent.desiredVelocity.magnitude > trackingSpeed) {
			targetVelocity = agent.desiredVelocity.normalized * trackingSpeed;
		} else {
			targetVelocity = agent.desiredVelocity;
		}
		agent.velocity = targetVelocity;
		currentSpeed = agent.velocity.magnitude;
			
		animator.SetFloat("Speed", currentSpeed);

		//僵尸进入狂暴状态，使用PRC函数设置所有客户端该僵尸的外观
		if (zombieRender != null && zombieRender.isCrazy == false)
			photonView.RPC ("ZombieSetCrazy", PhotonTargets.All);
	}

	//僵尸攻击状态处理函数
	void UpdateAttackState()
	{
		//如果僵尸附近没有玩家，返回游荡状态
		targetPlayer = zombieSoundSensor.getNearestPlayer ();
		if (targetPlayer == null) {
			curState = FSMState.Wander;
			agent.ResetPath ();
			animator.SetBool ("isAttack", false);
			return;
		}
		//如果僵尸距离敌人大于攻击距离，返回追踪状态
		if (Vector3.Distance(targetPlayer.position, zombieTransform.position)>attackRange) {
			curState = FSMState.Track;
			agent.ResetPath ();
			animator.SetBool ("isAttack", false);
			return;
		}

		PlayerHealth ph = targetPlayer.GetComponent<PlayerHealth> ();
		if (ph != null)
		{
			//计算僵尸的正前方和玩家的夹角，只有玩家在僵尸前方才能攻击
			Vector3 dir = targetPlayer.position - zombieTransform.position;
			float degree = Vector3.Angle (dir, zombieTransform.forward);

			if (degree < attackFieldOfView / 2 && degree > -attackFieldOfView / 2) {
				animator.SetBool ("isAttack", true);
				if (attackTime > attackInterval) {
					attackTime = 0;
					ph.TakeDamage (attackDamage, null);
					photonView.RPC ("PlayZombieAttackAudio", PhotonTargets.All);
				}
				attackTime += Time.deltaTime;
			} else {
				animator.SetBool ("isAttack", false);
				//否则，需要僵尸转身
				zombieTransform.LookAt(targetPlayer);
			}
		}

		//攻击状态下的敌人应当不断追踪玩家
		agent.SetDestination (targetPlayer.position);

		Vector3 targetVelocity = Vector3.zero;
		if (agent.desiredVelocity.magnitude > trackingSpeed) {
			targetVelocity = agent.desiredVelocity.normalized * trackingSpeed;
		} else {
			targetVelocity = agent.desiredVelocity;
		}
		agent.velocity = targetVelocity;
		currentSpeed = targetVelocity.magnitude;
		animator.SetFloat("Speed", currentSpeed);

		//僵尸进入狂暴状态，使用PRC函数设置所有客户端该僵尸的外观
		if (zombieRender != null && zombieRender.isCrazy == false)
			photonView.RPC ("ZombieSetCrazy", PhotonTargets.All);
	}

	//僵尸死亡
	void UpdateDyingState()
	{
		photonView.RPC ("ZombieDead", PhotonTargets.All);
		animator.SetBool ("isDead",true);
		disappearTimer = 0;
		disappeared = false;

		//僵尸回到正常状态，使用PRC函数设置所有客户端该僵尸的外观
		if (zombieRender != null && zombieRender.isCrazy == false)
			photonView.RPC ("ZombieSetNormal", PhotonTargets.All);
		curState = FSMState.Dead;
	}

	//僵尸死亡时，所有客户端改变相关的设置
	[PunRPC]
	void ZombieDead(){
		agent.ResetPath ();
		agent.enabled = false;
		animator.applyRootMotion = true;
		GetComponent<CapsuleCollider> ().enabled = false;
	}

	//僵尸死亡后，计算僵尸消失的时间
	void UpdateDeadState()
	{
		if (!disappeared) {

			if ( disappearTimer > disappearTime) {
				requestDisable ();
				disappeared = true;
			}
			disappearTimer += Time.deltaTime;
		}
	}

	//判断玩家是否在僵尸的攻击范围内
	bool inAttackRange(Vector3 pos)
	{
		Vector3 dir = pos - zombieTransform.position;
		if (dir.magnitude <= attackRange && (Vector3.Angle (dir, zombieTransform.forward) < attackFieldOfView)) {
			return true;
		}
		return false;
	}

	[PunRPC]
	void PlayZombieAttackAudio()
	{
		AudioSource.PlayClipAtPoint (zombieAttackAudio, transform.position);
	}





	/**僵尸自动生成
	 * 这里未用到
	 * 
	[PunRPC]
	void Born()
	{
		targetPlayer = null;
		curState = FSMState.Wander;
		zombieHealth.currentHP = zombieHealth.maxHP;
		agent.enabled = true;
		agent.ResetPath ();

		animator.applyRootMotion = false;
		GetComponent<CapsuleCollider> ().enabled = true;
		animator.SetBool("isDead",false);
		disappearTimer = 0;
		disappeared = false;
		curState = FSMState.Dead;
	}

	//将僵尸对象设为僵尸对象池的子对象
	public void requestSetGeneratorAsParent()
	{
		photonView.RPC ("setGeneratorAsParent", PhotonTargets.All);
	}
	[PunRPC]
	void setGeneratorAsParent()
	{
		zombieTransform.SetParent (GameObject.Find ("ZombieGenerator").transform);
	}
	*/
}