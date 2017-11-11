using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon;
using UnityEngine.UI;
public class GameManager : PunBehaviour {
	public static GameManager gm;		//GameManager静态实例

	public enum GameState{		//游戏状态枚举
		PreStart,				//游戏开始前
		Playing,				//游戏进行中
		GameWin,				//游戏胜利
		GameLose,				//游戏失败
		Tie};					//平手
	public GameState state = GameState.PreStart;	//初始化游戏状态
	public Transform[] teamOneSpawnTransform;		//队伍1出生位置
	public Transform[] teamTwoSpawnTransform;		//队伍2出生位置
	public float checkplayerTime = 5.0f;			//检查玩家加载场景时间
	public float gamePlayingTime = 600.0f;			//游戏时间
	public float gameOverTime = 10.0f;				//游戏结束时间
	public float spawnTime = 5.0f;					//重生时间
	public int targetScore = 50;					//目标分数
	public Text timeLabel;							//倒计时时间显示
	public Text targetScoreLabel;					//目标分数显示

	//实时计分板
	public Text Team1RealTimeScorePanelScore;
	public Text Team2RealTimeScorePanelScore;

	public GameObject scorePanel;				//玩家得分榜
	public Text teamOneTotal;					//队伍1总分
	public Text teamTwoTotal;					//队伍2总分
	public GameObject[] teamOneScorePanel;		//队伍1成员得分面板
	public GameObject[] teamTwoScorePanel;		//队伍2成员得分面板
	public Text gameResult;						//游戏结束信息
	public Slider hpSlider;						//玩家血条
	public AudioClip gameStartAudio;			//游戏开始音效
	public AudioClip gameWinAudio;				//游戏胜利音效
	public AudioClip gameLoseAudio;				//游戏失败音效
	public AudioClip tieAudio;					//平手音效

	double startTimer = 0;			//倒计时开始时间
	double endTimer = 0;			//倒计时结束时间
	double countDown = 0;			//倒计时
	int loadedPlayerNum = 0;		//已加载场景的玩家个数
	int currentScoreOfTeam1 = 0;	//队伍1得分
	int currentScoreOfTeam2 = 0;	//队伍2得分
	const float photonCircleTime = 4294967.295f;	//Photon服务器循环时间

	Camera mainCamera;
	GameObject localPlayer = null;
	ExitGames.Client.Photon.Hashtable playerCustomProperties;
	PlayerHealth playerHealth;

	//初始化
	void Start () {
		gm = GetComponent<GameManager> ();		//初始化GameManager静态实例gm
		mainCamera = Camera.main;				//获取摄像机
		photonView.RPC ("ConfirmLoad", PhotonTargets.All);				//使用RPC,告知所有玩家有一名玩家已成功加载场景
		playerCustomProperties = new ExitGames.Client.Photon.Hashtable{ { "Score",0 } };	//初始化玩家得分
		PhotonNetwork.player.SetCustomProperties (playerCustomProperties);	
		targetScoreLabel.text = "Object：" + targetScore.ToString ();	//显示目标分数
		//初始化队伍得分
		currentScoreOfTeam1 = 0;										
		currentScoreOfTeam2 = 0;
		UpdateScores (currentScoreOfTeam1, currentScoreOfTeam2);		//更新玩家得分榜
		if (PhotonNetwork.isMasterClient)								//MasterClient设置游戏开始倒计时
			photonView.RPC ("SetTime", PhotonTargets.All, PhotonNetwork.time, checkplayerTime);
		gameResult.text = "";					//清空游戏结果
		scorePanel.SetActive (false);			//禁用玩家得分榜
	}

	//RPC函数，增加成功加载场景的玩家个数
	[PunRPC]
	void ConfirmLoad(){
		loadedPlayerNum++;
	}

