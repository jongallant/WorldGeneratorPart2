using UnityEngine;
using AccidentalNoise;
using System.Collections.Generic;

public class Generator : MonoBehaviour {

	// Adjustable variables for Unity Inspector
	[SerializeField]
	int Width = 512;
	[SerializeField]
	int Height = 512;
	[SerializeField]
	int TerrainOctaves = 6;
	[SerializeField]
	double TerrainFrequency = 1.25;
	[SerializeField]
	float DeepWater = 0.2f;
	[SerializeField]
	float ShallowWater = 0.4f;	
	[SerializeField]
	float Sand = 0.5f;
	[SerializeField]
	float Grass = 0.7f;
	[SerializeField]
	float Forest = 0.8f;
	[SerializeField]
	float Rock = 0.9f;

	// private variables
	ImplicitFractal HeightMap;
	MapData HeightData;
	Tile[,] Tiles;

	List<TileGroup> Waters = new List<TileGroup> ();
	List<TileGroup> Lands = new List<TileGroup> ();
	
	// Our texture output gameobject
	MeshRenderer HeightMapRenderer;

	void Start()
	{
		HeightMapRenderer = transform.Find ("HeightTexture").GetComponent<MeshRenderer> ();

		Initialize ();
		GetData (HeightMap, ref HeightData);
		LoadTiles ();

		UpdateNeighbors ();
		UpdateBitmasks ();
		FloodFill ();

		HeightMapRenderer.materials[0].mainTexture = TextureGenerator.GetTexture (Width, Height, Tiles);
	}

	void Update()
	{
		if (Input.GetKeyDown (KeyCode.F5)) {
			Initialize ();
			GetData (HeightMap, ref HeightData);
			LoadTiles ();
		}
	}

	private void Initialize()
	{
		// Initialize the HeightMap Generator
		HeightMap = new ImplicitFractal (FractalType.MULTI, 
		                               BasisType.SIMPLEX, 
		                               InterpolationType.QUINTIC, 
		                               TerrainOctaves, 
		                               TerrainFrequency, 
		                               UnityEngine.Random.Range (0, int.MaxValue));
	}
	
