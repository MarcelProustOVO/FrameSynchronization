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
        public bool m_IsSend;    //��û�з��͹�
        public Info m_Info;
    }
    /// <summary>
    /// �ӷ��������յ���ָ��
    /// </summary>
    ConcurrentQueue<Info> m_ServerInfoDataQueue = new ConcurrentQueue<Info>();     
    
    /// <summary>
    /// //���ص�ָ����Ҫ�Ͻ���������
    /// </summary>
    Queue<LocalInfo> m_LocalInfoDataQueue = new Queue<LocalInfo>();      
    
    /// <summary>
    /// ������ִ����һ֮֡��Ŀ�������
    /// </summary>
    private Queue<Info> m_LocalSimulation = new Queue<Info>();

    /// <summary>
    /// ����ģ������һ֡�Ŀ���
    /// </summary>
    public Info m_LastInfo;
    
    
    /// <summary>
    ///   ��ǰ�������
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
    /// ����֮��ı��ؿ���
    /// </summary>
    /// <param name="info"></param>
    public void LocalSnapshootPush(Info localInfo)
    {
        m_LocalSimulation.Enqueue(localInfo);
    }

    /// <summary>
    /// �ͷ��������ս��бȶԡ�
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
                    //�������֡��ģ�����ͷ�������ͬ���ǽ�m_PhysicsLocalSimulation��ա���Ϊ����ģ���Ѿ�ʧЧ
                    if (!serverInfo.Rotate.Equals(first.Rotate) ||
                        !serverInfo.Result.Equals(first.Result) ||
                        !serverInfo.Velocity.Equals(first.Velocity) ||
                        !serverInfo.AngularVelocity.Equals(first.AngularVelocity))
                    {
                        Debug.Log(serverInfo.AngularVelocity + "  " + first.AngularVelocity);
                        Debug.Log(serverInfo.Velocity + "  " + first.Velocity);
                        Debug.Log(serverInfo.Rotate + "  " + first.Rotate);
                        Debug.Log(serverInfo.Result + "  " + first.Result);
                        Debug.Log("Ԥ��ʧЧ����ձ��ؿ��գ����µȴ��������ɿ���(��������ԭ��)");
                        //ɾ�����ؿ����Լ�Ԥ����
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
                        Debug.Log("Ԥ��ʧЧ����ձ��ؿ���");
                        //ɾ�����ؿ����Լ�Ԥ����
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
                Debug.Log("Ԥ��ɹ�");
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
    /// �����ز������͸������������Ƕ��в��Ƴ���
    /// </summary>
    /// <param name="byteBuffer"></param>
    /// <returns></returns>
    public bool LocalInfoDataQueueSendServer(ByteBuffer byteBuffer)
    {
        bool has = false;
        foreach (var item in m_LocalInfoDataQueue)
        {
            //ֻ��û�з��͹������кŲ���Ҫ����
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
    /// �����ز����б��еĲ���ȫ�����¼���һ��
    /// </summary>
    /// <param name="action"></param>
    public void LocalInfoDataQueueOneFrameReform(Action<Info> action)
    {
        //Debug.Log("û�з�����ȷ�Ϲ���ָ��������"+m_LocalInfoDataQueue.Count);
        foreach (var item in m_LocalInfoDataQueue)
        {
            action(item.m_Info);
        }
    }

    /// <summary>
    /// �ҵ��������������Ĳ�����źͱ��ؽ��бȶ�
    /// </summary>
    /// <param name="serverOperationNumber"></param>
    public void LocalInfoDataQueueOperationNumber(int serverOperationNumber)
    {

        while (m_LocalInfoDataQueue.Count > 0)
        {
            var tempInfo = m_LocalInfoDataQueue.Dequeue();
            //������ز�����ŵ��ڷ��������ߴ��ڷ�����������ţ�������Ѿ���������ģ������Ƴ����ز���
            if (serverOperationNumber >= tempInfo.m_Info.OperationNumber)
            {
                //Debug.Log("�ҵ���ȵ��Ƴ�֮�������ǣ�" + m_LocalInfoDataQueue.Count);
                break;
            }
        }
    }
}
