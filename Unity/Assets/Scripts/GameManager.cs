using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerIOClient;

public class GameManager : MonoBehaviour
{
	[SerializeField] private GameObject target;
	[SerializeField] private Text msgTxt;

	private const string RoomType = "AE2";
	private const string StartedKey = "started";
	private const string StartedDefaultValue = "0";
	private Connection pioconnection;
	private List<Message> msgList = new List<Message>();
	private bool hasJoinedRoom = false;
	private bool isWaitingSelfMsg;
	private string userId;

	private void Start()
	{
		Application.runInBackground = true;

		// Create a random userid 
		System.Random random = new System.Random();
		userId = "Guest" + random.Next(0, 10000);

		Debug.Log("Starting");

		PlayerIO.Authenticate(
			"demo-2tauqt5dkgv1acesbnzlg",            //Your game id
			"public",                               //Your connection id
			new Dictionary<string, string> {        //Authentication arguments
				{ "userId", userId },
			},
			null,                                   //PlayerInsight segments
			delegate (Client client) {
				Debug.Log("Successfully connected to Player.IO");
				SetMsgTxt("Successfully connected to Player.IO");

				target.transform.Find("NameTag").GetComponent<TextMesh>().text = userId;
				target.transform.name = userId;

				Debug.Log("Create ServerEndpoint");
				// Comment out the line below to use the live servers instead of your development server
				client.Multiplayer.DevelopmentServer = new ServerEndpoint("localhost", 8184);

				client.Multiplayer.ListRooms(RoomType, null, 200, 0,
					delegate (RoomInfo[] roomInfos)
					{
						var list = new List<RoomInfo>();
						foreach (var r in roomInfos)
                        {
							foreach (var kv in r.RoomData)
								Debug.Log("kv " + kv.Key + " | " + kv.Value);
							if (r.RoomData.TryGetValue(StartedKey, out var value) && value == StartedDefaultValue)
                            {
								list.Add(r);
							}
						}
						Debug.LogError($"There are {roomInfos.Length} rooms and {list.Count} avaiable");
						if (list.Count == 0)
						{
							CreateJoinRoom(client);
						}
						else
						{
							random = new System.Random(Time.frameCount);
							var roomInfo = list[random.Next(list.Count)];
							CreateJoinRoom(client, roomInfo.Id);
						}
					},
					delegate (PlayerIOError error)
					{
						Debug.Log("Error connecting: " + error.ToString());
						SetMsgTxt(error.ToString());
					}
				);
				
			},
			delegate (PlayerIOError error) {
				Debug.Log("Error connecting: " + error.ToString());
				SetMsgTxt(error.ToString());
			}
		);

	}
	private void FixedUpdate()
	{
		// process message queue
		foreach (Message m in msgList)
		{
			var playerId = string.Empty;
			switch (m.Type)
			{
				case "PlayerJoined":
					GameObject newplayer = GameObject.Instantiate(target) as GameObject;
					newplayer.transform.position = new Vector3(m.GetFloat(1), 0, m.GetFloat(2));
					newplayer.name = m.GetString(0);
					newplayer.transform.Find("NameTag").GetComponent<TextMesh>().text = m.GetString(0);
					break;
				case "PlayerLeft":
					playerId = m.GetString(0);
					Debug.LogError(playerId + " left room");
					// remove characters from the scene when they leave
					GameObject playerd = GameObject.Find(playerId);
					Destroy(playerd);
					SetMsgTxt(playerId != userId ? "Win" : "Lose");
					break;
				case "Move":
					playerId = m.GetString(0);
					if (playerId == userId)
						isWaitingSelfMsg = false;
					GameObject upplayer = GameObject.Find(playerId);
					upplayer.transform.position = new Vector3(m.GetInt(1), 0, m.GetInt(2));
					break;
			}
		}

		if (msgList.Count > 0)
			msgList.Clear();
	}

    private void Update()
    {
		if (hasJoinedRoom)
			CheckInput();
	}

    private void OnDestroy()
    {
		if (pioconnection != null)
			pioconnection.Disconnect();

	}


    private void CreateJoinRoom(Client client, string roomId = null)
	{
		Debug.Log("CreateJoinRoom " + roomId);
		var roomData = new Dictionary<string, string>()
		{
			{"mapId", "123" },
			{StartedKey, StartedDefaultValue }
		};
		//Create or join the room 
		client.Multiplayer.CreateJoinRoom(
			roomId,                             //Room id. If set to null a random roomid is used
			RoomType,                           //The room type started on the server
			true,                               //Should the room be visible in the lobby?
			roomData,
			null,
			delegate (Connection connection) {
				Debug.Log("Joined Room.");
				SetMsgTxt("Joined Room.");
				// We successfully joined a room so set up the message handler
				pioconnection = connection;
				pioconnection.OnMessage += (sender, m) => msgList.Add(m);
				hasJoinedRoom = true;
			},
			delegate (PlayerIOError error) {
				Debug.Log("Error Joining Room: " + error.ToString());
				SetMsgTxt(error.ToString());
			}
		);
	}

	private void Move(int curX, int curY, int targetX, int targetY)
    {
		Debug.Log("Move");
		isWaitingSelfMsg = true;
		var code = GetCode(1, 0, 0);
		pioconnection.Send("Move", curX, curY, targetX, targetY, code);

	}

	private int GetCode(int action, int dam1, int dam2)
    {
		return dam2 + 1000 * dam1 + 1000 * 100 * action;
	}

	private void CheckInput()
	{
		if (Application.isEditor)
		{
			if (Input.GetKeyDown(KeyCode.W))
			{
				if (!isWaitingSelfMsg)
				{
					if (target.transform.position.x == 0)
						Move(4, 2, 0, 0);
					else
						Move(0, 0, 0, 0);
				}
				else
				{
					Debug.LogError("isWaitingSelfMsg");
				}
			}
		}
		else
		{
			if (Input.GetKeyDown(KeyCode.P))
			{
				if (!isWaitingSelfMsg)
				{
					if (target.transform.position.x == 1)
						Move(2, 3, 0, 0);
					else
						Move(1, 1, 0, 0);
				}
				else
				{
					Debug.LogError("isWaitingSelfMsg");
				}
			}
		}
	}

	private void SetMsgTxt(string value) => msgTxt.text = value;
}
