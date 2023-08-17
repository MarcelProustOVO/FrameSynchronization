using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class Data
{

    bool m_IsPhysics;
    class LocalInfo
    {
        public bool m_IsSend;    //有没有发送过
        public Info m_Info;
    }
    /// <summary>
    /// 从服务器接收到的指令
    /// </summary>
    ConcurrentQueue<Info> m_ServerInfoDataQueue = new ConcurrentQueue<Info>();     
    
    /// <summary>
    /// //本地的指令需要上交给服务器
    /// </summary>
    Queue<LocalInfo> m_LocalInfoDataQueue = new Queue<LocalInfo>();      
    
    /// <summary>
    /// 本地在执行完一帧之后的快照数据
    /// </summary>
    private Queue<Info> m_LocalSimulation = new Queue<Info>();

    /// <summary>
    /// 本地模拟最后的一帧的快照
    /// </summary>
    public Info m_LastInfo;
    
    
    /// <summary>
    ///   当前操作序号
    /// </summary>
    public int m_OperationNumber
    {              
        get;
        private set;
    }

    public int m_ClientID
    {
        get;
        private set;
    }
    public Data(int id,bool isPhysics)
    {
        m_ClientID = id;
        m_IsPhysics = isPhysics;
        m_LastInfo = new Info();
        m_LastInfo.Result = new XYZW();
        m_LastInfo.Rotate = new XYZW();
        m_LastInfo.Velocity = new XYZW();
        m_LastInfo.Dir = new XYZW();
        m_LastInfo.AngularVelocity = new XYZW();
    }

    /// <summary>
    /// 操作之后的本地快照
    /// </summary>
    /// <param name="info"></param>
    public void LocalSnapshootPush(Info localInfo)
    {
        m_LocalSimulation.Enqueue(localInfo);
    }

    /// <summary>
    /// 和服务器快照进行比对。
    /// </summary>
    /// <param name="serverInfo"></param>
    /// <returns></returns>
    public bool LocalSnapshootComparison(Info serverInfo)
    {
        while (m_LocalSimulation.Count>0)
        {
            Info first = null;
            first = m_LocalSimulation.Dequeue();
            if (first.OperationNumber == serverInfo.OperationNumber)
            {
                if (m_IsPhysics)
                {
                    //如果操作帧的模拟结果和服务器不同，那将m_PhysicsLocalSimulation清空。因为本地模拟已经失效
                    if (!serverInfo.Rotate.Equals(first.Rotate) ||
                        !serverInfo.Result.Equals(first.Result) ||
                        !serverInfo.Velocity.Equals(first.Velocity) ||
                        !serverInfo.AngularVelocity.Equals(first.AngularVelocity))
                    {
                        Debug.Log(serverInfo.AngularVelocity + "  " + first.AngularVelocity);
                        Debug.Log(serverInfo.Velocity + "  " + first.Velocity);
                        Debug.Log(serverInfo.Rotate + "  " + first.Rotate);
                        Debug.Log(serverInfo.Result + "  " + first.Result);
                        Debug.Log("预测失效，清空本地快照，重新等待输入生成快照(物理引擎原因)");
                        //删除本地快照以及预操作
                        m_LocalSimulation.Clear();
                        m_LocalInfoDataQueue.Clear();
                        return false;
                    }
                }
                else
                {
                    if (!serverInfo.Result.Equals(first.Result))
                    {
                        Debug.Log(serverInfo.Result + "  " + first.Result);
                        Debug.Log("预测失效，清空本地快照");
                        //删除本地快照以及预操作
                        m_LocalSimulation.Clear();
                        m_LocalInfoDataQueue.Clear();
                        return false;
                    }
                }
            }else if (first.OperationNumber < serverInfo.OperationNumber)
            {
                continue;
            }
            else
            {
                Debug.Log("预测成功");
                return true;
            }
        }
        return false;
    }

    public void ServerPush(byte[] bytes)
    {
        Info info = Info.Parser.ParseFrom(bytes);
        if (info.Id == m_ClientID)
        {
            m_ServerInfoDataQueue.Enqueue(info);
        }
    }

    public Info ServerPop()
    {
        if (m_ServerInfoDataQueue.Count > 0)
        {
            Info inf;
            m_ServerInfoDataQueue.TryDequeue(out inf);
            m_OperationNumber = inf.OperationNumber;
            return inf;
        }
        return null;
    }

    public Info ServerPeek()
    {
        if (m_ServerInfoDataQueue.Count > 0)
        {
            Info inf;
            m_ServerInfoDataQueue.TryPeek(out inf);
            return inf;
        }
        return null;
    }

    public void LocalPush(Info info)
    {
        var localinfo = new LocalInfo();
        localinfo.m_Info = info;
        localinfo.m_IsSend = false;
        m_LocalInfoDataQueue.Enqueue(localinfo);
    }
    

    /// <summary>
    /// 将本地操作发送给服务器，但是队列不推出。
    /// </summary>
    /// <param name="byteBuffer"></param>
    /// <returns></returns>
    public bool LocalInfoDataQueueSendServer(ByteBuffer byteBuffer)
    {
        bool has = false;
        foreach (var item in m_LocalInfoDataQueue)
        {
            //只有没有发送过的序列号才需要发送
            if (!item.m_IsSend)
            {
                var bytes = item.m_Info.ToByteArray();
                item.m_IsSend = true;
                byteBuffer.WriteBytes(bytes);
                has = true;
            }
        }
        return has;
    }

    /// <summary>
    /// 将本地操作列表中的操作全部重新计算一遍
    /// </summary>
    /// <param name="action"></param>
    public void LocalInfoDataQueueOneFrameReform(Action<Info> action)
    {
        //Debug.Log("没有服务器确认过的指令数量："+m_LocalInfoDataQueue.Count);
        foreach (var item in m_LocalInfoDataQueue)
        {
            action(item.m_Info);
        }
    }

    /// <summary>
    /// 找到服务器发过来的操作序号和本地进行比对
    /// </summary>
    /// <param name="serverOperationNumber"></param>
    public void LocalInfoDataQueueOperationNumber(int serverOperationNumber)
    {

        while (m_LocalInfoDataQueue.Count > 0)
        {
            var tempInfo = m_LocalInfoDataQueue.Dequeue();
            //如果本地操作序号等于服务器或者大于服务器操作序号，则表明已经被服务器模拟过，推出本地操作
            if (serverOperationNumber >= tempInfo.m_Info.OperationNumber)
            {
                //Debug.Log("找到相等的推出之后现在是：" + m_LocalInfoDataQueue.Count);
                break;
            }
        }
    }
}
