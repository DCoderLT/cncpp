using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCClasses.FileFormats.Text;
using System.IO;
//using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses {
    public class MapClass {
        public MAP MapFile;

        public Theater TheaterData;

        public Rectangle MapSize;

        private Rectangle MapRect;

        public CellClass[] Cells = new CellClass[512 * 512];

        public class CellIterator {
            int NextX;
            int NextY;
            int CurrentY;

            int CurrentCellIndex;

            private MapClass Map;

            internal CellIterator(MapClass _Map) {
                Map = _Map;
                NextX = 1;
                NextY = Map.MapRect.Width;
                CurrentY = NextY - 1;
                CurrentCellIndex = 512 * NextY + 1;
            }

            internal void Reset() {
                NextX = 1;
                NextY = Map.MapRect.Width;
                CurrentY = NextY - 1;
                CurrentCellIndex = 512 * NextY + 1;
            }

            internal CellClass NextCell() {
                if (CurrentY != 0) {
                    ++NextX;
                    --NextY;
                    --CurrentY;
                    CurrentCellIndex -= 511;
                } else {
                    var nY = NextY;
                    var nX = NextX;
                    var w = Map.MapRect.Width;
                    NextX = nY;
                    NextY = nX;
                    var cy = w;
                    if (((nY - w + nX - 1) & 1) != 0) {
                        --cy;
                        NextY = nX + 1;
                    } else {
                        cy -= 2;
                        NextX = nY + 1;
                    }
                    CurrentY = cy;
                    CurrentCellIndex = 512 * NextY + NextX;
                }
                return Map.Cells[CurrentCellIndex];
            }
        }

        public CellIterator cellIter;


        public MapClass(String filename) {
            MapFile = new MAP(FileSystem.LoadFile(filename));
        }

        public void Initialize(String filename = null) {
            if (filename != null) {
                MapFile = new MAP(FileSystem.LoadFile(filename));
            }

            ClearCells();

            String TheaterName = "";
            MapFile.GetString("Map", "Theater", out TheaterName, "");

            if (!Theater.Theaters.ContainsKey(TheaterName)) {
                throw new InvalidDataException(String.Format("Theater {0} is not recognized.", TheaterName));
            }

            int[] sz = new int[4];
            if (MapFile.Get4Integers("Map", "Size", out sz, new int[4])) {
                MapSize = new Rectangle() { X = sz[0], Y = sz[1], Width = sz[2], Height = sz[3] };

                MapRect = MapSize;

                CreateMap();
            }

            cellIter = new CellIterator(this);

            TheaterData = Theater.Theaters[TheaterName];

            Theater.Init(TheaterData);

            foreach (var packedTile in MapFile.Tiles) {
                var x = packedTile.X;
                var y = packedTile.Y;
                var cell = GetCellAt(x, y);
                if (cell != null) {
                    cell.IsoTileTypeIndex = (int)packedTile.TileTypeIndex;
                    cell.IsoTileTypeSubIndex = (int)packedTile.TileSubtypeIndex;
                    cell.Level = packedTile.Level;
                }
            }
        }

        private void ClearCells() {
            for (var i = 0; i < 512 * 512; ++i) {
                Cells[i] = null;
            }
        }

        private CellClass GetCellAt(int X, int Y) {
            if (X >= 0 && X < 512) {
                if (Y >= 0 && Y < 512) {
                    var offset = X + (Y << 9);
                    return Cells[offset];
                }
            }
            return null;
        }

        private void CreateMap() {

            var wh = MapSize.Width + MapSize.Height - 1;

            var lineBaseOffset = 0;

            for (var v2 = 0; v2 < 2 * wh + 2; ++v2) {
                for (var v1 = 0; v1 < wh + 2; ++v1) {

                    var v3 = v1 + v2;
                    var v4 = MapRect.Width;
                    if (v3 > v4) {
                        if (v1 - v2 < v4) {
                            if (v2 - v1 < v4) {
                                if (v3 < v4 + 2 * MapRect.Height) {
                                    var cell = new CellClass() {
                                        X = v1,
                                        Y = v2,
                                        Level = 0
                                    };
                                    Cells[v1 + lineBaseOffset] = cell;
                                }
                            }
                        }
                    }
                }

                lineBaseOffset += 512;
            }

        }

        Helpers.ZBufferedTexture TileTexture;

        public Texture2D GetTexture(GraphicsDevice gd) {
            var Tactical = TacticalClass.Instance;

            //var MapTexture = new Texture2D(GraphicsDevice, Tactical.Width, Tactical.Height);

            //var TileTexture = new Color[Tactical.Width * Tactical.Height];

            TileTexture = new Helpers.ZBufferedTexture(gd, Tactical.Width, Tactical.Height);

            cellIter.Reset();
            for (var c = cellIter.NextCell(); c != null; c = cellIter.NextCell()) {
                Tactical.UpdateCellPosition(c);
            }

            var visibleCells = Cells.Where(c => c != null && c.VisibleInTactical).OrderBy(c => c.Y * Tactical.Width + c.X);

            //var TextureSize = new Rectangle(0, 0, Tactical.Width, Tactical.Height);

            foreach (var c in visibleCells) {
                c.DrawBase(TileTexture);
            }

            foreach (var c in visibleCells) {
                c.DrawExtra(TileTexture);
            }

            return TileTexture.Compile();
        }
    }
}
