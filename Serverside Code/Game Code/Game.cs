using System;
using System.Collections.Generic;
using PlayerIO.GameLibrary;

namespace AncientEmpire {

	public class Player : BasePlayer {
		public int curX = 0;
		public int curY = 0;
		public int targetX = 0;
		public int targetY = 0;
		public int code;//aa-bbb-ccc (aa:action|bbb-dam1|ccc-dam2)
	}

	[RoomType("AE2")]
	public class GameCode : Game<Player> {
		private const string StartedKey = "started";
		private bool hasStarted;

		// This method is called when an instance of your the game is created
		public override void GameStarted() {
			// anything you write to the Console will show up in the 
			// output window of the development server
			Console.WriteLine("Game is started: " + RoomId + ", mapId: " + RoomData["mapId"]);
		}

		// This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("Game is closed: " + RoomId);
		}

		// This method is called whenever a player joins the game
		public override void UserJoined(Player player) {

			foreach(Player pl in Players) {
				if(pl.ConnectUserId != player.ConnectUserId) {
					pl.Send("PlayerJoined", player.ConnectUserId, 1, 1);
					player.Send("PlayerJoined", pl.ConnectUserId, pl.curX, pl.curY);
				}
			}
			//Console.WriteLine("UserJoined: " + GetPlayersCount());
			if (GetPlayersCount() >= 2)
			{
				RoomData[StartedKey] = "1";
				RoomData.Save();
				hasStarted = true;
			}
		}

        public override bool AllowUserJoin(Player player)
        {
			/*int maxplayers;
			if (!int.TryParse(RoomData["maxplayers"], out maxplayers))
			{
				maxplayers = 4; //Default
			}
			//Check if there's room for this player.
			if (GetPlayersCount() < maxplayers - 1)
			{
				return true;
			}
			return false;*/
			//Console.WriteLine("AllowUserJoin: started " + hasStarted + "," + RoomData[StartedKey] + ", count " + GetPlayersCount());
			return !hasStarted && GetPlayersCount() < 2;
		}

		// This method is called when a player leaves the game
		public override void UserLeft(Player player) {
			Broadcast("PlayerLeft", player.ConnectUserId);
			foreach (Player p in Players)
            {
				if (p != player)
				{
					p.Disconnect();
				}
            }
		}

		// This method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message) {
			switch(message.Type) {
				// called when a player clicks on the ground
				case "Move":
					player.curX = message.GetInt(0);
					player.curY = message.GetInt(1);
					Broadcast("Move", player.ConnectUserId, player.curX, player.curY);
					break;
			}
		}

		private int GetPlayersCount()
        {
			var c = 0;
			foreach (Player pl in Players) c++;
			return c;
        }
	}
}