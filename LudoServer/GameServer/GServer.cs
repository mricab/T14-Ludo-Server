using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using server;
using LudoProtocol;
using LudoMatch;

namespace GameServer
{
    public class GServer : IServer
    {
        // Constants
        private const string usersFile = "./users.txt";
        private const string boardsAddress = "LudoServer.GameServer.Resources.Boards.";

        // Properties
        private static Server TServer;
        private Dictionary<string, User> Users; // string = username
        private Dictionary<int, User> UsersConnected; //int = user.connection
        private static int MatchCount;
        private Dictionary<int, Match> Matches; //int = match number

        // Constructor
        public GServer(int ListeningPort)
        {
            TServer = new Server(ListeningPort, typeof(LPackage));
            TServer.RegisterObserver(this);
            Users = new Dictionary<string, User>();
            UsersConnected = new Dictionary<int, User>();
            MatchCount = 0;
            Matches = new Dictionary<int, Match>();
            CreateUsersFile();          
        }

        // Main Methods
        public void Start()
        {
            LoadUsers();
            TServer.Start();
        }

        public void Stop()
        {
            SaveUsers();
            TServer.Stop();
        }

        private void Send(int connection, string action, string[] contents = null)
        {
            if(contents != null) 
            {
                Console.WriteLine("(GServer)\tClient#{0}: Sending '{1}' [" + LProtocol.ActionCode(action) + "] with [" + string.Join(" ", contents) + "]", connection, action);
                TServer.Send(connection, LProtocol.GetPackage(action, contents));
            }
            else
            {
                Console.WriteLine("(GServer)\tClient#{0}: Sending '{1}' [" + LProtocol.ActionCode(action) + "]", connection, action);
                TServer.Send(connection, LProtocol.GetPackage(action));
            }
        }

        // IServer
        public void OnMessageReceived(ServerMessageEvent e)
        {
            int connection = e.Data.Id;
            LPackage lPackage = (LPackage)e.Data.Obj;
            Console.WriteLine("(GServer)\tClient#{0}: Message Received [{1}]", e.Data.Id, lPackage.ToString());
            AnalizeRequest(connection, lPackage);
        }

        // Actions responses
        private void AnalizeRequest(int connection, LPackage lPackage)
        {
            string actionName = LProtocol.ActionName(lPackage.action);
            switch (actionName)
            {
                case "login":           Login(connection, lPackage.contents);   break;
                case "logout":          Logout(connection);                     break;
                case "register":        Register(connection, lPackage.contents);break;
                case "game create":     CreateGame(connection, lPackage.contents);break;
                case "game join":       JoinGame(connection, lPackage.contents);break;
                case "game play":       GamePlay(connection, lPackage.contents);break;
                case "game throw dice": GameDice(connection, lPackage.contents);break;
                case "game move piece": GameMove(connection, lPackage.contents);break;
                default:                InvalidRequest(connection);             break;
            }
        }

        public void InvalidRequest(int connection)
        {
            TServer.Send(connection, LProtocol.GetPackage("unknown request"));
        }

        public void Login(int connection, string[] contents)
        {
            User user;
            if (Users.TryGetValue(contents[0], out user))
            {
                if (user.password == contents[1])
                {
                    try
                    {
                        // Successful Authentication
                        user.connection = connection;
                        user.token = GetToken();
                        UsersConnected.Add(user.connection, user);
                        Send(connection, "login successful", new string[] { user.token });
                        //TServer.Send(user.connection, LProtocol.GetPackage("login successful", new string[] { user.token }));
                    }
                    catch (ArgumentException e) // Duplicate Key
                    {
                        // Duplicate login
                        Send(connection, "duplicate login");
                        //TServer.Send(connection, LProtocol.GetPackage("duplicate login"));
                    }
                }
                else
                {
                    // Password incorrect
                    Send(connection, "login failure");
                    //TServer.Send(connection, LProtocol.GetPackage("login failure"));
                }              
            }
            else
            {
                // User doesn't exists
                Send(connection, "login failure");
                //TServer.Send(connection, LProtocol.GetPackage("login failure"));
            }
        }

        public void Logout(int connection)
        {
            if (UsersConnected.Remove(connection))
            {
                Send(connection, "logout successful");
                //TServer.Send(connection, LProtocol.GetPackage("logout successful"));
            }
            else
            {
                // Connection not found
                Send(connection, "logout failure");
                //TServer.Send(connection, LProtocol.GetPackage("logout failure"));
            }
        }

        public void Register(int connection, string[] contents)
        {            
            try
            {
                User user = new User(contents[0], contents[1]);
                Users.Add(user.username, user);
                user.connection = connection;
                user.token = GetToken();
                UsersConnected.Add(connection, user);
                Send(connection, "register successful", new string[] { user.token });
                //TServer.Send(user.connection, LProtocol.GetPackage("register successful", new string[] { user.token }));
            }
            catch (ArgumentException e) // Duplicate Key
            {
                // Duplicate user
                Send(connection, "register failure", new string[] { "Username taken." });
                //TServer.Send(connection, LProtocol.GetPackage("register failure", new string[] { "Username taken." }));
            }
        }

