using Google.Protobuf;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Client : SingletonMono<Client>
{
    public struct PlayerMessage
    {
        public int Id;
        public Info Info;
        public int Frame;
        public bool IsMe;
        public bool IsPhysics;
    }
    int m_PlayerID = -1;
    SocketClient m_SocketClient;
    public ConcurrentQueue<PlayerMessage> m_PlayerQueue = new ConcurrentQueue<PlayerMessage>();

    bool LoginOver = false;

    List<CubeParent> m_CubeList = new List<CubeParent>();
    bool m_IsPhysics;
    public void Init(bool isPhysics)
    {
        m_IsPhysics = isPhysics;
        m_SocketClient = new SocketClient(this);
        Physics.autoSimulation = false;
    }

    public void LoginRequest()
    {
        m_SocketClient.Login(m_IsPhysics);
    }

    public void LoginResponse(int id, Info info, int frame, bool isPhysics)
    {
        if (id == m_PlayerID)
            return;
        bool me = false;
        if (m_PlayerID != -1)
            me = false;
        else
        {
            Debug.Log("同步帧号:" + frame);
            LoaclData.Instance.SetServerSimulateFrame(frame, 5); //比服務器快5幀
            me = true;
            m_PlayerID = id;
            LoginOver = true;
        }
        PlayerMessage pm = new PlayerMessage() { Id = id, Info = info, IsMe = me, Frame = frame, IsPhysics = isPhysics };
        m_PlayerQueue.Enqueue(pm);
    }


    private void OnDestroy()
    {
        if (m_SocketClient != null)
            m_SocketClient.Close();
    }
    GameObject m_PrafabCube;
    public void Update()
    {
        if (m_PlayerQueue.Count > 0)
        {
            PlayerMessage pm;
            if (m_PlayerQueue.TryDequeue(out pm))
            {
                if (m_PrafabCube == null)
                    m_PrafabCube = Resources.Load<GameObject>("Cube");
                GameObject go = GameObject.Instantiate(m_PrafabCube);
                if (pm.IsMe)
                {
                    var cube = go.AddComponent<Cube>();
                    go.transform.position = new Vector3(0, 0, 0);
                    m_PlayerData = cube.Init(pm.Id, pm.IsPhysics);
                    m_PlayerID = pm.Id;
                    m_CubeList.Add(cube);
                }
                else
                {
                    var cube = go.AddComponent<CubeOther>();
                    cube.Init(pm.Id, pm.Info, pm.IsPhysics);
                    Debug.Log("位置目标：" + pm.Info.Result + "pm.Info"+":"+ pm.IsPhysics);
                    m_CubeList.Add(cube);
                }
            }
        }
    }
    Data m_PlayerData;
    ByteBuffer localOperation = new ByteBuffer();   //本地操作的集合
    Type moveType;
    //已经设置成1秒50次。
    public void FixedUpdate()
    {
        if (LoginOver)
        {
            LoaclData.Instance.AddFrame();
            //Debug.Log(ServerData.Instance.ServerSimulateFrame);
            if (m_PlayerData == null)
                return;
            if (moveType == null)
            {
                moveType = new Type();
                moveType.Type_ = OperationType.Move;
            }
            localOperation.Destory();
            if (m_PlayerData.LocalInfoDataQueueSendServer(localOperation))
            {
                var typeBytes = moveType.ToByteArray();
                localOperation.WriteBytes(typeBytes, false);
                m_SocketClient.Send(localOperation.mGetbuffer());
            }
        }

        for (int i = 0; i < m_CubeList.Count; i++)
        {
            m_CubeList[i].InputMessage();
        }
        Physics.Simulate(Time.fixedDeltaTime);
        for (int i = 0; i < m_CubeList.Count; i++)
        {
            m_CubeList[i].Result();
            m_CubeList[i].ExecuteServer();
        }
    }
}