	//每帧执行一次，更新倒计时，控制游戏状态
	void Update(){
		countDown = endTimer - PhotonNetwork.time;	//计算倒计时
		if (countDown >= photonCircleTime)			//防止entTimer值超过Photon服务器循环时间，确保倒计时能正确结束
			countDown -= photonCircleTime;
		UpdateTimeLabel ();							//更新倒计时的显示

		//游戏状态控制
		switch (state) {
		//如果游戏处于游戏开始前
		case GameState.PreStart:					
			if (PhotonNetwork.isMasterClient) {		//MasterClient检查倒计时和场景加载人数，控制游戏开始
				CheckPlayerConnected ();
			}
			break;
		//如果游戏处于游戏进行中
		case GameState.Playing:				
			hpSlider.value = playerHealth.currentHP;			//更新玩家生命值血条的显示
			#if(!UNITY_ANDROID)
			scorePanel.SetActive (Input.GetKey (KeyCode.Tab));	//PC使用Tab键显示玩家得分榜
			#endif
			if (PhotonNetwork.isMasterClient) {					//MasterClient检查游戏状态
				if (currentScoreOfTeam1 >= targetScore)			//队伍1达到目标分数，队伍1获胜
					photonView.RPC ("EndGame", PhotonTargets.All, "Team1",PhotonNetwork.time);
				else if (currentScoreOfTeam2 >= targetScore)	//队伍2达到目标分数，队伍2获胜
					photonView.RPC ("EndGame", PhotonTargets.All, "Team2",PhotonNetwork.time);
				else if (countDown <= 0.0f) {					//游戏倒计时结束，得分高的队伍获胜
					if (currentScoreOfTeam1 > currentScoreOfTeam2)		
						photonView.RPC ("EndGame", PhotonTargets.All, 
							"Team1", PhotonNetwork.time);
					else if (currentScoreOfTeam1 < currentScoreOfTeam2)
						photonView.RPC ("EndGame", PhotonTargets.All, 
							"Team2",PhotonNetwork.time);
					else                                        //双方得分相同，平手
						photonView.RPC ("EndGame", PhotonTargets.All, 
							"Tie",PhotonNetwork.time);
				}
			}
			break;
		case GameState.GameWin:		//游戏胜利状态，倒计时结束，退出游戏房间
			if (countDown <= 0)
				LeaveRoom ();
			break;
		case GameState.GameLose:	//游戏结束状态，倒计时结束，退出游戏房间
			if (countDown <= 0)
				LeaveRoom ();
			break;
		case GameState.Tie:
			if (countDown <= 0)		//平手状态，倒计时结束，退出游戏房间
				LeaveRoom ();
			break;
		}
	}

	/**IPunCallback回调函数，有玩家断开连接时（离开房间）调用
	 * MasterClient检查双方人数，更新玩家得分榜的显示
	 */
	public override void OnPhotonPlayerDisconnected(PhotonPlayer other){
		if (state != GameState.Playing)		//游戏状态不是游戏进行中，结束函数执行
			return;
		if (PhotonNetwork.isMasterClient) {	//MasterClient检查
			CheckTeamNumber ();				//检查两队人数
			//更新玩家得分榜
			photonView.RPC ("UpdateScores", PhotonTargets.All, currentScoreOfTeam1, currentScoreOfTeam2);
		}
	}

	/**检查加载场景的玩家个数
	 * 该函数只由MasterClient调用
	 */
	void CheckTeamNumber(){
		PhotonPlayer[] players = PhotonNetwork.playerList;		//获取房间内玩家列表
		int teamOneNum = 0, teamTwoNum = 0;						
		foreach (PhotonPlayer p in players) {					//遍历所有玩家，计算两队人数
			if (p.customProperties ["Team"].ToString () == "Team1")
				teamOneNum++;
			else
				teamTwoNum++;
		}
		//如果有某队伍人数为0，另一队获胜
		if (teamOneNum == 0)
			photonView.RPC ("EndGame", PhotonTargets.All, "Team2",PhotonNetwork.time);
		else if (teamTwoNum == 0)
			photonView.RPC ("EndGame", PhotonTargets.All, "Team1",PhotonNetwork.time);
	}

	//显示两队实时分数
	void UpdateRealTimeScorePanel()
	{
		string team1Title = string.Empty;
		string team2Title = string.Empty;

		team1Title = string.Format("Score：{0}",currentScoreOfTeam1);
		team2Title = string.Format("Score：{0}",currentScoreOfTeam2);

		Team1RealTimeScorePanelScore.text = team1Title;
		Team2RealTimeScorePanelScore.text = team2Title;
	}

	//显示倒计时时间
	void UpdateTimeLabel(){
		int minute = (int)countDown / 60;
		int second = (int)countDown % 60;
		timeLabel.text = minute.ToString ("00") + ":" + second.ToString ("00");
	}

