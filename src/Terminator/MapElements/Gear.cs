﻿using System;

namespace WiRK.Terminator
{
	public class Gear : Floor
	{
		public Rotation Direction { get; set; }

		public Gear(Rotation direction)
		{
			if (direction == Rotation.None)
				throw new ArgumentException("direction");

			Direction = direction;
		}

		public override void Execute(Game game, TileExecution execution)
		{
			if (execution == TileExecution.Gear)
			{
				Rotate(RobotOnTile(game));
			}

			base.Execute(game, execution);
		}

		/// <summary>
		/// Rotate the robot
		/// </summary>
		/// <param name="robot">Robot to rotate</param>
		private void Rotate(Robot robot)
		{
			if (robot == null)
				throw new ArgumentNullException("robot");

			if (Direction == Rotation.Clockwise)
				robot.Facing = Utilities.ClockwiseRotation(robot.Facing);
			else
				robot.Facing = Utilities.CounterclockwiseRotation(robot.Facing);
		}
	}
}