	// Extract data from a noise module
	private void GetData(ImplicitModuleBase module, ref MapData mapData)
	{
		mapData = new MapData (Width, Height);

		// loop through each x,y point - get height value
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {

				//Wrap on x-axis only
//				//Noise range
//				float x1 = 0, x2 = 1;
//				float y1 = 0, y2 = 1;				
//				float dx = x2 - x1;
//				float dy = y2 - y1;
//
//				//Sample noise at smaller intervals
//				float s = x / (float)Width;
//				float t = y / (float)Height;
//
//				// Calculate our 3D coordinates
//				float nx = x1 + Mathf.Cos (s * 2 * Mathf.PI) * dx / (2 * Mathf.PI);
//				float ny = x1 + Mathf.Sin (s * 2 * Mathf.PI) * dx / (2 * Mathf.PI);
//				float nz = t;
//
//				float heightValue = (float)HeightMap.Get (nx, ny, nz);
//
//				// keep track of the max and min values found
//				if (heightValue > mapData.Max)
//					mapData.Max = heightValue;
//				if (heightValue < mapData.Min)
//					mapData.Min = heightValue;
//
//				mapData.Data [x, y] = heightValue;



				// WRAP ON BOTH AXIS
				// Noise range
				float x1 = 0, x2 = 2;
				float y1 = 0, y2 = 2;				
				float dx = x2 - x1;
				float dy = y2 - y1;

				// Sample noise at smaller intervals
				float s = x / (float)Width;
				float t = y / (float)Height;

			
				// Calculate our 4D coordinates
				float nx = x1 + Mathf.Cos (s*2*Mathf.PI) * dx/(2*Mathf.PI);
				float ny = y1 + Mathf.Cos (t*2*Mathf.PI) * dy/(2*Mathf.PI);
				float nz = x1 + Mathf.Sin (s*2*Mathf.PI) * dx/(2*Mathf.PI);
				float nw = y1 + Mathf.Sin (t*2*Mathf.PI) * dy/(2*Mathf.PI);
			
				float heightValue = (float)HeightMap.Get (nx, ny, nz, nw);
				// keep track of the max and min values found
				if (heightValue > mapData.Max) mapData.Max = heightValue;
				if (heightValue < mapData.Min) mapData.Min = heightValue;

				mapData.Data[x,y] = heightValue;
			}
		}	
	}

	// Build a Tile array from our data
	private void LoadTiles()
	{
		Tiles = new Tile[Width, Height];
		
		for (var x = 0; x < Width; x++)
		{
			for (var y = 0; y < Height; y++)
			{
				Tile t = new Tile();
				t.X = x;
				t.Y = y;
				
				float value = HeightData.Data[x, y];
				value = (value - HeightData.Min) / (HeightData.Max - HeightData.Min);
				
				t.HeightValue = value;
				
				//HeightMap Analyze
				if (value < DeepWater)  {
					t.HeightType = HeightType.DeepWater;
					t.Collidable = false;
				}
				else if (value < ShallowWater)  {
					t.HeightType = HeightType.ShallowWater;
					t.Collidable = false;
				}
				else if (value < Sand) {
					t.HeightType = HeightType.Sand;
					t.Collidable = true;
				}
				else if (value < Grass) {
					t.HeightType = HeightType.Grass;
					t.Collidable = true;
				}
				else if (value < Forest) {
					t.HeightType = HeightType.Forest;
					t.Collidable = true;
				}
				else if (value < Rock) {
					t.HeightType = HeightType.Rock;
					t.Collidable = true;
				}
				else  {
					t.HeightType = HeightType.Snow;
					t.Collidable = true;
				}
				
				Tiles[x,y] = t;
			}
		}
	}
	
	private void UpdateNeighbors()
	{
		for (var x = 0; x < Width; x++)
		{
			for (var y = 0; y < Height; y++)
			{
				Tile t = Tiles[x,y];
				
				t.Top = GetTop(t);
				t.Bottom = GetBottom (t);
				t.Left = GetLeft (t);
				t.Right = GetRight (t);
			}
		}
	}

	private void UpdateBitmasks()
	{
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {
				Tiles [x, y].UpdateBitmask ();
			}
		}
	}

	private void FloodFill()
	{
		// Use a stack instead of recursion
		Stack<Tile> stack = new Stack<Tile>();
		
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				
				Tile t = Tiles[x,y];

				//Tile already flood filled, skip
				if (t.FloodFilled) continue;

				// Land
				if (t.Collidable)   
				{
					TileGroup group = new TileGroup();
					group.Type = TileGroupType.Land;
					stack.Push(t);
					
					while(stack.Count > 0) {
						FloodFill(stack.Pop(), ref group, ref stack);
					}
					
					if (group.Tiles.Count > 0)
						Lands.Add (group);
				}
				// Water
				else {				
					TileGroup group = new TileGroup();
					group.Type = TileGroupType.Water;
					stack.Push(t);
					
					while(stack.Count > 0)	{
						FloodFill(stack.Pop(), ref group, ref stack);
					}
					
					if (group.Tiles.Count > 0)
						Waters.Add (group);
				}
			}
		}
	}


	private void FloodFill(Tile tile, ref TileGroup tiles, ref Stack<Tile> stack)
	{
		// Validate
		if (tile.FloodFilled) 
			return;
		if (tiles.Type == TileGroupType.Land && !tile.Collidable)
			return;
		if (tiles.Type == TileGroupType.Water && tile.Collidable)
			return;

		// Add to TileGroup
		tiles.Tiles.Add (tile);
		tile.FloodFilled = true;

		// floodfill into neighbors
		Tile t = GetTop (tile);
		if (!t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetBottom (tile);
		if (!t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetLeft (tile);
		if (!t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetRight (tile);
		if (!t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
	}
	
	private Tile GetTop(Tile t)
	{
		return Tiles [t.X, MathHelper.Mod (t.Y - 1, Height)];
	}
	private Tile GetBottom(Tile t)
	{
		return Tiles [t.X, MathHelper.Mod (t.Y + 1, Height)];
	}
	private Tile GetLeft(Tile t)
	{
		return Tiles [MathHelper.Mod(t.X - 1, Width), t.Y];
	}
	private Tile GetRight(Tile t)
	{
		return Tiles [MathHelper.Mod (t.X + 1, Width), t.Y];
	}


}
