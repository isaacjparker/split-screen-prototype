using Godot;
using System;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	public override void _Ready()
    {
        Instance = this;
    }

	public void CreateAudio(SoundEffectConfig config)
	{
		if (config?.SoundEffect == null) return;
		if (config.HasReachedLimit()) return;

		config.ChangeAudioCount(1);

		AudioStreamPlayer newAudio = new();
		AddChild(newAudio);

		newAudio.Stream = config.SoundEffect;
		newAudio.VolumeDb = config.Volume;
		newAudio.PitchScale = config.GetRandomisedPitchScale();

		newAudio.Finished += config.OnAudioFinished;
		newAudio.Finished += newAudio.QueueFree;
		newAudio.Play();
	}

	public void CreateAudioAtPosition(Vector3 position, SoundEffectConfig config)
	{
		// TODO: Split-screen positional panning
		// Goal: Derive a weighted average screen position across all active players in
		// LocalMultiplayerManager.Instance.PlayerSlotToInstance based on their distance
		// to 'position', then map that to a pan value on a standard AudioStreamPlayer.
		// The closer a player is to 'position', the more that player's camera quadrant
		// should influence the final pan. Replace CreateAudio call below when implemented.


		// Delegates to createAudio as top down camera too far away to hear
		// 3D audio. See above for potential split screen audio
		CreateAudio(config);
	}
}
