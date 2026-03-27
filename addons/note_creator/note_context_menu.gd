@tool
class_name NoteContextMenu
extends EditorContextMenuPlugin

var _dialog: Window
var _target_dir: String
var _current_paths: PackedStringArray   # store paths here instead of binding

func _popup_menu(paths: PackedStringArray) -> void:
	_current_paths = paths
	add_context_menu_item("New Note...", _on_create_note)   # no .bind()

func _on_create_note(_unused) -> void:
	if _current_paths.size() > 0:
		var path := _current_paths[0]
		if DirAccess.dir_exists_absolute(ProjectSettings.globalize_path(path)):
			_target_dir = path
		else:
			_target_dir = path.get_base_dir()
	else:
		_target_dir = "res://"

	_show_dialog()

func _show_dialog() -> void:
	_dialog = NoteDialog.new()
	_dialog.confirmed.connect(_on_dialog_confirmed)
	_dialog.canceled.connect(_on_dialog_canceled)
	# Add to the editor viewport so it renders properly
	EditorInterface.get_base_control().add_child(_dialog)
	_dialog.popup_centered()

func _on_dialog_confirmed() -> void:
	var file_name: String = _dialog.get_file_name()
	if file_name.is_empty():
		_cleanup_dialog()
		return

	# Ensure .txt extension
	if not file_name.ends_with(".txt"):
		file_name += ".txt"

	var full_path := _target_dir.path_join(file_name)

	# Don't overwrite existing files
	if FileAccess.file_exists(full_path):
		push_warning("NoteCreator: File already exists: %s" % full_path)
		_cleanup_dialog()
		return

	_write_note(full_path)
	EditorInterface.get_resource_filesystem().scan()
	_cleanup_dialog()

func _on_dialog_canceled() -> void:
	_cleanup_dialog()

func _cleanup_dialog() -> void:
	if is_instance_valid(_dialog):
		_dialog.queue_free()
	_dialog = null

func _write_note(path: String) -> void:
	var project_name: String = ProjectSettings.get_setting("application/config/name", "Untitled Project")
	var date := Time.get_date_dict_from_system()
	var date_str := "%04d-%02d-%02d" % [date["year"], date["month"], date["day"]]
	var file_title := path.get_file().get_basename()

	# Read template from file
	const TEMPLATE_PATH = "res://addons/note_creator/template.txt"
	var template_file := FileAccess.open(TEMPLATE_PATH, FileAccess.READ)
	if not template_file:
		push_error("NoteCreator: Could not read template at %s" % TEMPLATE_PATH)
		return
	var template := template_file.get_as_text()
	template_file.close()

	# Replace placeholders
	var content := template.format({
		"title":   file_title,
		"project": project_name,
		"date":    date_str,
	})

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(content)
		file.close()
		print("NoteCreator: Created note at ", path)
	else:
		push_error("NoteCreator: Failed to write file at %s (error %d)" % [path, FileAccess.get_open_error()])
