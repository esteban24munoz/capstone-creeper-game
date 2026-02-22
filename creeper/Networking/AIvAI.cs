using Godot;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Collections.Generic;

public partial class AIvAI : Node2D
{
	private static string SOFTSERVE_URL = "https://softserve.harding.edu";
	private const string PLAYER_NAME = "LOTRAI";
	private const string PLAYER_TOKEN = "yV73X6Wtg_4rAnHEI2UP1Iw53F9XkuQ4Gr-tbfWo1-M";
	private const string PLAYER_EMAIL = "hconner@harding.edu";
	private const string EVENT = "mirror";
	private static System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
	AI ai = new AI();
	
	//Called when the node enters scene tree for first time
	public override void _Ready()
	{
		//GD.Print("Testing Action Parsing");
		//Vector2I from = new Vector2I(6,7);
		//Vector2I to = new Vector2I(5,6);
		//ParseAction(from, to);
		//string state = ".o..x..o.....xoo....x....x..x.....oxx..o....x.oo.o...xx.o.x.....x............oox....ox";
		//GD.Print("Testing Parse Function");
		//ParseState(state, ai.AIGame);
		//PlayAiVsAi();
		GD.Print("Testing stringify");
		StringifyState();
	}
	
	public async void PlayAiVsAi()
	{
		string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		//while (true)
		for (int i = 0; i < 1; i++)
		{
			var playStateObj = new
			{
				@event = EVENT,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};
			
			GD.Print("Getting the state");
			var playStateResponse = await client.PostAsJsonAsync(playStateUrl, playStateObj);
			GD.Print(playStateResponse.StatusCode);
			playStateResponse.EnsureSuccessStatusCode();

			if (playStateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
			{
				// No games waiting for move, wait and continue
				GD.Print("204: Sleep 5 seconds and try again");
				await Task.Delay(5000);
				continue;
			}

			var playStateJson = await playStateResponse.Content.ReadFromJsonAsync<JsonElement>();
			//GD.Print(playStateJson);
			string state = playStateJson.GetProperty("state").GetString();
			string action_id = playStateJson.GetProperty("action_id").ToString();

			GD.Print($"state:\t{state}");

			/***************************************************************************************
			Starting here, AI should take over. The code below is just a placeholder
			to test the Network functionality. The state of the game is still just a string.
			***************************************************************************************/
			
			GD.Print("Testing Parse Function");
			ai.currentPlayer = ai.AIGame.UpdateState(state);
			
			//Get a list of possible actions based on the state from the API
			var actionsResponse = await client.GetAsync($"{SOFTSERVE_URL}/state/{state}/actions");
			actionsResponse.EnsureSuccessStatusCode();

			var actionsJson = await actionsResponse.Content.ReadFromJsonAsync<JsonElement>();
			var actions = actionsJson.GetProperty("actions").EnumerateArray();

			var actionList = new List<string>();
			foreach (var act in actions)
			{
				actionList.Add(act.GetString());
				//GD.Print(act.GetString());
			}
			GD.Print("Count of available actions: " + actionList.Count);

			//Pick random action from list
			var random = new Random();
			string action = actionList[random.Next(actionList.Count)];

			// Simulate thinking time
			await Task.Delay(2000);

			GD.Print($"action chosen:\t{action}");
			
			//Get move from AI here
			GD.Print("Testing Action Parsing");
			Vector2I from = new Vector2I(6,7);
			Vector2I to = new Vector2I(5,6);
			string parsedAction = ParseAction(from, to);
			
			/************************************************************
			End of placeholder AI
			************************************************************/
			
			var submitActionObj = new
			{
				action = action,
				action_id = action_id,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};

			var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
			submitResponse.EnsureSuccessStatusCode();
		}
	}
	
	private void _on_back_btn_pressed()
	{
		GetTree().ChangeSceneToFile("res://game.tscn");
	}
	
	public string ParseAction(Vector2I from, Vector2I to)
	{
		//Convert the X to letters. Format should look like f7e6
		int asciiValue = 96;
		char fromCol = (char)(asciiValue+from.Y);
		char toCol = (char)(asciiValue+to.Y);
		
		string action = $"{fromCol}{from.X}{toCol}{to.X}";
		GD.Print(action);
		return action;
	}
	
	public string StringifyState()
	{
		string state = ai.AIGame.StringifyState();
		
		if (ai.currentPlayer == Constants.Player.Hero)
		{
			state = state + 'x';
		}
		else
		{
			state = state + 'o';
		}
		GD.Print(state);
		return state;
	}
}
