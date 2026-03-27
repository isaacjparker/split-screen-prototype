@tool
extends EditorPlugin

var _context_menu_plugin

func _enter_tree() -> void:
	_context_menu_plugin = NoteContextMenu.new()
	add_context_menu_plugin(EditorContextMenuPlugin.CONTEXT_SLOT_FILESYSTEM, _context_menu_plugin)

func _exit_tree() -> void:
	remove_context_menu_plugin(_context_menu_plugin)
	_context_menu_plugin = null
