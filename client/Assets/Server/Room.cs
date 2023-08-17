using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameServer
{
    public class Room
    {
        const int c_MaxCount = 10;
        //static int S_RoomId = 0;
        List<Client> m_ClientList = new List<Client>();
        bool m_IsTaskRun = true;
        //Stopwatch m_StopWatch;
        //double m_Accumulator = 0;


        Stopwatch m_StopWatch2;
        double m_Accumulator2 = 0;

        double m_FrameMsLength = 20;//服务器帧率也是1秒50次
        double m_FrameLerp = 0;
        double m_SendInterval = 100;//服务器下发1秒10次同步
        public int m_FrameId
        {
            get;
            private set;
        }
        public void Init()
        {
            m_IsTaskRun = true;
            //m_StopWatch = new Stopwatch();
            m_StopWatch2 = new Stopwatch();
            //Task task = Task.Factory.StartNew(Update, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(UpdateSend, TaskCreationOptions.LongRunning);
        }

        public void PushClient(Client client)
        {
            if (!m_ClientList.Contains(client) && !IsFull())
                m_ClientList.Add(client);
        }

        public int PopClient(Client client)
        {
            if (m_ClientList.Contains(client))
            {
                m_ClientList.Remove(client);
            }

            if (m_ClientList.Count == 0)
            {
                Close();
            }
            return m_ClientList.Count;
        }

        public void Close()
        {
            m_ClientList.Clear();

        }

        public void Send()
        {
            ByteBuffer bb = new ByteBuffer();
            for (int i = 0; i < m_ClientList.Count; i++)
            {
                byte[] bt = m_ClientList[i].DataAssemble();
                bb.WriteBytesNoLength(bt);
            }
            //如果没有东西
            if (bb.mEffectiveHead == 0)
                return;
            Type t = new Type();
            t.Type_ = OperationType.Move;
            byte[] b = t.ToByteArray();
            bb.WriteBytes(b, false);
            //从头开始
            bb.WriteInt32(bb.mEffectiveHead, false);
            //Console.WriteLine("本次长度："+bb.GetEffectiveHead);
            for (int i = 0; i < m_ClientList.Count; i++)
            {
                if (m_ClientList[i].m_ClientData.m_LoginOver)
                    m_ClientList[i].Send(bb.mGetbuffer());
            }
        }

        public void LoginSend(int id, string name, bool isPhysics)
        {
            var length = 0;
            ByteBuffer bb = new ByteBuffer();

            Type t = new Type();
            t.Type_ = OperationType.Login;
            byte[] bt = t.ToByteArray();
            bb.WriteBytes(bt);
            length += bt.Length;

            //将第一条登录消息作为发送请求一方
            LoginResponse login = new LoginResponse();
            LoginResponse.Types.Data data = new LoginResponse.Types.Data();
            data.Id = id;
            data.Info = new Info();
            data.Frame = m_FrameId;
            data.IsPhysics = isPhysics;
            UnityEngine.Debug.Log("同步帧号：" + m_FrameId);
            login.Data.Add(data);

            for (int i = 0; i < m_ClientList.Count; i++)
            {
                if (m_ClientList[i].m_ClientData.m_ClientID != id && m_ClientList[i].m_ClientData.m_LoginOver)
                {
                    data = new LoginResponse.Types.Data();
                    data.Id = m_ClientList[i].m_ClientData.m_ClientID;
                    ClientData.Data firstSimulation = m_ClientList[i].m_ClientData.m_CurData;
                    if (firstSimulation != null)
                    {
                        data.Info = firstSimulation.m_Info;
                        //重置帧号
                        data.Frame = firstSimulation.m_FrameID;
                        data.IsPhysics = m_ClientList[i].m_ClientData.m_IsPhysics;
                    }
                    login.Data.Add(data);
                }
            }
            bb.WriteBytes(login.ToByteArray());
            length += bt.Length;
            bb.WriteInt32(length, false);
            for (int i = 0; i < m_ClientList.Count; i++)
            {
                if (m_ClientList[i].m_ClientData.m_LoginOver)
                    m_ClientList[i].Send(bb.mGetbuffer());
            }
        }

        public bool IsFull()
        {
            if (m_ClientList.Count >= c_MaxCount)
            {
                return true;
            }
            return false;
        }

        public void FixedUpdate()
        {
            if (m_IsTaskRun)
            {
                m_FrameId++;
                //模拟状态全部设置，所有物体在这一帧都加上客户端的操作指令
                Simulation();
                //进行模拟
                Physics.Simulate(Time.fixedDeltaTime);
                //在所有东西模拟结束之后进行数值赋予
                Result();
            }
        }


        private void UpdateSend()
        {
            while (m_IsTaskRun)
            {
                double time = m_StopWatch2.Elapsed.TotalMilliseconds;
                m_StopWatch2.Restart();
                m_Accumulator2 += time;
                while (m_Accumulator2 >= m_SendInterval)
                {
                    Send();
                    m_Accumulator2 -= m_SendInterval;
                }
                Thread.Sleep(10);
            }
        }

        private void Simulation()
        {
            int tempCount = m_ClientList.Count;
            for (int i = tempCount - 1; i >= 0; i--)
            {
                m_ClientList[i].Simulate(m_FrameId);
            }
        }

        private void Result()
        {
            int tempCount = m_ClientList.Count;
            for (int i = tempCount - 1; i >= 0; i--)
            {
                m_ClientList[i].Result(m_FrameId);
            }
        }
    }

}
