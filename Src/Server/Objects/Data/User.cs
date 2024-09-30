using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace _7YMedioServer.Objects.Data
{
    public class User
    {

        private static readonly char[] NEWLINE_CHARS = { '\r', '\n' };

        private Main server;
        private TcpClient client;
        private Action<User, String> onPacket;
        private int victories;

        public User(Main server, TcpClient client, Action<User, String> onPacket)
        {
            this.server = server;
            this.client = client;
            this.onPacket = onPacket;

            new Thread(ListenPacket).Start();
        }

        public void ListenPacket()
        {
            byte[] buffer = new byte[1024];
            NetworkStream stream = client.GetStream();

            int amount;
            StringBuilder stringBuilder = new StringBuilder();
            while (stream.Socket.Connected)
            {
                try
                {
                    amount = stream.Read(buffer, 0, buffer.Length);
                } catch (System.IO.IOException ex)
                {
                    Console.WriteLine("Disconnected user");
                    break;
                }

                if (amount > 0)
                {
                    String fragment = Encoding.UTF8.GetString(buffer);

                    if (fragment.Length == 0)
                    {
                        continue;
                    }

                    int indexOf = 0;
                    int charIndexOf = fragment.Length;

                    do
                    {
                        foreach (char c in NEWLINE_CHARS)
                        {
                            int currentCharIndexOf = fragment.IndexOf(c, indexOf);
                            if (charIndexOf == -1 || currentCharIndexOf < charIndexOf) {
                                charIndexOf = currentCharIndexOf;
                            }
                        }

                        if (charIndexOf != -1 && (charIndexOf - indexOf) > 0)
                        {
                            if (stringBuilder.Length > 0)
                            {
                                stringBuilder.Append(fragment.Substring(indexOf, (charIndexOf - indexOf)));

                                this.onPacket(this, stringBuilder.ToString());

                                stringBuilder.Clear();
                            } else
                            {
                                this.onPacket(this, fragment.Substring(indexOf, (charIndexOf - indexOf)));
                            }
                        }

                        if (charIndexOf != -1)
                        {
                            indexOf = charIndexOf + 1;
                            charIndexOf = fragment.Length;
                        }
                    } while (charIndexOf != -1);

                    stringBuilder.Append(fragment.Substring(indexOf));
                }
            }
        }

        public void SendPacket(String packet)
        {
            this.client.GetStream().Write(Encoding.UTF8.GetBytes(packet));
            this.client.GetStream().Flush();
        }

        public void AddVictory()
        {
            this.victories += 1;
        }

        public int GetVictories()
        {
            return this.victories;
        }

    }
}
