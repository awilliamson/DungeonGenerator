using System;
using System.Collections.Generic;

using System.IO;
using System.Text;

namespace DungeonGenerator
{
	/// <summary>
	///  roomTypes enumeration.
	///  Values will be used to store onto the map array.
	/// </summary>
	public enum roomTypes
	{
		/// <summary>
		///  Unwalkable, represents parts of the map that are undefined.
		/// </summary>
		tile_void = 0,

		/// <summary>
		///  A wall, players should not be able to pass through these, counted as geometry.
		/// </summary>
		tile_wall = 1,

		/// <summary>
		///  Player can freely walk on these tiles.
		/// </summary>
		tile_walkable = 2
	}

	/// <summary>
	///  roomSize enumeration.
	///  Tuple is ( min_width, min_height, max_width, max_height ).
	/// </summary>
	public enum roomSizes
	{
		/// <summary>
		///  Small room, defined as 3x3 -> 5x5
		/// </summary>
		small = 0,

		/// <summary>
		///  Medium room, defined as 5x5 -> 7x7
		/// </summary>
		medium = 1,

		/// <summary>
		///  Large room, defined as 7x7 -> 9x9
		/// </summary>
		large = 2,

		/// <summary>
		///  Huge room, defined as 9x9 -> 17 x 17
		/// </summary>
		huge = 3
	}

	//! Class for Dungeon Generation
	/*!
	 *  This is responsible for generating a dungeon based upon a given seed value.
	 *  This will then be used for all the generation.
	 *  Output will be a standard 2D ( multidimensional ) int array, using the built-in enumerated 'terrain' types.
	 *  A* pathfinding will be used for connceting rooms
	 */
	public class Map
	{
		/// <summary>
		/// 	Random object, private getter and setter.
		/// </summary>
		/// <value>Random Object</value>
		private Random rng { get; set; }

		/// <summary>
		/// 	Tuple of ints, representing width and height dimensions.
		/// </summary>
		/// <value>Tuple of integers for width and height.</value>
		private Tuple< int, int > dimensions { get; set; }

		/// <summary>
		/// 	Multidimensional array holding map data.
		/// </summary>
		/// <value>2D array, corresponding to dimensions width and height. Populated with enumerated tileTypes.</value>
		private int[,] tileArray { get; set; }

		/// <summary>
		/// 	List of Room objects, with getter and setter.
		/// </summary>
		/// <value> List of Rooms </value>
		private List< Room > rooms { get; set; }

		private List< Tuple< int, int, int, int > > roomSizeList;

		/// <summary>
		/// Initializes a new instance of the <see cref="DungeonGenerator.Map"/> class using a seeded random object.
		/// </summary>
		/// <param name="w"> World width. </param>
		/// <param name="h"> World height. </param>
		/// <param name="seed"> Seed to use for generation </param>
		public Map( int w, int h, int seed )
		{
			rng = new Random ( seed );
			dimensions = new Tuple< int, int > (w, h);
			//dimensions.Item1 = w ;
			//dimensions.Item2 = h ;

			tileArray = new int[ dimensions.Item1, dimensions.Item2 ];

			// Initialise a list to store rooms in.
			rooms = new List< Room > ();

			// This will hold our 'sizes', < min_width, min_height, max_width, max_height >
			roomSizeList = new List< Tuple< int, int, int, int > > ();


			roomSizeList.Add(new Tuple< int, int, int, int >(3, 3, 5, 5));
			roomSizeList.Add(new Tuple< int, int, int, int >(5, 5, 7, 7));
			roomSizeList.Add(new Tuple< int, int, int, int >(7, 7, 9, 9));
			roomSizeList.Add(new Tuple< int, int, int, int >(9, 9, 17, 17));

		}

		/// <summary>
		/// 	Initializes a new instance of the <see cref="DungeonGenerator.Map"/> class using a randomly-seeded random object.
		/// </summary>
		/// <param name="w"> World width. </param>
		/// <param name="h"> World height. </param>
		public Map( int w, int h ) : this( w, h, new Random().Next(Int32.MinValue, Int32.MaxValue) ){}
	
