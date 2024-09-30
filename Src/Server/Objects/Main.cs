using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using _7YMedioServer.Objects.Data;
using System.Diagnostics;
using _7YMedioServer.Objects.Service;
using static System.Formats.Asn1.AsnWriter;

namespace _7YMedioServer.Objects
{

    public class Main
    {

        private static readonly int TICKS_PER_SECOND = 20;

        private TcpListener server = new TcpListener(IPAddress.Any, 25565);
        private List<User> users = new List<User>();
        private List<KeyValuePair<User, String>> packets = new List<KeyValuePair<User, string>>();

        private CardContainerService cardContainerService = new CardContainerService();

        public void Start()
        {
            server.Start();

            Console.WriteLine("Listening to address *:25565");

            new Thread(AcceptConnection).Start();

            StartGame();
        }

        private void AcceptConnection()
        {
            while (server.Server.IsBound)
            {
                TcpClient client = server.AcceptTcpClient();

                users.Add(new User(this, client, delegate (User user, String packet)
                {
                    this.packets.Add(new KeyValuePair<User, String>(user, packet));
                }));

                Console.WriteLine("Added new user");
            }
        }

        private void StartGame()
        {
            WaitUntil(delegate () { return this.users.Count > 1; });

            Console.WriteLine("Starting game");

            int i = 0;
            while (this.users.Find(delegate (User user) { return user.GetVictories() >= 3; }) == null)
            {
                ServerAgainstUser(users[i = (++i % users.Count)]);
            }

            User? winner = users.Find(delegate (User user) { return user.GetVictories() >= 3; });
            if (winner != null)
            {
                foreach (User user in this.users)
                {
                    if (user != winner)
                    {
                        user.SendPacket("MATCHWIN");
                    }
                    else
                    {
                        user.SendPacket("MATCHLOSE");
                    }
                }
            }

            this.server.Stop();
        }

        private void ServerAgainstUser(User user)
        {
            double score = 0;

            this.cardContainerService.ResetDeck();

            WaitUntil(delegate ()
            {
                List<KeyValuePair<User, String>> packets = new List<KeyValuePair<User, string>>();

                foreach (KeyValuePair<User, String> packet in packets)
                {
                    if (packet.Key != user)
                    {
                        return false;
                    }

                    String[] args = packet.Value.Split();

                    switch (args[0].ToUpper())
                    {
                        case "PICK":
                            Card card = this.cardContainerService.PullCard();
                            score += card.GetValue();
                            if (score > 7.5F)
                            {
                                user.SendPacket("LOSE|" + score);
                                return true;
                            }
                            else
                            {
                                user.SendPacket("CARD|" + card.GetValue() + "|" + score);
                            }
                            return false;
                        case "STAND":
                            return true;
                    }
                }
                return false;
            });

            if (score > 7.5F)
            {
                return;
            } else
            {
                double serverScore = 0D;

                while (serverScore < score && serverScore < 7.5D)
                {
                    serverScore += this.cardContainerService.PullCard().GetValue();
                }

                if (serverScore == 7.5D || serverScore >= score)
                {
                    user.SendPacket("LOSE|" + score + "|" + serverScore);
                } else
                {
                    user.AddVictory();
                    user.SendPacket("WIN|" + score + "|" + serverScore);
                }
            }

        }

        private void Broadcast(String data)
        {
            foreach (var user in users)
            {
                user.SendPacket(data);
            }
        }

        private void WaitUntil(Func<Boolean> condition)
        {
            while (!condition())
            {
                Thread.Sleep(1000 / TICKS_PER_SECOND);
            }
        }
            
    }
}
