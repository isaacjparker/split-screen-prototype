@tool
class_name NoteDialog
extends ConfirmationDialog

var _line_edit: LineEdit

func _init() -> void:
	title = "Create New Note"
	min_size = Vector2i(340, 100)

	var vbox := VBoxContainer.new()

	var label := Label.new()
	label.text = "Note file name:"
	vbox.add_child(label)

	_line_edit = LineEdit.new()
	_line_edit.placeholder_text = "my_note  (will add .txt if omitted)"
	_line_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_line_edit.gui_input.connect(_on_line_edit_input)
	vbox.add_child(_line_edit)

	add_child(vbox)

func _ready() -> void:
	_line_edit.call_deferred("grab_focus")

func get_file_name() -> String:
	return _line_edit.text.strip_edges()

func _on_line_edit_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_ENTER or event.keycode == KEY_KP_ENTER:
			confirmed.emit()
			hide()
