﻿using System;
using System.Collections.Generic;
using System.Linq;
using WiRK.Terminator.MapElements;

namespace WiRK.Terminator
{
	public class Game
	{
		private int _register;

		public Game(Map map, IEnumerable<Robot> robots)
		{
			if (map == null)
				throw new ArgumentNullException("map");
			if (robots == null)
				throw new ArgumentNullException("robots");
			
			Cards = new Deck();
			Board = map;
			Robots = robots;
		}

		public Map Board { get; private set; }

		public IEnumerable<Robot> Robots { get; private set; }

		private Deck Cards { get; set; }

		public void Initialize()
		{
			foreach (var robot in Robots)
			{
				robot.Initialize(this);
			}
		}

		/// <summary>
		/// Called at the start of each turn. 
		/// </summary>
		public void StartTurn()
		{
			StartTurn(true /* dealCards */);
		}

		/// <summary>
		/// Called at the start of each turn
		/// </summary>
		/// <param name="dealCards">Deal cards to robots. Default is true. Overwritten for simulation.</param>
		internal void StartTurn(bool dealCards)
		{
			if (dealCards)
			{
				// Deal cards
				Cards.Deal(Robots);
			}

			// The game refers to the first register as register 1 and not register 0.
			_register = 1;
		}

		/// <summary>
		/// Executes the next register.
		/// - Robots move
		/// - Board elements move
		///		- Express conveyers move 1 space
		///		- Express and normal conveyers move 1 space
		///		- Pushers push if active
		///		- Gears rotate 90 degrees
		/// - Lasers fire
		/// </summary>
		/// <returns>Registers remaining to execute</returns>
		public int ExecuteNextRegister()
		{
			if (_register <= Constants.RobotRegisters)
			{
				ExecuteMoves();

				ExecuteExpressConveyers();

				ExecuteConveyers();

				ExecutePushers();

				ExecuteGears();

				ExecuteLasers();

				++_register;
			}

			return Constants.RobotRegisters - _register + 1;
		}

		/// <summary>
		/// Called at the end of each turn.
		/// - Claims cards back to the deck
		/// - Wrenches heal, etc
		/// </summary>
		public void EndTurn()
		{
			// TODO: Do end of turn stuff, e.g. heal on wrench

			// Pick up unlocked cards
			Cards.Reclaim(Robots);
		}

		private void ExecuteMoves()
		{
			var robotsCardsByPriority = Robots
				.Select(robot => new Tuple<Robot, int>(robot, robot.CardAtRegister(_register)))
				.OrderByDescending(x => x.Item2)
				.ToList();

			// Highest priority card executes first
			foreach (var robotCard in robotsCardsByPriority)
			{
				Robot robot = robotCard.Item1;
				robot.ExecuteMove(_register);
			}
		}

		private void ExecuteExpressConveyers()
		{
			// Express conveyers convey 1 square
			foreach (var conveyer in Robots.Select(robot => Board.Tile<ExpressConveyer>(robot.Position)).Where(conveyer => conveyer != null))
			{
				conveyer.Execute(this, TileExecution.ExpressConveyer);
			}
		}

		private void ExecuteConveyers()
		{
			// Express and Normal conveyers convey 1 square
			foreach (var conveyer in Robots.Select(robot => Board.Tile<Conveyer>(robot.Position)).Where(conveyer => conveyer != null))
			{
				conveyer.Execute(this, TileExecution.Conveyer);
			}
		}

		private void ExecutePushers()
		{
			// Pushers push if active for register
			foreach (var robot in Robots)
			{
				var floor = Board.Tile<Floor>(robot.Position);
				if (floor != null)
				{
					var pusher = floor.GetPusher();
					if (pusher != null)
					{
						pusher.Push(robot, _register);
					}
				}
			}
		}

		private void ExecuteGears()
		{
			foreach (var gear in Robots.Select(robot => Board.Tile<Gear>(robot.Position)).Where(gear => gear != null))
			{
				gear.Execute(this, TileExecution.Gear);
			}
		}

		private void ExecuteLasers()
		{
			for (int y = 0; y < Board.Squares.Count(); ++y)
			{
				for (int x = 0; x < Board.Squares.ElementAt(y).Count(); ++x)
				{
					var pos = new Coordinate { X = x, Y = y };
					var floor = Board.Tile<Floor>(pos);

					if (floor == null)
						continue;

					foreach (var laserEdge in floor.GetLasers())
					{
						bool skipFirst = true;
						Func<Coordinate, Coordinate> traverser = GetLaserIterator(laserEdge.Item1);

						for (var p = new Coordinate { X = x, Y = y }; ; p = traverser(p))
						{
							ITile tile = Board.Tile(p);

							if (tile == null)
								break;
							if (tile is Pit)
								continue;

							var floorTile = (Floor) tile;

							// If it hits a wall entering a tile then laser stops
							// The first time we enter this loop it the actual laser doing the firing, so it does
							// not make sense to stop on a collision with it.
							if (!skipFirst)
							{
								if (floorTile.GetEdge(laserEdge.Item1) != null)
								{
									break;
								}
							}
							skipFirst = false;

							// If the laser collides with a bot, it deals damage and stops the laser
							Robot robot = RobotAt(p);
							if (robot != null) // hit!
							{
								robot.Damage += laserEdge.Item2.Lasers;
								break;
							}

							// If the laser collides with a wall while exiting a tile then the laser stops
							if (floorTile.GetEdge(Utilities.GetOppositeOrientation(laserEdge.Item1)) != null)
							{
								break;
							}
						}
					}
				}
			}
		}

		private Func<Coordinate, Coordinate> GetLaserIterator(Orientation orientation)
		{
			if (orientation == Orientation.Top)
				return c => { c.Y = c.Y + 1; return c; };
			if (orientation == Orientation.Bottom)
				return c => { c.Y = c.Y - 1; return c; };
			if (orientation == Orientation.Left)
				return c => { c.X = c.X + 1; return c; };

			// Right
			return c => { c.X = c.X - 1; return c; };
		}

		internal Robot RobotAt(Coordinate position)
		{
			return Robots.FirstOrDefault(robot => robot.Position == position);
		}
	}
}