	//检查所有玩家是否已经加载场景
	void CheckPlayerConnected(){
		if (countDown <=0.0f || loadedPlayerNum == PhotonNetwork.playerList.Length) {	//游戏开始倒计时结束，或者所有玩家加载场景
			startTimer = PhotonNetwork.time;								//游戏开始时间（使用Photon服务器的时间）
			photonView.RPC ("StartGame",PhotonTargets.All,startTimer);		//使用RPC，所有玩家开始游戏
		}
	}

	//RPC函数，开始游戏
	[PunRPC]
	void StartGame(double timer){
		SetTime(timer,gamePlayingTime);	//设置游戏进行倒计时时间
		gm.state = GameState.Playing;	//游戏状态切换到游戏进行状态
		InstantiatePlayer ();			//创建玩家对象
		AudioSource.PlayClipAtPoint(gameStartAudio, localPlayer.transform.position);	//播放游戏开始音效
	}

	//RPC函数，设置倒计时时间
	[PunRPC]
	void SetTime(double sTime,float dTime){
		startTimer = sTime;
		endTimer = sTime + dTime;
	}

	//生成玩家对象
	void InstantiatePlayer(){
		playerCustomProperties= PhotonNetwork.player.customProperties;	//获取玩家自定义属性
		//如果玩家属于队伍1，生成EthanPlayer对象
		if (playerCustomProperties ["Team"].ToString ().Equals ("Team1")) {	
			localPlayer = PhotonNetwork.Instantiate ("EthanPlayer", 
				teamOneSpawnTransform [(int)playerCustomProperties ["TeamNum"]].position, Quaternion.identity, 0);
		}
		//如果玩家属于队伍2，生成RobotPlayer对象
		else if (PhotonNetwork.player.customProperties ["Team"].ToString ().Equals ("Team2")) {
			localPlayer = PhotonNetwork.Instantiate ("RobotPlayer", 
				teamTwoSpawnTransform [(int)playerCustomProperties ["TeamNum"]].position, Quaternion.identity, 0);
		}
		localPlayer.GetComponent<PlayerMove> ().enabled = true;					//启用PlayerMove脚本，使玩家对象可以被本地客户端操控
		PlayerShoot playerShoot = localPlayer.GetComponent<PlayerShoot> ();		//获取玩家对象的PlayerShoot脚本
		playerHealth = localPlayer.GetComponent<PlayerHealth> ();				//获取玩家对象的PlayerHealth脚本
		hpSlider.maxValue = playerHealth.maxHP;									//设置显示玩家血量的Slider控件
		hpSlider.minValue = 0;
		hpSlider.value = playerHealth.currentHP;
		Transform tempTransform = localPlayer.transform;
		mainCamera.transform.parent = tempTransform;							//将场景中的摄像机设为玩家对象的子对象
		mainCamera.transform.localPosition = playerShoot.shootingPosition;		//设置摄像机的位置，为PlayerShoot脚本中的射击起始位置
		mainCamera.transform.localRotation = Quaternion.identity;				//设置摄像机的朝向
		for (int i = 0; i < tempTransform.childCount; i++) {					//将枪械对象设为摄像机的子对象
			if (tempTransform.GetChild (i).name.Equals ("Gun")) {
				tempTransform.GetChild (i).parent = mainCamera.transform;
				break;
			}
		}
	}

	/**玩家得分增加函数
	 * 该函数只由MasterClient调用
	 */
	public void AddScore(int killScore, PhotonPlayer p){
		if (!PhotonNetwork.isMasterClient)		//如果函数不是由MasterClient调用，结束函数执行
			return;
		int score = (int)p.customProperties ["Score"];		//获取击杀者玩家得分
		score += killScore;									//增加击杀者玩家得分
		playerCustomProperties = new ExitGames.Client.Photon.Hashtable{ { "Score",score } };
		p.SetCustomProperties (playerCustomProperties);
		if (p.customProperties ["Team"].ToString () == "Team1")
			currentScoreOfTeam1 += killScore;		//增加队伍1总分
		else
			currentScoreOfTeam2 += killScore;		//增加队伍2总分
		//使用RPC，更新所有客户端的玩家得分榜
		photonView.RPC ("UpdateScores",PhotonTargets.All,currentScoreOfTeam1,currentScoreOfTeam2);
	}

