using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Photon;

public class LobbyPanelController : PunBehaviour {

	public GameObject loginPanel;			//游戏登录面板
	public GameObject lobbyPanel;			//游戏大厅面板
	public GameObject userMessage;			//玩家昵称信息
	public Button backButton;				//返回按钮
	public GameObject lobbyLoadingLabel;	//游戏大厅加载提示信息
	public GameObject roomLoadingLabel;		//游戏房间加载提示信息
	public GameObject roomMessagePanel;		//房间信息面板
	public Button randomJoinButton;			//"随机进入房间"按钮
	public GameObject previousButton;		//"上一页"按钮
	public GameObject nextButton;			//"下一页"按钮
	public Text pageMessage;				//房间页数文本控件
	public GameObject createRoomPanel;		//创建房间面板
	public GameObject roomPanel;			//游戏房间面板

	private RoomInfo[] roomInfo;			//游戏大厅房间列表信息
	private int currentPageNumber;			//当前房间页
	private int maxPageNumber;				//最大房间页
	private int roomPerPage = 4;			//每页显示房间个数
	private GameObject[] roomMessage;		//游戏房间信息

	//当游戏大厅面板启用时调用，初始化信息
	void OnEnable(){
		currentPageNumber = 1;				//初始化当前房间页
		maxPageNumber = 1;					//初始化最大房间页	
		lobbyLoadingLabel.SetActive (true);	//启用游戏大厅加载提示信息
		roomLoadingLabel.SetActive (false);	//禁用游戏房间加载提示信息
		if(createRoomPanel!=null)
			createRoomPanel.SetActive (false);	//禁用创建房间面板

		//获取房间信息面板
		RectTransform rectTransform = roomMessagePanel.GetComponent<RectTransform> ();
		roomPerPage = rectTransform.childCount;		//获取房间信息面板的条目数

		//初始化每条房间信息条目
		roomMessage = new GameObject[roomPerPage];	
		for (int i = 0; i < roomPerPage; i++) {
			roomMessage [i] = rectTransform.GetChild (i).gameObject;
			roomMessage [i].SetActive (false);			//禁用房间信息条目
		}

		backButton.onClick.RemoveAllListeners ();		//移除返回按钮绑定的所有监听事件
		backButton.onClick.AddListener (delegate() {	//为返回按钮绑定新的监听事件
			PhotonNetwork.Disconnect();					//断开客户端与Photon服务器的连接
			loginPanel.SetActive(true);					//启用游戏登录面板
			lobbyPanel.SetActive(false);				//禁用游戏大厅面板
			userMessage.SetActive (false);				//禁用玩家昵称信息
			backButton.gameObject.SetActive (false);	//禁用返回按钮
		});
		if(roomPanel!=null)
			roomPanel.SetActive (false);					//禁用游戏房间面板
	}

	/**覆写IPunCallback回调函数，当玩家进入游戏大厅时调用
	 * 禁用游戏大厅加载提示
	 */
	public override void OnJoinedLobby(){
		lobbyLoadingLabel.SetActive (false);
	}
	/**覆写IPunCallback回调函数，当玩家进入游戏房间时调用
	 * 禁用游戏大厅面板，启用游戏房间面板
	 */
	public override void OnJoinedRoom(){
		lobbyPanel.SetActive (false);
		roomPanel.SetActive (true);
	}

	/**覆写IPunCallback回调函数，当客户端连接到MasterServer时调用
	 * 加入默认游戏大厅
	 * 效果等同于勾选PhotonServerSettings中的Auto-join Lobby
	public override void OnConnectedToMaster ()
	{
		PhotonNetwork.JoinLobby ();
	}
	*/

	/**覆写IPunCallback回调函数，当房间列表更新时调用
	 * 更新游戏大厅中房间列表的显示
	 */
	public override void OnReceivedRoomListUpdate(){
		roomInfo = PhotonNetwork.GetRoomList ();					//获取游戏大厅中的房间列表
		maxPageNumber = (roomInfo.Length - 1) / roomPerPage + 1;	//计算房间总页数
		if (currentPageNumber > maxPageNumber)		//如果当前页大于房间总页数时
			currentPageNumber = maxPageNumber;		//将当前房间页设为房间总页数
		pageMessage.text = currentPageNumber.ToString () + "/" + maxPageNumber.ToString ();	//更新房间页数信息的显示
		ButtonControl ();		//翻页按钮控制
		ShowRoomMessage ();		//显示房间信息

		if (roomInfo.Length == 0) {
			randomJoinButton.interactable = false;	//如果房间数为0，禁用"随机进入房间"按钮的交互功能
		} else
			randomJoinButton.interactable = true;	//如果房间数不为0，启用"随机进入房间"按钮的交互功能
	}

