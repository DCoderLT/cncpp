using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCClasses.FileFormats.Binary;
using System.IO;
using CCClasses.FileFormats.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses {
    public class IsoTileTypeClass {

        public class TilesetConfig {
            public String SetName;
            public String FileName;
            public int TilesInSet;
            public int MarbleMadness;
            public int NonMarbleMadness;
            public bool Morphable;
            public bool AllowToPlace;
            public bool AllowBurrowing;
            public bool AllowTiberium;
            public bool RequiredForRMG;
            public int ToTemperateTheater;
            public int ToSnowTheater;
            public bool ShadowCaster;
            public int ShadowTiles;
        }

        public String SetName;
        public String FileName;
        public int MarbleMadness;
        public int NonMarbleMadness;
        public bool Morphable;
        public bool AllowToPlace;
        public bool AllowBurrowing;
        public bool AllowTiberium;
        public bool RequiredForRMG;
        public int ToTemperateTheater;
        public int ToSnowTheater;
        public bool ShadowCaster;
        public int ShadowTiles;
        public int AnimTypeIndex;
        public int TileXOffset;
        public int TileYOffset;
        public int TileAttachesTo;
        public int TileZAdjust;

        protected TilesetConfig cfg;
        protected int IndexInTileset;

        public IsoTileTypeClass NextVariation;

        public TMP Tile;

        public class TilesetLookup {
            public String Name;
            public int TilesetIndex;
            public int TileIndex;
        };

        public static Dictionary<String, TilesetLookup> TilesetIndices = new Dictionary<string, TilesetLookup>() {
             {"RampBase", new TilesetLookup(){ Name = "RampBase", TilesetIndex = -1, TileIndex = -1 } },
             {"RampSmooth", new TilesetLookup(){ Name = "RampSmooth", TilesetIndex = -1, TileIndex = -1 } },
             {"MMRampBase", new TilesetLookup(){ Name = "MMRampBase", TilesetIndex = -1, TileIndex = -1 } },
             {"ClearTile", new TilesetLookup(){ Name = "ClearTile", TilesetIndex = -1, TileIndex = -1 } },
             {"RoughTile", new TilesetLookup(){ Name = "RoughTile", TilesetIndex = -1, TileIndex = -1 } },
             {"SandTile", new TilesetLookup(){ Name = "SandTile", TilesetIndex = -1, TileIndex = -1 } },
             {"GreenTile", new TilesetLookup(){ Name = "GreenTile", TilesetIndex = -1, TileIndex = -1 } },
             {"PaveTile", new TilesetLookup(){ Name = "PaveTile", TilesetIndex = -1, TileIndex = -1 } },
             {"MiscPaveTile", new TilesetLookup(){ Name = "MiscPaveTile", TilesetIndex = -1, TileIndex = -1 } },
             {"ClearToRoughLat", new TilesetLookup(){ Name = "ClearToRoughLat", TilesetIndex = -1, TileIndex = -1 } },
             {"ClearToSandLat", new TilesetLookup(){ Name = "ClearToSandLat", TilesetIndex = -1, TileIndex = -1 } },
             {"ClearToGreenLat", new TilesetLookup(){ Name = "ClearToGreenLat", TilesetIndex = -1, TileIndex = -1 } },
             {"ClearToPaveLat", new TilesetLookup(){ Name = "ClearToPaveLat", TilesetIndex = -1, TileIndex = -1 } },
             {"HeightBase", new TilesetLookup(){ Name = "HeightBase", TilesetIndex = -1, TileIndex = -1 } },
             {"BlackTile", new TilesetLookup(){ Name = "BlackTile", TilesetIndex = -1, TileIndex = -1 } },
             {"BridgeSet", new TilesetLookup(){ Name = "BridgeSet", TilesetIndex = -1, TileIndex = -1 } },
             {"WoodBridgeSet", new TilesetLookup(){ Name = "WoodBridgeSet", TilesetIndex = -1, TileIndex = -1 } },
             {"CliffSet", new TilesetLookup(){ Name = "CliffSet", TilesetIndex = -1, TileIndex = -1 } },
             {"ShorePieces", new TilesetLookup(){ Name = "ShorePieces", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterSet", new TilesetLookup(){ Name = "WaterSet", TilesetIndex = -1, TileIndex = -1 } },
             {"SlopeSetPieces", new TilesetLookup(){ Name = "SlopeSetPieces", TilesetIndex = -1, TileIndex = -1 } },
             {"SlopeSetPieces2", new TilesetLookup(){ Name = "SlopeSetPieces2", TilesetIndex = -1, TileIndex = -1 } },
             {"MonorailSlopes", new TilesetLookup(){ Name = "MonorailSlopes", TilesetIndex = -1, TileIndex = -1 } },
             {"Tunnels", new TilesetLookup(){ Name = "Tunnels", TilesetIndex = -1, TileIndex = -1 } },
             {"TrackTunnels", new TilesetLookup(){ Name = "TrackTunnels", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtTunnels", new TilesetLookup(){ Name = "DirtTunnels", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtTrackTunnels", new TilesetLookup(){ Name = "DirtTrackTunnels", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterfallEast", new TilesetLookup(){ Name = "WaterfallEast", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterfallWest", new TilesetLookup(){ Name = "WaterfallWest", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterfallNorth", new TilesetLookup(){ Name = "WaterfallNorth", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterfallSouth", new TilesetLookup(){ Name = "WaterfallSouth", TilesetIndex = -1, TileIndex = -1 } },
             {"CliffRamps", new TilesetLookup(){ Name = "CliffRamps", TilesetIndex = -1, TileIndex = -1 } },
             {"PavedRoads", new TilesetLookup(){ Name = "PavedRoads", TilesetIndex = -1, TileIndex = -1 } },
             {"PavedRoadEnds", new TilesetLookup(){ Name = "PavedRoadEnds", TilesetIndex = -1, TileIndex = -1 } },
             {"Medians", new TilesetLookup(){ Name = "Medians", TilesetIndex = -1, TileIndex = -1 } },
             {"RoughGround", new TilesetLookup(){ Name = "RoughGround", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtRoadJunction", new TilesetLookup(){ Name = "DirtRoadJunction", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtRoadCurve", new TilesetLookup(){ Name = "DirtRoadCurve", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtRoadStraight", new TilesetLookup(){ Name = "DirtRoadStraight", TilesetIndex = -1, TileIndex = -1 } },
             {"DestroyableCliffs", new TilesetLookup(){ Name = "DestroyableCliffs", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterCaves", new TilesetLookup(){ Name = "WaterCaves", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterCliffs", new TilesetLookup(){ Name = "WaterCliffs", TilesetIndex = -1, TileIndex = -1 } },
             {"PavedRoadSlopes", new TilesetLookup(){ Name = "PavedRoadSlopes", TilesetIndex = -1, TileIndex = -1 } },
             {"DirtRoadSlopes", new TilesetLookup(){ Name = "DirtRoadSlopes", TilesetIndex = -1, TileIndex = -1 } },
             {"Rocks", new TilesetLookup(){ Name = "Rocks", TilesetIndex = -1, TileIndex = -1 } },
             {"WaterBridge", new TilesetLookup(){ Name = "WaterBridge", TilesetIndex = -1, TileIndex = -1 } }
        };

        public static List<IsoTileTypeClass> All = new List<IsoTileTypeClass>();

        public static PAL isoPAL {
            get {
                return MapTheater.isoPAL;
            }
        }

        internal static String GetSuffix(int idx) {
            if (idx == 0) {
                return "";
            }
            return "" + (char)(0x60 + idx);
        }

        internal static void TheaterChanged() {
            All.Clear();
        }

        public static void LoadListFromINI(MapTheater TheaterData, bool arg) {

            String IniName = String.Format("{0}MD.INI", TheaterData.mixName);

            var INIStream = FileSystem.LoadFile(IniName);
            var isoINI = new INI(INIStream);

            int negative = -1;

            foreach (var lookup in TilesetIndices) {
                isoINI.GetInteger("General", lookup.Value.Name, out lookup.Value.TilesetIndex, negative);
            }

            int tsetIdx = 0;
            while (true) {
                String TilesetSection = String.Format("TileSet{0:d4}", tsetIdx);
                if (!isoINI.SectionExists(TilesetSection)) {
                    break;
                }
                var tsCfg = new TilesetConfig() {
                    TilesInSet = -1
                };
                isoINI.GetInteger(TilesetSection, "TilesInSet", out tsCfg.TilesInSet, negative);
                if (tsCfg.TilesInSet == -1) {
                    break;
                }

                foreach (var lookup in TilesetIndices) {
                    if (tsetIdx == lookup.Value.TilesetIndex) {
                        lookup.Value.TileIndex = All.Count;
                    }
                }

                ++tsetIdx;

                isoINI.GetString(TilesetSection, "SetName", out tsCfg.SetName, "No Name");
                isoINI.GetString(TilesetSection, "FileName", out tsCfg.FileName, "TILE");
                isoINI.GetInteger(TilesetSection, "MarbleMadness", out tsCfg.MarbleMadness, 65535);
                isoINI.GetInteger(TilesetSection, "NonMarbleMadness", out tsCfg.NonMarbleMadness, 65535);
                isoINI.GetBool(TilesetSection, "Morphable", out tsCfg.Morphable, false);
                isoINI.GetBool(TilesetSection, "AllowToPlace", out tsCfg.AllowToPlace, true);
                isoINI.GetBool(TilesetSection, "AllowBurrowing", out tsCfg.AllowBurrowing, true);
                isoINI.GetBool(TilesetSection, "AllowTiberium", out tsCfg.AllowTiberium, false);
                isoINI.GetBool(TilesetSection, "RequiredForRMG", out tsCfg.RequiredForRMG, false);
                isoINI.GetInteger(TilesetSection, "ToSnowTheater", out tsCfg.ToSnowTheater, -1);
                isoINI.GetInteger(TilesetSection, "ToTemperateTheater", out tsCfg.ToTemperateTheater, -1);
                isoINI.GetBool(TilesetSection, "ShadowCaster", out tsCfg.ShadowCaster, false);
                if (tsCfg.ShadowCaster) {
                    isoINI.GetInteger(TilesetSection, "ShadowTiles", out tsCfg.ShadowTiles, 0);
                }

                for (var i = 1; i <= tsCfg.TilesInSet; ++i) {
                    var TileFnameBase = String.Format("{0:s}{1:d2}", tsCfg.FileName, i);

                    var variation = 0;
                    var exists = false;
                    IsoTileTypeClass curTile = null;
                    do {
                        var TileFname = String.Format("{0:s}{1:s}.{2:s}", TileFnameBase, GetSuffix(variation), TheaterData.Extension);
                        ++variation;
                        var tileFile = FileSystem.LoadFile(TileFname);
                        if (tileFile == null) {
                            exists = false;
                        } else {
                            var tileVariation = new IsoTileTypeClass() {
                                cfg = tsCfg,
                                IndexInTileset = i,
                                AllowBurrowing = tsCfg.AllowBurrowing,
                                AllowTiberium = tsCfg.AllowTiberium,
                                AllowToPlace = tsCfg.AllowToPlace,
                                Morphable = tsCfg.Morphable,
                                RequiredForRMG = tsCfg.RequiredForRMG,
                                ToSnowTheater = tsCfg.ToSnowTheater,
                                ToTemperateTheater = tsCfg.ToTemperateTheater,
                            };
                            try {
                                var tmpVariation = new TMP(tileFile);
                                tileVariation.Tile = tmpVariation;
                                if (curTile == null) {
                                    curTile = tileVariation;
                                } else {
                                    curTile.NextVariation = tileVariation;
                                }
                                exists = true;
                            } catch (ArgumentException) {
                                // bleh, broken file
                            }
                        }
                        if (curTile == null) {
                            Debug.WriteLine("Failed to load tile {0}{1}", TileFname, ".");
                        }
                    } while (exists);

                    All.Add(curTile);
                }

            }

        }

        internal TMP.TileHeader getSubTile(int idx) {
            return Tile.Tiles[idx % (int)Tile.Header.Area];
        }

        internal bool DrawSubTile(int IsoTileTypeSubIndex, Helpers.ZBufferedTexture tex, CellStruct TopLeft, int CellLevel, bool highlight = false) {
            var t = getSubTile(IsoTileTypeSubIndex);

            var clipped = tex.CopyTexture(t.GetTextureStandalone(isoPAL), new CellStruct(TopLeft.X + t.Bounds.X, TopLeft.Y + t.Bounds.Y), CellLevel * 30, false);

            if (highlight) {
                t.Highlight(tex, TopLeft);
            }

            return clipped;
        }

        public static void PrepaintTiles() {
            foreach (var t in All) {
                if (t != null) {
                    foreach (var c in t.Tile.TilesReal) {
                        c.PrepareTexture(isoPAL);
                    }
                }
            }
        }
    }
}
