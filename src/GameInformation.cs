using System;
using System.Collections.Generic;
using System.Text;

namespace BorderlandsNumPlayersSetter
{
    public class GameInformation
    {
        static GameInformation()
        {
            Games = new List<GameInformation>();
            Games.Add(new GameInformation()
            {
                ProcessName = "Borderlands",
                ValToFind = new byte[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, 1, 00, 00, 00, 1, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToFindWildcard = new byte?[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, default(byte?), 00, 00, 00, default(byte?), 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToChangeOffset = new int[] { 20, 24 }
            });
            Games.Add(new GameInformation()
            {
                ProcessName = "Borderlands2",
                ValToFind = new byte[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, 1, 00, 00, 00, 1, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToFindWildcard = new byte?[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, default(byte?), 00, 00, 00, default(byte?), 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToChangeOffset = new int[] { 20, 24 }
            });
            Games.Add(new GameInformation()
            {
                ProcessName = "BorderlandsPreSequel",
                ValToFind = new byte[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, 1, 00, 00, 00, 1, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToFindWildcard = new byte?[] { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 04, 00, 00, 00, default(byte?), 00, 00, 00, default(byte?), 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 01, 00, 00, 00 },
                ValToChangeOffset = new int[] { 20, 24 }
            });
        }

        public static List<GameInformation> Games { get; set; }
        public string ProcessName { get; set; }
        public byte?[] ValToFindWildcard { get; set; }
        public byte[] ValToFind { get; set; }
        public int[] ValToChangeOffset { get; set; }
    }
}
