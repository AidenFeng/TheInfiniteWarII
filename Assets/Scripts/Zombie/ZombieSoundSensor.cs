using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSoundSensor : MonoBehaviour {

	public float Range = 15.0f;				//僵尸听觉范围
	public float sensorInterval = 1.0f;		//僵尸听觉感知时间间隔

	private float trigerTime = 0.0f;

	private Transform sensorTransform;
	private Transform nearestPlayer;

	void Start()
	{
		sensorTransform = transform;
	}

	//每隔一段时间僵尸使用听觉感知附近
	void FixedUpdate()
	{
		if (trigerTime >= sensorInterval) {
			trigerTime = 0;
			UpdatePlayerList ();
		}
		trigerTime += Time.deltaTime;

	}

	//更新僵尸听觉范围内的玩家
	void UpdatePlayerList()
	{
		nearestPlayer = null;
		GameObject[] playerObjList = GameObject.FindGameObjectsWithTag ("Player");
		float min = float.MaxValue;
		foreach (GameObject p in playerObjList) 
		{
			PlayerHealth ph = p.GetComponent<PlayerHealth> ();
			if (ph != null && ph.isAlive)
			{
				float dist = Vector3.Distance (p.transform.position, sensorTransform.position);
				if (dist < Range && dist < min) {
					min = dist;
					nearestPlayer = p.transform;	//设置离僵尸最近的玩家作为僵尸的追踪对象
				}
					
			}
		}
	}

	//获取僵尸听觉范围内，离僵尸最近的玩家对象
	public Transform getNearestPlayer()
	{
		return nearestPlayer;
	}
}