        public void CreateGame(int connection, string[] contents)
        {
            User user;
            string[] boardData;
            String[] sendData;
            Match m;
            switch (contents[0])
            {
                case "A":
                    MatchCount++; boardData = LoadBoard("A"); UsersConnected.TryGetValue(connection, out user);
                    m = new Match(MatchCount, boardData);
                    m.RegisterPlayer(user.username, connection);
                    Matches.Add(MatchCount, m);
                    Send(connection, "game data", m.StartData());
                    break;
                case "B":
                    MatchCount++; boardData = LoadBoard("B"); UsersConnected.TryGetValue(connection, out user);
                    m = new Match(MatchCount, boardData);
                    m.RegisterPlayer(user.username, connection);
                    Matches.Add(MatchCount, m);
                    Send(connection, "game data", m.StartData());
                    break;
                default:    
                    InvalidRequest(connection);
                    break;
            }
        }

        public void JoinGame(int connection, string[] contents)
        {
            int gameId; int.TryParse(contents[0], out gameId);
            Match m; Matches.TryGetValue(gameId, out m);
            User user; UsersConnected.TryGetValue(connection, out user);

            String[] sendData;
            String[] boardCells = m.board.cells;
            sendData = new string[1 + 1 + boardCells.Length];
            sendData[0] = MatchCount.ToString();
            sendData[1] = m.board.size.ToString();
            Array.Copy(boardCells, 0, sendData, 2, boardCells.Length);
            Send(connection, "game data", sendData);
            //TServer.Send(connection, LProtocol.GetPackage("game data", sendData));
            if (m.RegisterPlayer(user.username, connection))
            {
                StartGame(gameId);
            }
        }

        public void StartGame(int gameId)
        {
            Match m; Matches.TryGetValue(gameId, out m);
            m.Start();
            Dictionary<int, string[]> status = m.Status();
            foreach (var item in status)
            {
                Send(item.Key, "game status", item.Value);
                //TServer.Send(item.Key, LProtocol.GetPackage("game status", item.Value));
            }
        }

        public void GamePlay(int connection, string[] contents)
        {
            int gameId; int.TryParse(contents[0], out gameId);
            Match m; Matches.TryGetValue(gameId, out m);
            m.Play();
            Dictionary<int, string[]> status = m.Status();
            foreach (var item in status)
            {
                Send(item.Key, "game status", item.Value);
                //TServer.Send(item.Key, LProtocol.GetPackage("game status", item.Value));
            }
        }

        public void GameDice(int connection, string[] contents)
        {
            int gameId; int.TryParse(contents[0], out gameId);
            Match m; Matches.TryGetValue(gameId, out m);
            m.Throw();
            Dictionary<int, string[]> status = m.Status();
            foreach (var item in status)
            {
                Send(item.Key, "game status", item.Value);
                //TServer.Send(item.Key, LProtocol.GetPackage("game status", item.Value));
            }
        }

        public void GameMove(int connection, string[] contents)
        {
            int gameId; int.TryParse(contents[0], out gameId);
            int piece; int.TryParse(contents[1], out piece);
            Match m; Matches.TryGetValue(gameId, out m);
            m.Move(piece);
            Dictionary<int, string[]> status = m.Status();
            foreach (var item in status)
            {
                Send(item.Key, "game status", item.Value);
            }
        }

        // Load Board
        private string[] LoadBoard(string boardType)
        {
            // https://stackoverflow.com/questions/11996803/c-sharp-where-to-place-txt-files
            string[] data; char[] separators = new char[] { ' ', '\t', '\n'};
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(boardsAddress+boardType+".txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    data = reader.ReadToEnd().Split(separators);                 
                }
            }
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = data[i].Trim();
            }
            return data;
        }

        // Users Methods
        private void CreateUsersFile()
        {
            if (!File.Exists(usersFile))
            {
                File.Create(usersFile).Close();
                GenerateSampleUsers(usersFile);
            }
        }
        public void GenerateSampleUsers(string usersFile)
        {
            if (File.Exists(usersFile))
            {
                StreamWriter writer = new StreamWriter(usersFile);
                String[] names = new String[] { "ana", "jaime", "dani", "vico" };
                foreach (var name in names)
                {
                    writer.WriteLine(name+"|"+"1234");
                }
                writer.Close();
            }
        }

        private void LoadUsers()
        {
            if (File.Exists(usersFile))
            {
                StreamReader stream = new StreamReader(usersFile);
                string line;
                while ((line = stream.ReadLine()) != null)
                {
                    int sepIdx = line.IndexOf("|");
                    string name = line.Substring(0, sepIdx);
                    string pass = line.Substring(sepIdx + 1, line.Length-sepIdx-1);
                    //Console.WriteLine(name + "(" + name.Length + ")" + " " + pass + "(" + pass.Length + ")");
                    Users.Add(name, new User(name, pass));
                }
                stream.Close();
            }
            else
            {
                throw new Exception("Users data file not found.");
            }
        }

        private void SaveUsers()
        {
            File.Create(usersFile).Close();
            StreamWriter writer = new StreamWriter(usersFile);
            foreach (var item in Users)
            {
                writer.WriteLine(item.Value.username + "|" + item.Value.password);
            }
            writer.Close();
        }

        // Helpher Methods
        private string GetToken()
        {
            Random r = new Random();
            return r.Next().ToString();
        }
    }
}
