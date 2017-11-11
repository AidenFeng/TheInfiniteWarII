using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerMove : MonoBehaviour {
	public float moveSpeed = 6.0f;		//玩家移动速度
	public float rotateSpeed = 10.0f;	//玩家转向速度
	public float jumpVelocity = 5.0f;	//玩家跳跃速度

	float minMouseRotateX = -45.0f;		//摄像机旋转角度的最小值
	float maxMouseRotateX = 45.0f;		//摄像机旋转角度的最大值
	float mouseRotateX;					//当前摄像机在X轴的旋转角度
	bool isGrounded;					//玩家是否在地面上

	Camera myCamera;					
	Animator anim;
	Rigidbody rigid;
	CapsuleCollider capsuleCollider;
	PlayerHealth playerHealth;

	//初始化，获取组件，并计算相关值
	void Start(){
		myCamera = Camera.main;											//获取摄像机组件
		mouseRotateX = myCamera.transform.localEulerAngles.x;			//将当前摄像机在X轴的旋转角度赋值给mouseRotateX
		anim = GetComponent<Animator> ();								//获取玩家动画控制器组件
		rigid = GetComponent<Rigidbody> ();								//获取玩家刚体组件
		capsuleCollider = GetComponent<CapsuleCollider> ();				//获取玩家胶囊体碰撞体
		playerHealth = GetComponent<PlayerHealth> ();					//获取玩家PlayerHealth脚本
	}

	//每隔固定时间执行一次，用于物理模拟
	void FixedUpdate(){
		if (!playerHealth.isAlive)	//如果玩家已死亡，结束函数执行
			return;
		CheckGround();				//判断玩家是否位于地面
		if (isGrounded == false)				
			anim.SetBool ("isJump", false);		
	}

	//判断玩家是否位于地面，更新PlayerMove类的isGrounded字段
	void CheckGround()
	{            
		RaycastHit hitInfo;
		float shellOffset = 0.01f;
		float groundCheckDistance = 0.01f;
		Vector3 currentPos = transform.position;
		currentPos.y += capsuleCollider.height / 2f;
		if (Physics.SphereCast (currentPos, capsuleCollider.radius * (1.0f - shellOffset), Vector3.down, out hitInfo,
			((capsuleCollider.height / 2f) - capsuleCollider.radius) + groundCheckDistance, ~0, QueryTriggerInteraction.Ignore)) {
			isGrounded = true;
		} else {
			isGrounded = false;
		}

	}

	//跳跃函数
	void Jump(bool isGround){
		//当玩家按下跳跃键，并且玩家在地面上时执行跳跃相关函数
		if (CrossPlatformInputManager.GetButtonDown ("Jump") && isGround) {			
			//给玩家刚体组件添加向上的作用力，以改变玩家的运动速度，改变值为jumpVelocity
			rigid.AddForce (Vector3.up * jumpVelocity, ForceMode.VelocityChange);	
			anim.SetBool ("isJump", true);		//播放玩家跳跃动画
		} else
			anim.SetBool ("isJump", false);		
	}

	//每帧执行一次，用于获取玩家输入并控制角色的行为
	void Update()
	{
		if (!playerHealth.isAlive) 	//如果玩家已死亡，结束函数执行
			return;
		float h = CrossPlatformInputManager.GetAxisRaw ("Horizontal");	//获取玩家水平轴上的输入
		float v = CrossPlatformInputManager.GetAxisRaw ("Vertical");	//获取玩家垂直轴上的输入
		Move (h, v);		//根据玩家的输入控制角色移动
		float rv = CrossPlatformInputManager.GetAxisRaw ("Mouse X");	//获取玩家鼠标垂直轴上的移动
		float rh = CrossPlatformInputManager.GetAxisRaw ("Mouse Y");	//获取玩家鼠标水平轴上的移动
		Rotate (rh, rv);	//根据玩家的鼠标输入控制角色转向
		Jump (isGrounded);	//玩家的跳跃函数
	}

	//角色移动函数
	void Move(float h,float v){
		//玩家以moveSpeed的速度进行平移
		transform.Translate ((Vector3.forward * v + Vector3.right * h) * moveSpeed * Time.deltaTime);
		if (h != 0.0f || v != 0.0f) {
			anim.SetBool ("isMove", true);		//播放玩家奔跑动画
		} else
			anim.SetBool ("isMove", false);
	}
	//角色转向函数
	void Rotate(float rh,float rv){
		transform.Rotate (0, rv * rotateSpeed, 0);	//鼠标水平轴上的移动控制角色左右转向
		mouseRotateX -= rh * rotateSpeed;			//计算当前摄像机的旋转角度
		mouseRotateX = Mathf.Clamp (mouseRotateX, minMouseRotateX, maxMouseRotateX);	//将旋转角度限制在miniMouseRotateX与MaxiMouseRotateY之间
		myCamera.transform.localEulerAngles = new Vector3 (mouseRotateX, 0.0f, 0.0f);	//设置摄像机的旋转角度
	}
}






















