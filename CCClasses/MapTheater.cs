using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCClasses.FileFormats.Binary;

namespace CCClasses {
    public class MapTheater {

        public static readonly Dictionary<String, MapTheater> Theaters = new Dictionary<String, MapTheater>() {
            {"TEMPERATE", new MapTheater() {
                Name = "TEMPERATE", CSFName = "Name:Temperate",
                mixName = "TEMPERAT", isoName1 = "ISOTEMP", isoName2 = "ISOTEM",
                Extension = "TEM", MarbleExtension = "MMT", NewTheaterChar = 'T'
            }},
            {"SNOW", new MapTheater() {
                Name = "SNOW", CSFName = "Name:Snow",
                mixName = "SNOW", isoName1 = "ISOSNOW", isoName2 = "ISOSNO",
                Extension = "SNO", MarbleExtension = "MMS", NewTheaterChar = 'S'
            }},
            {"URBAN", new MapTheater() {
                Name = "URBAN", CSFName = "Name:Urban",
                mixName = "URBAN", isoName1 = "ISOURB", isoName2 = "ISOURB",
                Extension = "URB", MarbleExtension = "MMU", NewTheaterChar = 'U'
            }},
            {"DESERT", new MapTheater() {
                Name = "DESERT", CSFName = "Name:Desert",
                mixName = "DESERT", isoName1 = "ISODES", isoName2 = "ISODES",
                Extension = "DES", MarbleExtension = "MMD", NewTheaterChar = 'D'
            }},
            {"NEWURBAN", new MapTheater() {
                Name = "NEWURBAN", CSFName = "Name:New Urban",
                mixName = "URBANN", isoName1 = "ISOUBN", isoName2 = "ISOUBN",
                Extension = "UBN", MarbleExtension = "MMT", NewTheaterChar = 'N'
            }},
            {"LUNAR", new MapTheater() {
                Name = "LUNAR", CSFName = "Name:Lunar",
                mixName = "LUNAR", isoName1 = "ISOLUN", isoName2 = "ISOLUN",
                Extension = "LUN", MarbleExtension = "MML", NewTheaterChar = 'L'
            }}

        };

        public String Name;
        public String CSFName;
        public String mixName;
        public String isoName1, isoName2;
        public String Extension, MarbleExtension;
        public char NewTheaterChar;

        private List<MIX> Mixes = new List<MIX>();

        internal static MapTheater CurrentTheater;

        internal static PAL isoPAL;
        internal static PAL unitPAL;

        public static void Init(MapTheater tData) {

            var mixFiles = new List<String>() {
                String.Format("{0:s}.MIX", tData.mixName),
                String.Format("{0:s}MD.MIX", tData.mixName),
                String.Format("{0:s}.MIX", tData.isoName1),
                String.Format("{0:s}MD.MIX", tData.isoName2),
                String.Format("{0:s}.MIX", tData.Extension),
            };

            if (CurrentTheater != tData) {
                if (CurrentTheater != null) {
                    CurrentTheater.Mixes.ForEach(M => FileSystem.UnloadMIX(M));
                    CurrentTheater.Mixes.Clear();
                }

                CurrentTheater = tData;
                IsoTileTypeClass.TheaterChanged();

                foreach (var mixName in mixFiles) {
                    var M = FileSystem.LoadMIX(mixName);
                    if (M != null) {
                        CurrentTheater.Mixes.Add(M);
                    }
                }

                String PalName = String.Format("ISO{0}.PAL", CurrentTheater.Extension);
                var PalStream = FileSystem.LoadFile(PalName);
                if (PalStream != null) {
                    isoPAL = new PAL(PalStream);
                } else {
                    isoPAL = null;
                }

                PalName = String.Format("UNIT{0}.PAL", CurrentTheater.Extension);
                PalStream = FileSystem.LoadFile(PalName);
                if (PalStream != null) {
                    unitPAL = new PAL(PalStream);
                } else {
                    unitPAL = null;
                }
            }
        }
    }
}
