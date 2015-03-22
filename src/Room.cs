using System;

namespace DungeonGenerator
{
	/// <summary>
	/// 	Room class, representing a room within the dungeon
	/// </summary>
	public class Room
	{
		public Tuple< int, int > origin;
		public Tuple< int, int > size;
		public roomSizes type;

		public Room( Tuple< int, int > position, Tuple< int, int> size, roomSizes type ){
			this.origin = position;
			this.size = size;
			this.type = type;
		}


		public Room( int x, int y, int w, int h, roomSizes t ) : this( new Tuple< int, int>(x, y), new Tuple< int, int >(w, h), t ){}
	}
}

