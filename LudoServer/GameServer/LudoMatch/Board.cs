using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoMatch
{
    public class Board // V1.15
    {
        public int size;
        public string[] cells;
        public int[] restPositions;
        public int[] startPositions;
        public int[] endPositions;
        public List<int> road;
        public List<int> p1Road;
        public List<int> p2Road;
        public List<int> p3Road;
        public List<int> p4Road;

        public Board(string[] data)
        {
            int.TryParse(data[0], out this.size);
            cells = new string[data.Length - 1];
            Array.Copy(data, 1, cells, 0, data.Length - 1);
            findRestPositions(); findStartPositions(); findEndPositions();
            findRoad(); findPlayersRoads();
        }

        private void findRestPositions()
        {
            restPositions = new int[16]; //0-3 -> p1, 4-7 -> p2, 8-11 -> p3, 12-15 -> p4
            int p1 = 0; int p2 = 4; int p3 = 8; int p4 = 12;
            for (int i = 0; i < cells.Length; i++)
            {
                int cell = int.Parse(cells[i]);
                if (cell >= 11 && cell <= 14)
                {
                    switch (cell)
                    {
                        case 11: restPositions[p1] = i; p1++; break;    // Red      p1
                        case 12: restPositions[p2] = i; p2++; break;    // Yellow   p2
                        case 13: restPositions[p3] = i; p3++; break;    // Green    p3
                        case 14: restPositions[p4] = i; p4++; break;    // Blue     p4
                        default: break;
                    }
                }
            }
        }

        public int[] getRestPositions(int players)
        {
            int[] data = new int[players * 4];
            Array.Copy(restPositions, 0, data, 0, players * 4);
            return data;
        }

        private void findStartPositions()
        {
            startPositions = new int[4]; //0 -> p1, 1 -> p2, 2 -> p3, 3 -> p4
            for (int i = 0; i < cells.Length; i++)
            {
                int cell = int.Parse(cells[i]);
                if (cell >= 21 && cell <= 24)
                {
                    switch (cell)
                    {
                        case 21: startPositions[0] = i; break;    // Red      p1
                        case 22: startPositions[1] = i; break;    // Yellow   p2
                        case 23: startPositions[2] = i; break;    // Green    p3
                        case 24: startPositions[3] = i; break;    // Blue     p4
                        default: break;
                    }
                }
            }
        }

        private void findEndPositions()
        {
            endPositions = new int[4]; //0 -> p1, 1 -> p2, 2 -> p3, 3 -> p4
            for (int i = 0; i < cells.Length; i++)
            {
                int cell = int.Parse(cells[i]);
                if (cell >= 40 && cell <= 48)
                {
                    switch (cell)
                    {
                        case 40: endPositions[0] = i; break;    // Red      p1
                        case 46: endPositions[1] = i; break;    // Yellow   p2
                        case 44: endPositions[2] = i; break;    // Green    p3
                        case 42: endPositions[3] = i; break;    // Blue     p4
                        default: break;
                    }
                }
            }
        }

        private void findRoad()
        {
            road = new List<int>();

            string[] directions = new string[] {
                "up", "right", "up-right", "up", "right", "down", "down-right", "right", "down", "left", 
                "down-left", "down", "left", "up", "up-left", "left" 
            };
            
            int start = startPositions[0];
            road.Add(start);
            int dir = 4;
            int last = start;
            int next = start + size;
            
            while(next != start)
            {
                int cell = int.Parse(cells[next]);
                if (cell >= 20 && cell < 25)
                {
                    if (road.Contains(next)) { dir = (dir + 1) % 16; }
                    else { road.Add(next); Console.Write(next + " "); }
                }
                else
                {
                    next = last;
                    dir = (dir + 1) % 16;
                }
                if (directions[dir] == "down" && next + size < size*size) { last = next; next = next + size;  }
                if (directions[dir] == "down-right") { last = next; next = next + size + 1; }
                if (directions[dir] == "right" && (next + 1) % size != 0) { last = next; next = next + 1; }
                if (directions[dir] == "left" && next % size != 0) { last = next; next = next - 1; };
                if (directions[dir] == "down-left") { last = next; next = next + size - 1; }
                if (directions[dir] == "up" && next - size > 0) { last = next; next = next - size; }
                if (directions[dir] == "up-left") { last = next; next = next - size - 1; }
                if (directions[dir] == "up-right") { last = next; next = next - size + 1; }
            }
        }

        public void findPlayersRoads()
        {
            p1Road = new List<int>();
            p2Road = new List<int>();
            p3Road = new List<int>();
            p4Road = new List<int>();

            for (int i = 0; i < cells.Length; i++)
            {
                int cell = int.Parse(cells[i]);
                if ((cell >= 31 && cell <= 34) || (cell >= 40 && cell <= 48))
                {
                    switch (cell)
                    {
                        case 31: p1Road.Add(i); break;    // Red      p1
                        case 32: p2Road.Add(i); break;    // Yellow   p2
                        case 33: p3Road.Add(i); break;    // Green    p3
                        case 34: p4Road.Add(i); break;    // Blue     p4
                        case 40: p1Road.Add(i); break;    // Red      p1
                        case 46: p2Road.Add(i); break;    // Yellow   p2
                        case 44: p3Road.Add(i); break;    // Green    p3
                        case 42: p4Road.Add(i); break;    // Blue     p4
                        default: break;
                    }
                }
            }

            p1Road.Sort();
            p2Road.Sort();
            p3Road.Sort(); p3Road.Reverse();
            p4Road.Sort(); p4Road.Reverse();
        }

        public int Move(int startPosition, int positions, int player)
        {
            if(restPositions.Contains(startPosition)) // At rest area
            {
                int index = Array.FindIndex(restPositions, pos => pos==startPosition);
                if (index < 4)  { return startPositions[0]; }   //p1
                if (index < 8)  { return startPositions[1]; }   //p2
                if (index < 12) { return startPositions[2]; }   //p3
                else            { return startPositions[3]; }   //p4
            }
            else if (p1Road.Contains(startPosition) || p2Road.Contains(startPosition) || p3Road.Contains(startPosition) || p4Road.Contains(startPosition))
            {
                int index;
                switch (player)
                {
                    case 0: index = p1Road.FindIndex(pos => pos == startPosition); break;  //p1
                    case 1: index = p2Road.FindIndex(pos => pos == startPosition); break;  //p2
                    case 2: index = p3Road.FindIndex(pos => pos == startPosition); break;  //p3
                    case 3: index = p4Road.FindIndex(pos => pos == startPosition); break;  //p4
                    default: throw new Exception("Unknown player");
                }
                if ( index + positions <= (p1Road.Count-1) )
                {
                    index = index + positions;
                }
                else
                {
                    int extraPositions = (index + positions) - (p1Road.Count - 1);
                    index = (p1Road.Count - 1) - extraPositions;
                }
                switch (player)
                {
                    case 0: return p1Road[index];   //p1
                    case 1: return p2Road[index];   //p2
                    case 2: return p3Road[index];   //p3
                    case 3: return p4Road[index];   //p4
                    default: throw new Exception("Unknown player");
                }
            }
            else // At road
            {
                int dist = distance(startPosition, player);

                if ( dist + positions > (road.Count-1) )
                {
                    positions = (dist + positions) - (road.Count - 1);
                    switch (player)
                    {
                        case 0: return p1Road[positions];   //p1
                        case 1: return p2Road[positions];   //p2
                        case 2: return p3Road[positions];   //p3
                        case 3: return p4Road[positions];   //p4
                        default: throw new Exception("Unknown player");
                    }
                }               
                else
                {
                    int index = road.FindIndex(pos => pos == startPosition);
                    return road[(index + positions) % road.Count];
                }
            }
        }

        private int distance(int position, int player)
        {
            int index = road.FindIndex(pos => pos == position);
            int startIndex = road.FindIndex(pos => pos == startPositions[player]);
            Console.WriteLine("{0} -> {1}", index, startIndex);
            if (index >= startIndex) { return index - startIndex; }
            else { return (road.Count - startIndex) + index; }
        }

        public string[] BoardData()
        {
            string[] data = new string[1 + cells.Length];
            data[0] = size.ToString();
            Array.Copy(cells, 0, data, 1, cells.Length);
            return data;
        }
    }
}