	//RPC函数，更新玩家得分榜
	[PunRPC]
	void UpdateScores(int teamOneScore,int teamTwoScore){
		//禁用所有显示玩家得分的条目
		foreach (GameObject go in teamOneScorePanel)
			go.SetActive (false);
		foreach (GameObject go in teamTwoScorePanel)
			go.SetActive (false);

		//更新队伍1、队伍2的总分
		currentScoreOfTeam1 = teamOneScore;		
		currentScoreOfTeam2 = teamTwoScore;
		PhotonPlayer[] players = PhotonNetwork.playerList;	//获取房间内所有玩家的信息
		List<PlayerInfo> teamOne = new List<PlayerInfo>();
		List<PlayerInfo> teamTwo = new List<PlayerInfo>();
		PlayerInfo tempPlayer;
		//遍历房间内所有玩家，将他们的得分根据他们的队伍放入对应的队伍列表中
		foreach (PhotonPlayer p in players) {
			tempPlayer = new PlayerInfo (p.name, (int)p.customProperties ["Score"]);
			if (p.customProperties ["Team"].ToString () == "Team1")
				teamOne.Add (tempPlayer);
			else
				teamTwo.Add (tempPlayer);
		}
		//分别对两队队伍列表排序，按照分数从大到小排序
		teamOne.Sort ();
		teamTwo.Sort ();
		Text[] texts;
		int length = teamOne.Count;
		//依次在玩家得分榜显示两队玩家得分，保证得分高的玩家在得分低的玩家之上
		for (int i = 0; i < length; i++) {
			texts = teamOneScorePanel [i].GetComponentsInChildren<Text> ();
			texts [0].text = teamOne [i].playerName;
			texts [1].text = teamOne [i].playerScore.ToString();
			teamOneScorePanel [i].SetActive (true);
		}
		length = teamTwo.Count;
		for (int i = 0; i < length; i++) {
			texts = teamTwoScorePanel [i].GetComponentsInChildren<Text> ();
			texts [0].text = teamTwo [i].playerName;
			texts [1].text = teamTwo [i].playerScore.ToString();
			teamTwoScorePanel [i].SetActive (true);
		}
		//显示两队得分
		teamOneTotal.text = "Team1：" + currentScoreOfTeam1.ToString ();
		teamTwoTotal.text = "Team2：" + currentScoreOfTeam2.ToString ();
		UpdateRealTimeScorePanel();		//更新实时得分榜
	}

	//游戏结束，更改客户端的游戏状态
	[PunRPC]
	void EndGame(string winTeam,double timer){
		//如果两队不是平手，游戏结束信息显示获胜队伍胜利
		if (winTeam != "Tie")
			gameResult.text = winTeam + " Wins!";
		if (winTeam == "Tie") 			//如果两队打平
		{	
			gm.state = GameState.Tie;	//游戏状态切换为平手状态
			AudioSource.PlayClipAtPoint (tieAudio, localPlayer.transform.position);	//播放平手音效
			gameResult.text = "Tie!";	//游戏结束信息显示"Tie!"表示平手
		} 
		else if (winTeam == PhotonNetwork.player.customProperties ["Team"].ToString ()) 	//如果玩家属于获胜队伍
		{
			gm.state = GameState.GameWin;		//游戏状态切换为游戏胜利状态
			//播放游戏胜利音效
			AudioSource.PlayClipAtPoint (gameWinAudio,localPlayer.transform.position);
		} 
		else //如果玩家属于失败队伍
		{
			gm.state = GameState.GameLose;		//游戏状态切换为游戏失败状态
			//播放游戏失败音效
			AudioSource.PlayClipAtPoint (gameLoseAudio, localPlayer.transform.position);
		}

		scorePanel.SetActive(true);		//游戏结束后，显示玩家得分榜
		SetTime (timer, gameOverTime);	//设置游戏结束倒计时时间
	}

	//本地玩家加分
	public void localPlayerAddHealth(int points){
		PlayerHealth ph = localPlayer.GetComponent<PlayerHealth> ();
		ph.requestAddHP (points);
	}

	//如果玩家断开与Photon服务器的连接，加载场景GameLobby
	public override void OnConnectionFail(DisconnectCause cause){
		PhotonNetwork.LoadLevel ("GameLobby");
	}

	//显示玩家得分榜，用于移动端的得分榜按钮，代替PC的Tab键
	public void ShowScorePanel(){
		scorePanel.SetActive (!scorePanel.activeSelf);
	}

	//离开房间函数
	public void LeaveRoom(){
		PhotonNetwork.LeaveRoom ();				//玩家离开游戏房间
		PhotonNetwork.LoadLevel ("GameLobby");	//加载场景GameLobby
	}
}
