using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace GameServer
{
  public class Server:SingletonMono<Server>
    {
        private IPEndPoint m_IpEndPoint;
        private Socket m_ServerSocket;
        private List<Client> m_ClientList = new List<Client>();
        private List<Room> m_RoomList = new List<Room>();

        private int m_ClientID = 0;
        public void Init(string ipStr, int port)
        {
            Physics.autoSimulation = false;
            SetIpAndPort(ipStr, port);
            Init();
        }

        public void SetIpAndPort(string ipStr, int port)
        {
            m_IpEndPoint = new IPEndPoint(IPAddress.Parse(ipStr), port);
        }

        public void Init()
        {
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_ServerSocket.Bind(m_IpEndPoint);
            m_ServerSocket.Listen(0);
            m_ServerSocket.BeginAccept(AcceptCallBack, null);
        }
        private void AcceptCallBack(IAsyncResult ar)
        {
            Debug.Log("接收到回调");
            Socket clientSocket = m_ServerSocket.EndAccept(ar);
            Room r;
            if (m_RoomList.Count != 0)
                r = m_RoomList[0];
            else
            {
                r = new Room();
                m_RoomList.Add(r);
                r.Init();
            }
            Client client = new Client(clientSocket, this, r, m_ClientID);
            client.Start();
            m_ClientList.Add(client);
            r.PushClient(client);
            m_ServerSocket.BeginAccept(AcceptCallBack, null);
            m_ClientID++;
        }
        public void RemoveClient(Client client)
        {
           Debug.Log("玩家" + client.m_ClientData.m_ClientID + "退出了房间");
            lock (m_ClientList)
            {
                m_ClientList.Remove(client);
            }

            lock (m_RoomList)
            {
                if (m_RoomList.Count > 0) {
                    m_RoomList[0].PopClient(client);
                }
            }
        }
        public void FixedUpdate()
        {
            foreach (var item in m_RoomList)
            {
                item.FixedUpdate();
            }
        }
        public void OnDisable()
        {
            if (m_ServerSocket != null) {
                m_ServerSocket.Close();
            }
        }

    }
}
