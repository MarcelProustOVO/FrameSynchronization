using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameServer
{
    public class ClientData
    {
        public class Data
        {
            public int m_FrameID;
            public Info m_Info;
        }

        public int m_ClientID
        {
            get;
            private set;
        }

        public bool m_LoginOver
        {
            get;
            private set;
        }
        public bool m_IsPhysics
        {
            get;
            private set;
        }
        private Room m_Room;
        private Transform m_Cube;
        static GameObject m_Go; //只需要一份
        private ServerCube m_CubeServer;
        bool m_IsSimulate;  //是否进行过模拟
        //收到的数据
        ConcurrentQueue<Data> m_ConcurDictionary = new ConcurrentQueue<Data>();
        //开始进行模拟的数据
        ConcurrentQueue<Data> m_ConcurSimulationDictionary = new ConcurrentQueue<Data>();
        //从子线程事件返回主线程执行
        ConcurrentQueue<Action> ClientEvent = new ConcurrentQueue<Action>();

        /// <summary>
        /// 自己模拟的数据
        /// </summary>
        public Data m_CurData
        {
            get;
            private set;
        }
        public ClientData(int id, Room room)
        {
            m_CurData = new Data();
            m_CurData.m_Info = new Info();
            m_CurData.m_Info.Result = new XYZW();
            m_CurData.m_Info.Rotate = new XYZW();
            m_CurData.m_Info.Velocity = new XYZW();
            m_CurData.m_Info.Dir = new XYZW();
            m_CurData.m_Info.AngularVelocity = new XYZW();
            m_CurData.m_Info.Id = id;
            m_ClientID = id;
            ClientEvent.Enqueue(InitPrafeb);
            m_Room = room;
        }

        public void Init(bool isPhysics)
        {
            ClientEvent.Enqueue(CreateSimulant);
            m_LoginOver = true;
            m_IsPhysics = isPhysics;
        }

        //将操作序号推入
        public void Push(byte[] bytes)
        {
            Info info = Info.Parser.ParseFrom(bytes);
            Data data = new Data();
            data.m_FrameID = info.Frame;
            data.m_Info = info;
            m_ConcurDictionary.Enqueue(data);
        }

        //将数据组装,将本帧之前的包括本帧的数据全部组装
        public byte[] DataAssemble()
        {
            ByteBuffer bt = new ByteBuffer();
            Data data;
            while (m_ConcurSimulationDictionary.TryDequeue(out data))
            {
                byte[] a = data.m_Info.ToByteArray();
                bt.WriteBytes(a);
            }
            return bt.mGetbuffer();
        }
        //一帧只能模拟一次输入操作
        public void Simulate(int frame)
        {
            m_IsSimulate = false;
            EventUpdate();
            Data data;
            if (m_ConcurDictionary.TryPeek(out data))
            {
                if (data.m_Info.Frame <= frame)
                {
                    m_CurData.m_Info.Result.X += data.m_Info.Dir.X;
                    m_CurData.m_Info.Result.Y += data.m_Info.Dir.Y;
                    Debug.Log( m_CurData.m_Info.Result);
                    data.m_Info.Result = m_CurData.m_Info.Result;
                    data.m_Info.Isexecute = true;
                    m_CurData = data;
                    m_CubeServer.Simulatio(data.m_Info);
                    m_IsSimulate = true;
                    //删除本地收到的帧号，或者新建一个录像转入录像
                    m_ConcurDictionary.TryDequeue(out data);
                }
            }
        }

        public void Result(int frame)
        {
            //如果模拟过了就需要发送给客户端
            if (m_IsSimulate)
            {
                if (m_IsPhysics)
                {
                    m_CubeServer.Reset(ref m_CurData.m_Info);
                }
                Data data = new Data();
                data.m_Info = m_CurData.m_Info.Clone();
                Puchm_ConcurSimulationDictionary(data);
            }
        }

        public void Puchm_ConcurSimulationDictionary(Data data)
        {
            m_ConcurSimulationDictionary.Enqueue(data);
        }

        public Data Getm_ConcurSimulationDictionaryPeek()
        {
            Data data = null;
            if (m_ConcurDictionary.TryPeek(out data))
            {
                return data;
            }
            return data;
        }

        public void Destroy()
        {
            if (m_Cube != null)
            {
                GameObject.Destroy(m_Cube.gameObject);
            }
        }

        //主线程中调用
        public void CreateSimulant()
        {
            m_Cube = GameObject.Instantiate(m_Go).transform;
            m_CubeServer = m_Cube.gameObject.AddComponent<ServerCube>();
            m_CubeServer.ServerInit(m_ClientID, m_IsPhysics, this);
        }

        //主线程中调用
        private void InitPrafeb()
        {
            if (m_Go == null)
                m_Go = Resources.Load("Cube") as GameObject;
        }

        //主线程中调用
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
