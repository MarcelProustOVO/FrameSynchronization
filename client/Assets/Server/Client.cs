using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using UnityEngine;

namespace GameServer
{
    public class Client
    {
        private Socket m_ClientSocket;
        private Server m_Server;
        ByteBuffer m_ByteBuffer;
        public ClientData m_ClientData;
        public Room m_Room; //自身所在的房间
        ConcurrentQueue<Action> ClientEvent = new ConcurrentQueue<Action>();         //从子线程事件返回主线程执行
        public Client(Socket clientSocket, Server server, Room room, int id)
        {
            this.m_ClientSocket = clientSocket;
            this.m_Server = server;
            byte[] bt = new byte[1024];
            m_ByteBuffer = new ByteBuffer(bt);
            m_ClientData = new ClientData(id,room);
            m_Room = room;
        }
        public void Start()
        {
            if (m_ClientSocket == null || m_ClientSocket.Connected == false) return;
            m_ClientSocket.BeginReceive(m_ByteBuffer.mBytes, m_ByteBuffer.mEffectiveHead, m_ByteBuffer.m_RemainSize, SocketFlags.None, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (m_ClientSocket == null || m_ClientSocket.Connected == false) return;
                int count = m_ClientSocket.EndReceive(ar);
                if (count == 0)
                {
                    ClientEvent.Enqueue(Close);
                    return;
                }
                m_ByteBuffer.SetEffectiveHead(count);
                Analysis();
                Start();
            }
            catch (Exception e)
            {
                Debug.Log(e);
                ClientEvent.Enqueue(Close);
            }
        }
        private void Analysis()
        {
            int length = m_ByteBuffer.ReadInt32();
            int cur = 0;
            if (length == -1)
                return;
            if ((length) <= m_ByteBuffer.mEffectiveHead)
            {
                int subLenght = m_ByteBuffer.ReadInt32();
                byte[] data = m_ByteBuffer.ReadByte(subLenght);
                Type type = Type.Parser.ParseFrom(data);
                cur += 4;
                cur += subLenght;
                if (type.Type_ == OperationType.Move)
                {
                    while (cur < length)
                    {
                        subLenght = m_ByteBuffer.ReadInt32();
                        data = m_ByteBuffer.ReadByte(subLenght);
                        cur += 4;
                        cur += subLenght;
                        m_ClientData.Push(data);
                    }
                }
                else //如果是登录请求
                {
                    subLenght = m_ByteBuffer.ReadInt32();
                    data = m_ByteBuffer.ReadByte(subLenght);
                    LoginRequest loginRequest = LoginRequest.Parser.ParseFrom(data);
                    m_ClientData.Init(loginRequest.IsPhysics);
                    Debug.Log("玩家" + m_ClientData.m_ClientID + "进入房间");
                    m_Room.LoginSend(m_ClientData.m_ClientID, loginRequest.Name, loginRequest.IsPhysics);
                    m_ByteBuffer.Clear();
                }
            }
        }
        private void Close()
        {
            lock (m_ClientSocket)
            {
                m_ClientSocket.Close();
            }
            m_ClientData.Destroy();
            m_Server.RemoveClient(this);
        }
        public void Send(byte[] bt)
        {
            lock (m_ClientSocket)
            {
                m_ClientSocket.Send(bt);
            }
        }

        public byte[] DataAssemble()
        {
            if (m_ClientData == null)
                return null;
            return m_ClientData.DataAssemble();
        }

        //每帧执行
        public void Simulate(int frame)
        {
            EventUpdate();
            if (m_ClientData == null)
                return;
            m_ClientData.Simulate(frame);
        }

        public void Result(int frame) {
            if (m_ClientData == null)
                return;
            m_ClientData.Result(frame);
        }

        public void EventUpdate()
        {
            Action action;
            while (ClientEvent.TryDequeue(out action))
            {
                action();
            }
        }
    }
}