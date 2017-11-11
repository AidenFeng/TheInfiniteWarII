using UnityEngine;
using System.Collections;

public class SelfRotate : MonoBehaviour {

	public float rotateSpeed = 5.0f;	//自转速度
			
	Transform myTransform;	//游戏对象Transform组件
	Vector3 vec3 ;			//自转向量

	void Start () {
		myTransform = transform;	//初始化游戏对象Transform组件
	}

	//Update函数中完成自转
	void Update () {
		vec3.x = 0.0f;
		vec3.y = rotateSpeed * Time.deltaTime;	//绕y轴的旋转度数
		vec3.z = 0.0f;
		myTransform.Rotate (vec3, Space.World);	//游戏对象以世界坐标轴旋转
	}
}