		private Room generateRoom()
		{
			// Choose a random type
			Array vals = Enum.GetValues (typeof(roomSizes));

			roomSizes randomSize = (roomSizes)vals.GetValue (rng.Next (vals.Length));

			// +1 on maxvalue, as according to https://msdn.microsoft.com/en-us/library/2dx6wyd4%28v=vs.110%29.aspx, maxvalue is exclusive, whereas minvalue is inclusive.
			int x = rng.Next (roomSizeList[ (int)randomSize ].Item1, roomSizeList[ (int)randomSize ].Item3 + 1);
			int y = rng.Next (roomSizeList[ (int)randomSize ].Item2, roomSizeList[ (int)randomSize ].Item4 + 1);

			Tuple< int, int > randPos = new Tuple< int, int > (rng.Next (dimensions.Item1 - x), rng.Next (dimensions.Item2 - y));
			//Tuple< int, int> randPos = new Tuple< int, int > ( 1, 1 );

			return new Room (randPos, new Tuple< int, int > (x, y), randomSize );
		}

		/// <summary>
		/// 	Generates the map, why creating rooms and connecting them
		/// </summary>
		public void generateMap()
		{
			// Phase 1 - Random Rooms
			do {
				Room r = generateRoom ();

				while (!isFree (r)) {
					r = generateRoom ();
				}

				rooms.Add (r);
				writeRoom (r);
			} while (rooms.Count < 10); //TODO: Need a termination case for room placement, eg give up after 20 unsuccessful placement attemps, then remove that candidate and finish.
				
		}

		/// <summary>
		/// Checks if the given room is within the bounds of the map.
		/// </summary>
		/// <returns><c>true</c>, if bounds was within, <c>false</c> otherwise.</returns>
		/// <param name="r"> Room object </param>
		private bool withinBounds( Room r ){
			Tuple< int, int > o = r.origin;
			return ( o.Item1 > 0 && o.Item2 > 0 ) && ( o.Item1 < dimensions.Item1 - r.size.Item1 && o.Item2 < dimensions.Item2 - r.size.Item2 );
		}

		/// <summary>
		/// Checks if the given room is free to be placed.
		/// </summary>
		/// <returns><c>true</c>, if tiles proposed for room are unoccupied, <c>false</c> otherwise.</returns>
		/// <param name="r"> Room object </param>
		private bool isFree( Room r ){

			// Check if proposed room is within bounds
			if (!withinBounds (r)) {
				return false;
			}

			// Check if it can be placed, by making sure no existing tiles are at the locations.
			for (int i = r.origin.Item1; i < r.origin.Item1 + r.size.Item1; ++i) {
				for (int j = r.origin.Item2; j < r.origin.Item2 + r.size.Item2; ++j) {
					if (tileArray [i , j] != (int)roomTypes.tile_void) {
						return false;
					}
				}
			}

			// Loop done, therefore no overlaps.
			return true;
		}

		/// <summary>
		/// 	Takes a Room object and adds it to the tileArray in the corresponding position.
		/// </summary>
		/// <param name="r"> Room object </param>
		private void writeRoom( Room r ){
			for (int i = r.origin.Item1; i < r.origin.Item1 + r.size.Item1; ++i) {
				for (int j = r.origin.Item2; j < r.origin.Item2 + r.size.Item2; ++j) {
					tileArray [i, j] = (int)roomTypes.tile_walkable;

					int xoff = i - r.origin.Item1;
					int yoff = j - r.origin.Item2;

					if (xoff == 0 || xoff == r.size.Item1 - 1 || yoff == 0 || yoff == r.size.Item2 - 1) {
						tileArray [i, j] = (int)roomTypes.tile_wall;
					}
				}
			}
		}

		/// <summary>
		/// 	Outputs the tileArray as a csv file
		/// </summary>
		/// <param name="fl"> Filepath eg "./output.csv" </param>
		/// <param name="delimit"> String to delimit entries, eg "," </param>
		public void outputMap( string fl, string delimit ){
			StringBuilder sb = new StringBuilder ();

			string[][] output = new string[ dimensions.Item1 ][];
			for (int i = 0; i < dimensions.Item1; ++i) {
				output [i] = new string[ dimensions.Item2 ];

				#if DEBUG
					Console.Write ("\n"); 
				#endif
				for (int j = 0; j < dimensions.Item2; j++) {
					output [i][j] = tileArray [i, j].ToString ();
					#if DEBUG
						Console.Write (tileArray [i, j].ToString ());
					#endif
				}
			}

			for (int ind = 0; ind < dimensions.Item1-1; ++ind) {
				sb.AppendLine (string.Join (delimit, output [ind]));
				File.WriteAllText (fl, sb.ToString ());
			}

		}


	}
}

