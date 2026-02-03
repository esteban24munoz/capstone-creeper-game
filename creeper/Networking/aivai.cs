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
	
	//Called when the node enters scene tree for first time
	public async override void _Ready()
	//static async Task Main()
	{
		string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		//while (true)
		for (int i = 0; i < 5; i++)
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
}
