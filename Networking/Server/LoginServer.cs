﻿namespace AnCoFT.Networking.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using AnCoFT.Database;
    using AnCoFT.Networking.Packet;
    using AnCoFT.Networking.Server.Base;

    public class LoginServer : TcpServer
    {
        private readonly PacketHandler _packetHandler = new PacketHandler();

        public LoginServer(string ipAddress, int port, DatabaseContext databaseContext) : base(ipAddress, port, databaseContext)
        {
        }

        public override void ListenerThread()
        {
            this.Listener.Start();
            while (!this.Stopped)
            {
                try
                {
                    Client client = new Client(this.Listener.AcceptTcpClient(), this.DatabaseContext);
                    Thread receivingThread = new Thread(this.ReceivingThread);
                    receivingThread.Start(client);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        public override void ReceivingThread(object o)
        {
            Client client = (Client)o;
            PacketStream clientStream = client.PacketStream;
            byte[] clientBuffer = new byte[4096];

            this._packetHandler.SendWelcomePacket(client);

            while ((!this.Stopped) && (clientStream.Read(clientBuffer, 0, 8) != 0))
            {
                if (BitConverter.ToInt16(clientBuffer, 6) > 0)
                {
                    clientStream.Read(clientBuffer, 8, BitConverter.ToInt16(clientBuffer, 6));
                }

                Packet packet = new Packet(clientBuffer);
                Console.WriteLine($"RECV [{packet.PacketId:X4}] {BitConverter.ToString(packet.GetRawPacket(), 0, packet.DataLength + 8)}");
                this._packetHandler.HandlePacket(client, packet);
            }
        }
    }
}