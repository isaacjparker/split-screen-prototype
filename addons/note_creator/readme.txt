NOTE CREATOR - Godot 4 Editor Plugin
=====================================
Adds a "New Note..." option to the FileSystem right-click context menu.
Clicking it prompts for a filename and creates a .txt file using a
pre-defined template. Useful for keeping documentation and notes inside your
project folder


INSTALLATION
------------
1. Copy the addons/note_creator/ folder into your project's addons/ folder
2. Open Project > Project Settings > Plugins.
3. Find "Note Creator" and set it to Active.


USAGE
-----
1. Right-click any file or folder in the FileSystem panel.
2. Select "New Note..." from the context menu.
3. Enter a filename (the .txt extension is added automatically if omitted).
4. Click OK. The note will appear in the same folder as the item you right-clicked.


CUSTOMISING THE TEMPLATE
------------------------
Edit addons/note_creator/template.txt to change the template applied to
every new note. Three placeholders are available:

{title}     -       The filename without extension
{project}   -       Your project name from Project Settings
{date}      -       Today's date in YYYY-MM-DD format

No changes to any .gd file are needed. Just save template.txt and the
next note you create will use the updated template.


TROUBLESHOOTING
---------------
Q:  I get an error about curly braces when creating a note.
A:  The template uses { and } as placeholder markers. If you want a literal
	curly brace in your template text (e.g. for code examples or JSON),
	escape it by doubling it: {{ and }}. For example, write {{example}}
	if you want {example} to appear in the final note.
	
Q:  The plugin fails to activate with a parse or "nonexistent function" error.
A:  Make sure all three .gd files are present in the addons/note_creator/
	folder. Try disabling the plugin, waiting a moment, and re-enabling it.
	This forces Godot to reload and re-parse the scripts cleanly.
	
Q:  "New Note..." does not appear in the context menu.
A:  Confirm the plugin is enabled in Project Settings > Plugins. If it is,
	try disabling and re-enabling it. Also check the Godot console for any
	error messages from the plugin scripts on startup.
	
Q:  The template.txt file is missing and no note is created.
A:  The plugin expects the template at:
		res://addons/note_creator/template.txt
	If this file is missing or was moved, the plugin will log an error and
	abort. Resotre the file to that exact path to fix it.
