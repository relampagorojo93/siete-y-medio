using _7YMedioServer.Objects.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace _7YMedioServer.Objects.Data
{
    public enum Action
    {
        ATTACK, DEFENSE, PASS
    }

    public class Constants
    {

        public static readonly int TICKS_PER_SECOND = 20;

    }

    public class User
    {

        private static readonly char[] NEWLINE_CHARS = { '\r', '\n' };

        private Program program;
        private SocketManager socketManager;
        private String username;
        private int health = 100;
        private Action? lastAction = null;

        public User(Program program, SocketManager socketManager, String username, Action<User, String?> onPacket)
        {
            this.program = program;
            this.socketManager = socketManager;
            this.username = username;

            new Thread(delegate () { onPacket(this, this.socketManager.WaitUntilPacket()); });
        }

        public SocketManager GetSocketManager()
        {
            return this.socketManager;
        }

        public String GetUsername()
        {
            return this.username;
        }

        public void SetLastAction(Action lastAction)
        {
            this.lastAction = lastAction;
        }

        public Action? GetLastAction()
        {
            return this.lastAction;
        }

        public void TakeHealth()
        {
            this.health -= (this.lastAction == Action.DEFENSE ? 2 : 20);
        }

        public int GetHealth()
        {
            return this.health;
        }

    }

    public class SocketManager
    {

        private static readonly char[] NEWLINE_CHARS = { '\r', '\n' };

        private TcpClient client;
        private List<String> pendingPackets = new List<String>();

        public SocketManager(TcpClient client)
        {
            this.client = client;

            new Thread(ListenPacket).Start();
        }

        public void ListenPacket()
        {
            byte[] buffer = new byte[1024];
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = Timeout.Infinite;
            stream.WriteTimeout = Timeout.Infinite;

            int amount;
            StringBuilder stringBuilder = new StringBuilder();
            while (stream.Socket.Connected)
            {
                try
                {
                    amount = stream.Read(buffer, 0, buffer.Length);
                }
                catch (System.IO.IOException ex)
                {
                    Console.WriteLine("Disconnected user");
                    break;
                }

                if (amount > 0)
                {
                    String fragment = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(fragment);

                    if (fragment.Length == 0)
                    {
                        continue;
                    }

                    int indexOf = 0;
                    int charIndexOf = fragment.Length;
                    int lastValidIndexOf = 0;

                    do
                    {
                        foreach (char c in NEWLINE_CHARS)
                        {
                            int currentCharIndexOf = fragment.IndexOf(c, indexOf);
                            if (charIndexOf == -1 || currentCharIndexOf < charIndexOf)
                            {
                                charIndexOf = currentCharIndexOf;
                            }
                        }

                        if (charIndexOf != -1 && (charIndexOf - indexOf) > 0)
                        {
                            if (stringBuilder.Length > 0)
                            {
                                stringBuilder.Append(fragment.Substring(indexOf, (charIndexOf - indexOf)));

                                this.pendingPackets.Add(stringBuilder.ToString());

                                stringBuilder.Clear();
                            } else {
                                this.pendingPackets.Add(fragment.Substring(indexOf, (charIndexOf - indexOf)));
                            }

                            lastValidIndexOf = charIndexOf + 1;
                        }

                        if (charIndexOf != -1)
                        {
                            indexOf = charIndexOf + 1;
                            charIndexOf = fragment.Length;
                        }
                    } while (charIndexOf != -1);

                    if (lastValidIndexOf < fragment.Length - 1)
                    {
                        stringBuilder.Append(fragment.Substring(lastValidIndexOf));
                    }
                }
            }
        }

        public void SendPacket(String packet)
        {
            this.client.GetStream().Write(Encoding.UTF8.GetBytes(packet + "\n"));
            this.client.GetStream().Flush();
        }

        public String? WaitUntilPacket()
        {
            WaitUntil(delegate () { return !this.client.Client.IsBound || this.pendingPackets.Count > 0; });

            if (this.pendingPackets.Count > 0)
            {
                String packet = this.pendingPackets[0];

                this.pendingPackets.RemoveAt(0);

                return packet;
            } else
            {
                return null;
            }
        }

        private void WaitUntil(Func<Boolean> condition)
        {
            while (!condition())
            {
                Thread.Sleep(1000 / Constants.TICKS_PER_SECOND);
            }
        }

    }
}

