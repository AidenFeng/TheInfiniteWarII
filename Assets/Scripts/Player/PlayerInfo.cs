using System.Collections;
using System;

public class PlayerInfo:IComparable<PlayerInfo>{	//PlayerInfo实现IComparable接口
	public string playerName;	//玩家姓名
	public int playerScore;		//玩家得分
		
	//无参构造函数
	public PlayerInfo(){
	}

	//构造函数：玩家姓名，玩家得分
	public PlayerInfo(string _playerName,int _playerScore){
		playerName = _playerName;
		playerScore = _playerScore;
	}

	//实现IComparable接口的CompareTo函数，完成PlayerInfo对象的比较
	public int CompareTo(PlayerInfo other){
		if (this.playerScore > other.playerScore)
			return 1;
		else if (this.playerScore == other.playerScore)
			return 0;
		else
			return -1;
	}
}
