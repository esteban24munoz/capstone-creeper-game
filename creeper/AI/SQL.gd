extends Node
var database : SQLite

func _ready():
	database =SQLite.new();
	database.path="res://default.db"
	database.open_db();
	create_table_raw();
	
	
	
func save_game_state(game_no: int, turn_no: int, board_state: String, winner: String):
	var db = SQLite.new()
	database.path="res://default.db"
	db.open_db()
	
	# Assuming a table named 'game_data' already exists
	var table_name = "Game_States"
	var data = {"Game_No": game_no, "Turn_No": turn_no, "Board_State": board_state, "Winner": winner}
	
	# Insert the data into the column 'Current_Board'
	db.insert_row(table_name, data)
	# print("Game state saved: " + game_state)
	
	db.close_db()
	pass
	
func create_table_raw():
	var db = SQLite.new()
	database.path="res://default.db"
	db.open_db()
	
	var sql = "CREATE TABLE IF NOT EXISTS Game_States (
    	ID INTEGER PRIMARY KEY AUTOINCREMENT,
		Game_No INTEGER,
		Turn_No INTEGER,
        Board_State TEXT,
		Winner TEXT,
		Weight REAL
    );"
	db.query(sql)
