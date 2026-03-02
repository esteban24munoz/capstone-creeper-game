extends Sprite2D

# This automatically finds the child named "EyePupil"
@onready var eye_pupil = $EyePupil 

var max_dist = 10 # Distance the pupil can travel from the center

func _process(_delta):
	# 1. Get mouse position relative to the center of the EyeBackground
	var mouse_pos = get_local_mouse_position()
	
	# 2. Limit how far the pupil can go so it stays inside the eye
	eye_pupil.position = mouse_pos.limit_length(max_dist)