namespace _7YMedioServer.Objects
{

    public class Program
    {

        private TcpListener server = new TcpListener(IPAddress.Any, 25565);
        private List<User> users = new List<User>();
        private List<KeyValuePair<User, String>> packets = new List<KeyValuePair<User, string>>();

        public static void Main(string[] args)
        {
            new Program().Start();
        }


        public void Start()
        {
            server.Start();

            Console.WriteLine("Listening to address *:25565");

            new Thread(AcceptConnection).Start();

            StartGame();
        }

        //DONE
        private void AcceptConnection()
        {
            while (server.Server.IsBound)
            {
                TcpClient client = server.AcceptTcpClient();

                SocketManager socketManager = new SocketManager(client);

                String? usernamePacket = socketManager.WaitUntilPacket();

                if (usernamePacket != null) {
                    users.Add(new User(this, socketManager, usernamePacket, delegate (User user, String? packet)
                    {
                        if (packet != null)
                        {
                            this.packets.Add(new KeyValuePair<User, String>(user, packet));
                        }
                    }));

                    Console.WriteLine("Added new user");
                }
            }
        }

        private void StartGame()
        {
            WaitUntil(delegate () { return this.users.Count > 1; });

            Broadcast("START");

            Console.WriteLine("Starting game");

            int i = 0;
            List<User> users;
            while ((users = this.users.FindAll(delegate (User user) { return user.GetHealth() >= 0; })).Count > 1)
            {
                if (i >= users.Count)
                {
                    i = 0;
                }

                User user = this.users[i = (++i % users.Count)];

                Console.WriteLine("Turn of user " + user.GetUsername());

                //TURNO
                user.GetSocketManager().SendPacket("YOUR_TURN");

                string? packet;

                while ((packet = user.GetSocketManager().WaitUntilPacket()) != null) 
                { 
                    String[] args = packet.Split('|');
                    if (args.Length > 0)
                    {
                        switch (args[0])
                        {
                            case "PLAYERS":
                                List<String> usernames = new List<string>();
                                foreach (User attackUser in users)
                                {
                                    usernames.Add(attackUser.GetUsername());
                                }
                                user.GetSocketManager().SendPacket(String.Join("|", usernames));
                                break;
                            case "ATTACK":
                                User? userAttacked = users.Find(delegate (User u) { return u.GetUsername().Equals(args[1]); });
                                if (userAttacked != null)
                                {
                                    userAttacked.TakeHealth();
                                    //TODO Enviar mensaje a todos los usuarios
                                } else
                                {
                                    user.GetSocketManager().SendPacket("INVALID_USER");
                                }
                                break;
                            case "DEFENSE":
                                break;
                            case "IDLE":
                                break;
                            default:
                                break;
                        }
                    }

                }




                i++;
            }

            User? winner = users.Find(delegate (User user) { return user.GetHealth() > 0; });
            if (winner != null)
            {
                foreach (User user in this.users)
                {
                    if (user != winner)
                    {
                        user.GetSocketManager().SendPacket("MATCHWIN");
                    }
                    else
                    {
                        user.GetSocketManager().SendPacket("MATCHLOSE");
                    }
                }
            }

            this.server.Stop();
        }


        private void Broadcast(String data)
        {
            foreach (var user in users)
            {
                user.GetSocketManager().SendPacket(data);
            }
        }

        private void WaitUntil(Func<Boolean> condition)
        {
            while (!condition())
            {
                Thread.Sleep(1000 / Constants.TICKS_PER_SECOND);
            }
        }

    }
}