	//显示房间信息
	void ShowRoomMessage(){
		int start, end, i, j;
		start = (currentPageNumber - 1) * roomPerPage;			//计算需要显示房间信息的起始序号
		if (currentPageNumber * roomPerPage < roomInfo.Length)	//计算需要显示房间信息的末尾序号
			end = currentPageNumber * roomPerPage;
		else
			end = roomInfo.Length;

		//依次显示每条房间信息
		for (i = start,j = 0; i < end; i++,j++) {
			RectTransform rectTransform = roomMessage [j].GetComponent<RectTransform> ();
			string roomName = roomInfo [i].name;	//获取房间名称
			rectTransform.GetChild (0).GetComponent<Text> ().text = (i + 1).ToString ();	//显示房间序号
			rectTransform.GetChild (1).GetComponent<Text> ().text = roomName;				//显示房间名称
			rectTransform.GetChild (2).GetComponent<Text> ().text 						
				= roomInfo [i].playerCount + "/" + roomInfo [i].maxPlayers;					//显示房间人数
			Button button = rectTransform.GetChild (3).GetComponent<Button> ();				//获取"进入房间"按钮组件
			//如果游戏房间人数已满，或者游戏房间的Open属性为false（房间内游戏已开始），表示房间无法加入，禁用"进入房间"按钮
			if (roomInfo [i].playerCount == roomInfo [i].maxPlayers || roomInfo [i].open == false)
				button.gameObject.SetActive (false);
			//如果房间可以加入，启用"进入房间"按钮，给按钮绑定新的监听事件，加入此房间
			else {
				button.gameObject.SetActive (true);
				button.onClick.RemoveAllListeners ();
				button.onClick.AddListener (delegate() {
					ClickJoinRoomButton (roomName);
				});
			}
			roomMessage [j].SetActive (true);	//启用房间信息条目
		}
		//禁用不显示的房间信息条目
		while (j < 4) {
			roomMessage [j++].SetActive (false);
		}
	}

	//翻页按钮控制函数
	void ButtonControl(){
		//如果当前页为1，禁用"上一页"按钮；否则，启用"上一页"按钮
		if (currentPageNumber == 1)
			previousButton.SetActive (false);
		else
			previousButton.SetActive (true);
		//如果当前页等于房间总页数，禁用"下一页"按钮；否则，启用"下一页"按钮
		if (currentPageNumber == maxPageNumber)
			nextButton.SetActive (false);
		else
			nextButton.SetActive (true);
	}

	//"创建房间"按钮事件处理函数，启用创建房间面板
	public void ClickCreateRoomButton(){
		createRoomPanel.SetActive (true);
	}
	//"随机进入房间"按钮事件处理函数，玩家随机加入大厅中的房间，启用游戏房间加载提示信息
	public void ClickRandomJoinButton(){
		PhotonNetwork.JoinRandomRoom ();
		roomLoadingLabel.SetActive (true);
	}
	//"上一页"按钮事件处理函数
	public void ClickPreviousButton(){
		currentPageNumber--;		//当前房间页减一
		pageMessage.text = currentPageNumber.ToString () + "/" + maxPageNumber.ToString ();	//更新房间页数显示
		ButtonControl ();			//当前房间页更新，调动翻页控制函数
		ShowRoomMessage ();			//当前房间页更新，重新显示房间信息
	}
	//"下一页"按钮事件处理函数
	public void ClickNextButton(){
		currentPageNumber++;		//当前房间页加一
		pageMessage.text = currentPageNumber.ToString () + "/" + maxPageNumber.ToString ();	//更新房间页数显示
		ButtonControl ();			//当前房间页更新，调动翻页控制函数
		ShowRoomMessage ();			//当前房间页更新，重新显示房间信息
	}
	//"进入房间"按钮事件处理函数
	public void ClickJoinRoomButton(string roomName){
		PhotonNetwork.JoinRoom(roomName);	//根据房间名加入游戏房间
		roomLoadingLabel.SetActive (true);	//启用房间加载提示信息
	}
}
