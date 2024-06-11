using Godot;
using System.Collections.Generic;

public partial class FighterSprite : AnimatedSprite2D
{
	private string _lastPlayed = "idle";
	private Dictionary<FighterMovement, string> _moveToAnimation = new Dictionary<FighterMovement, string>{
		{ FighterMovement.Running, "run" },
		{ FighterMovement.Jumping, "jump" },
		{ FighterMovement.Landing, "land" },
		{ FighterMovement.Idle, "idle" },
		{ FighterMovement.Skidding, "slide" },
		{ FighterMovement.Wallslide, "wallslide" },
		{ FighterMovement.WallslideJump, "wjump" },
	};

	private Dictionary<FighterAction, string> _actionToAnimation = new Dictionary<FighterAction, string>{
		{ FighterAction.None, null },
		{ FighterAction.Punch, "punch" },
		{ FighterAction.Stunned, "stunned" },
		{ FighterAction.Dying, "death" },
	};

	public IFighter Fighter { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Fighter == null)
		{
			return;
		}

		Position = Fighter.Position;
		FlipH = !Fighter.FacingRight;
		var actionAnimation = _actionToAnimation[Fighter.Action];
		if (actionAnimation != null)
		{
			PlayAnimationIfNew(actionAnimation);
		}
		else
		{
			var moveAnimation = _moveToAnimation[Fighter.Movement];
			PlayAnimationIfNew(moveAnimation);
		}
	}

	private void PlayAnimationIfNew(string animation)
	{
		if (Fighter.HasPistol)
		{
			animation += "_pistol";
		}
		if (_lastPlayed != animation)
		{
			Play(animation);
			_lastPlayed = animation;
		}
	}
}
