using Godot;
using System.Collections.Generic;

public partial class FpsLabel : Label
{
	private const int FpsCap = 120;
	private double _timeSinceLastUpdateS = 0;

	private double _bufferTotalTime = 0;
	private Queue<double> _fpsBuffer = new Queue<double>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Engine.MaxFps = FpsCap;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_fpsBuffer.Enqueue(delta);
		_bufferTotalTime += delta;
		while (_bufferTotalTime > 1)
		{
			_bufferTotalTime -= _fpsBuffer.Dequeue();
		}

		_timeSinceLastUpdateS += delta;
		if (_timeSinceLastUpdateS > 1)
		{
			_timeSinceLastUpdateS = 0;
			var fps = _fpsBuffer.Count / _bufferTotalTime;
			Text = $"FPS: {(int)fps}\nCap: {FpsCap}";
		}
	}
}
