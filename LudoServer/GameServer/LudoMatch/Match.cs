using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoMatch
{
    public class Match // V1.21
    {
        private Random random = new Random();
        private const int maxPlayers = 2;

        public int id;  // Match Id
        public int playerNum;
        public Board board;
        public List<string> players; //List of players ids.
        public Dictionary<string, int> connections; // <id, connection>
        public bool playing;
        public int turn;
        public int dice;
        public bool canThrow;
        public bool[] canMove;
        public int[] pieces;

        public Match(int id, string[] boardData)
        {
            this.id = id;
            this.board = new Board(boardData);
            players = new List<string>();
            connections = new Dictionary<string, int>();
        }

        // Server
        public bool RegisterPlayer(string playerId, int playerConnection)
        {
            if (players.Count < maxPlayers)
            {
                players.Add(playerId);
                connections.Add(playerId, playerConnection);
                if (players.Count == maxPlayers) { return true; } // When the last players joins, returns true.
            }
            return false;
        }

        public void Start()
        {
            // Shuffle list
            List<String> players = this.players.OrderBy(x => random.Next()).ToList();
            this.players = players;
            // Set first turn
            turn = 0;
            dice = 0;
            canThrow = true;
            canMove = new bool[] { false, false, false, false };
            pieces = board.getRestPositions(maxPlayers);
        }

        public int Throw()
        {
            canThrow = false;
            dice = random.Next(4, 7);

            bool canMoveAtLeastOne = setCanMove();
            if(!canMoveAtLeastOne)
            {
                canThrow = true;
                turn = (turn + 1) % players.Count();
            }
            return dice;
        }

        private bool setCanMove() // Returns true if the player has at least one piece to move.
        {
            int[] playerPieces = new int[4];
            Array.Copy(pieces, 4 * turn, playerPieces, 0, 4);

            canMove = new bool[]
            {
                ((board.restPositions.Contains(playerPieces[0]) && dice == 6) || !board.restPositions.Contains(playerPieces[0])) && !board.endPositions.Contains(playerPieces[0]),
                ((board.restPositions.Contains(playerPieces[1]) && dice == 6) || !board.restPositions.Contains(playerPieces[1])) && !board.endPositions.Contains(playerPieces[1]),
                ((board.restPositions.Contains(playerPieces[2]) && dice == 6) || !board.restPositions.Contains(playerPieces[2])) && !board.endPositions.Contains(playerPieces[2]),
                ((board.restPositions.Contains(playerPieces[3]) && dice == 6) || !board.restPositions.Contains(playerPieces[3])) && !board.endPositions.Contains(playerPieces[3]),
            };
            return canMove[0] || canMove[1] || canMove[2] || canMove[3];
        }


        public void Play()
        {
            canThrow = true; canMove = new bool[] { false, false, false, false };
            // Set next turn
            turn = (turn + 1) % players.Count();
            // Move Pieces
        }

        public void Move(int piece)
        {
            canThrow = true; canMove = new bool[] { false, false, false, false };
            // Move Pieces
            int newPosition = board.Move(pieces[(turn * 4) + piece], dice, turn);
            Eat(newPosition);
            pieces[(turn * 4) + piece] = newPosition;
            // Set next turn
            if (dice != 6) turn = (turn + 1) % players.Count();
        }

        private void Eat(int position)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                int[] currentPiecesIndexes = Enumerable.Range(turn * 4, 4).ToArray();
                if (pieces[i] == position && !currentPiecesIndexes.Contains(i)) pieces[i] = board.restPositions[i];
            }
        }

        public string[] StartData()
        {
            string[] boardData = board.BoardData();
            string[] data = new string[1 + boardData.Length];
            data[0] = id.ToString();
            Array.Copy(boardData, 0, data, 1, boardData.Length);
            return data;
        }

        public Dictionary<int, string[]> Status()
        {
            Dictionary<int, string[]> info = new Dictionary<int, string[]>();
            foreach (var connection in connections)
            {
                bool playing = (connection.Key == players[turn]);
                int playerNum = players.IndexOf(connection.Key);
                string[] permissions = new string[] {playerNum.ToString(), playing.ToString(), dice.ToString(), canThrow.ToString() };
                string[] pieces = this.pieces.Select(x => x.ToString()).ToArray();
                string[] canMove = this.canMove.Select(b => b.ToString()).ToArray();
                //Join all data
                string[] data = permissions.Concat(canMove).Concat(pieces).ToArray();
                info.Add(connection.Value, data);
            }
            return info;
        }

        // Client
        public void SetFromStatus(string[] status)
        {
            int.TryParse(status[0], out playerNum);
            bool.TryParse(status[1], out playing);
            int.TryParse(status[2], out dice);
            bool.TryParse(status[3], out canThrow);

            this.canMove = new bool[4];
            string[] canMove = new string[4];
            Array.Copy(status, 4, canMove, 0, 4);
            this.canMove = canMove.Select(b => bool.Parse(b)).ToArray();

            this.pieces = new int[status.Count() - 8];
            string[] pieces = new string[status.Count() - 8];
            Array.Copy(status, 8, pieces, 0, status.Count() - 8);
            this.pieces = pieces.Select(x => int.Parse(x)).ToArray();
        }
    }
}
