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
	int winCount = 0;
	int drawCount = 0;
	
	public async void PlayAiVsAi()
	{
		string PLAYER_TOKEN;
		
		try
		{
			PLAYER_TOKEN = await File.ReadAllTextAsync($"{PLAYER_NAME}_token.txt");
		}
		catch (FileNotFoundException)
		{
			var createPlayerPayload = new
			{
				name = PLAYER_NAME,
				email = PLAYER_EMAIL
			};
			
			var createResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/player/create", createPlayerPayload);
			createResponse.EnsureSuccessStatusCode();
			var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
			PLAYER_TOKEN = createJson.GetProperty("token").GetString();
			
			await File.WriteAllTextAsync($"{PLAYER_NAME}_token.txt", PLAYER_TOKEN);
		}
		
		string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		Model currentGame = new();
		int gameCount = 0;
		while (true)
		{
			var playStateObj = new
			{
				@event = EVENT,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};
			
			GD.Print("Getting the state");
			var playStateResponse = await client.PostAsJsonAsync(playStateUrl, playStateObj);
			//playStateResponse.EnsureSuccessStatusCode();

			if (playStateResponse.StatusCode != System.Net.HttpStatusCode.OK || playStateResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
			{
				// No games waiting for move, wait and continue
				GD.Print($"{playStateResponse.StatusCode}: Sleep 2 seconds and try again");
				await Task.Delay(2000);
				continue;
			}

			var playStateJson = await playStateResponse.Content.ReadFromJsonAsync<JsonElement>();
			string state = playStateJson.GetProperty("state").GetString();
			string game_id = playStateJson.GetProperty("game_id").ToString();
			string action_id = playStateJson.GetProperty("action_id").ToString();
			GD.Print($"game_id: {game_id}\t action_id: {action_id}");
			GD.Print($"state: {state}");
			string parsedAction;

			/***************************************************************************************
			Starting here, AI should take over. The state of the game is still just a string.
			***************************************************************************************/
			if (state == ".oo.xx.o.....xo.....x.......x.....ox.....o.xx.oo.o....x........................x....ox"){
				GD.Print("New game started");
				gameCount++;
				//return;
			}
			
			//Get State and update the Model accordingly
			Constants.Player activePlayer = currentGame.UpdateState(state);
			MTCS_Pure2.MonteCarloStrategy currentNode = new MTCS_Pure2.MonteCarloStrategy();
			MTCS_Pure2.Move bestMove = currentNode.ChooseBestMove(currentGame, activePlayer);
			GD.Print(bestMove);
			parsedAction = ParseAction(bestMove.From, bestMove.To);
			
			/***************************************************************************************
			End of AI
			***************************************************************************************/
			
			var submitActionObj = new
			{
				action = parsedAction,
				action_id = action_id,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};

			var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
			//submitResponse.EnsureSuccessStatusCode();
			if (submitResponse.StatusCode == System.Net.HttpStatusCode.OK)
			{
				var submitJson = await submitResponse.Content.ReadFromJsonAsync<JsonElement>();
				string winner = submitJson.GetProperty("winner").GetString();
				GD.Print($"Winner: {winner}");
				if (winner == "draw")
				{
					drawCount++;
					GD.Print($"Draws: {drawCount}");
				}
				if (winner == "x" || winner == "o")
				{
					winCount++;
					GD.Print($"Wins: {winCount}");
				}
			}
			else // (submitResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
			{
				GD.Print($"{submitResponse.StatusCode}: Sleep half a second and try again");
				await Task.Delay(500);
				continue;
			}
		}
	}
	
	//Called when the node enters scene tree for first time
	public override void _Ready()
	{
		//PlayAiVsAi();
	}
	
	public void _on_button_pressed()
	{
		try
		{
			PlayAiVsAi();
		}
		catch (HttpRequestException ex)
		{
			GD.Print($"Request to softserve failed: {ex.Message}");
		}
	}
	
	//public async void PlayAiVsAi()
	//{
		//string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		////while (true)
		//for (int i = 0; i < 1; i++)
		//{
			//var playStateObj = new
			//{
				//@event = EVENT,
				//player = PLAYER_NAME,
				//token = PLAYER_TOKEN
			//};
			//
			//GD.Print("Getting the state");
			//var playStateResponse = await client.PostAsJsonAsync(playStateUrl, playStateObj);
			//GD.Print(playStateResponse.StatusCode);
			//playStateResponse.EnsureSuccessStatusCode();
//
			//if (playStateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
			//{
				//// No games waiting for move, wait and continue
				//GD.Print("204: Sleep 5 seconds and try again");
				//await Task.Delay(5000);
				//continue;
			//}
//
			//var playStateJson = await playStateResponse.Content.ReadFromJsonAsync<JsonElement>();
			////GD.Print(playStateJson);
			//string state = playStateJson.GetProperty("state").GetString();
			//string action_id = playStateJson.GetProperty("action_id").ToString();
//
			//GD.Print($"state:\t{state}");
//
			///***************************************************************************************
			//Starting here, AI should take over. The code below is just a placeholder
			//to test the Network functionality. The state of the game is still just a string.
			//***************************************************************************************/
			//
			//GD.Print("Testing Parse Function");
			//ai.currentPlayer = ai.AIGame.UpdateState(state);
			//
			////Get a list of possible actions based on the state from the API
			//var actionsResponse = await client.GetAsync($"{SOFTSERVE_URL}/state/{state}/actions");
			//actionsResponse.EnsureSuccessStatusCode();
//
			//var actionsJson = await actionsResponse.Content.ReadFromJsonAsync<JsonElement>();
			//var actions = actionsJson.GetProperty("actions").EnumerateArray();
//
			//var actionList = new List<string>();
			//foreach (var act in actions)
			//{
				//actionList.Add(act.GetString());
				////GD.Print(act.GetString());
			//}
			//GD.Print("Count of available actions: " + actionList.Count);
//
			////Pick random action from list
			//var random = new Random();
			//string action = actionList[random.Next(actionList.Count)];
//
			//// Simulate thinking time
			//await Task.Delay(2000);
//
			//GD.Print($"action chosen:\t{action}");
			//
			////Get move from AI here
			//GD.Print("Testing Action Parsing");
			//Vector2I from = new Vector2I(6,7);
			//Vector2I to = new Vector2I(5,6);
			//string parsedAction = ParseAction(from, to);
			//
			///************************************************************
			//End of placeholder AI
			//************************************************************/
			//
			//var submitActionObj = new
			//{
				//action = action,
				//action_id = action_id,
				//player = PLAYER_NAME,
				//token = PLAYER_TOKEN
			//};
//
			//var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
			//submitResponse.EnsureSuccessStatusCode();
		//}
	//}
	
	public string ParseAction(Vector2I from, Vector2I to)
	{
		//Convert the X to letters. Format should look like f7e6
		char fromCol = (char)('a' + from.Y);
		char toCol = (char)('a' + to.Y);
		
		string action = $"{fromCol}{from.X + 1}{toCol}{to.X + 1}";
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
