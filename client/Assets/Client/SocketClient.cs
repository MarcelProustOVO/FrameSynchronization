using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SocketClient
{
    private const string m_IP = "127.0.0.1";
    private const int m_PORT = 8899;
    ByteBuffer m_ByteBuffer;
    ByteBuffer m_SocketBuffer;
    private Socket m_ClientSocket;
    Client m_Main;
    object locks = new object();
    private bool m_IsTaskRun;

    public SocketClient(Client mn)
    {
        byte[] bt = new byte[2048];
        m_ByteBuffer = new ByteBuffer(bt);
        byte[] socketBt = new byte[1024];
        m_SocketBuffer = new ByteBuffer(socketBt);
        m_Main = mn;
        OnInit();
    }

    public void OnInit()
    {
        m_ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            m_ClientSocket.Connect(m_IP, m_PORT);
            Start();
            Debug.Log("连接成功");
            m_IsTaskRun = true;
            Task a = new Task(() =>
            {
                while (m_IsTaskRun)
                {
                    lock (locks)
                    {
                        Analysis();
                    }
                    Thread.Sleep(10);
                }
            });
            a.Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private void Start()
    {

        m_ClientSocket.BeginReceive(m_SocketBuffer.mBytes, m_SocketBuffer.mEffectiveHead, m_SocketBuffer.m_RemainSize, SocketFlags.None, ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            if (m_ClientSocket == null || m_ClientSocket.Connected == false) return;
            int count = m_ClientSocket.EndReceive(ar);
            if (count == 0)
            {
                Close();
                return;
            }

            m_SocketBuffer.SetEffectiveHead(count);
            lock (locks)
            {
                m_ByteBuffer.WirteBuffer(m_SocketBuffer);
            }
            m_SocketBuffer.Clear();
            Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Close();
        }
    }

    private void Analysis()
    {
        try
        {
            int lenght = m_ByteBuffer.ReadInt32();
            int cur = 0;
            if (lenght == -1 || lenght == 0)
                return;
            if ((lenght) <= m_ByteBuffer.mEffectiveHead)
            {
                int subLenght = m_ByteBuffer.ReadInt32();
                byte[] data = m_ByteBuffer.ReadByte(subLenght);
                Type type = Type.Parser.ParseFrom(data);
                cur += 4;
                cur += subLenght;
                if (type.Type_ == OperationType.Move)
                {
                    int count = 0;
                    while (cur < lenght)
                    {
                        subLenght = m_ByteBuffer.ReadInt32();
                        data = m_ByteBuffer.ReadByte(subLenght);
                        cur += 4;
                        cur += subLenght;
                        LoaclData.Instance.PushServerdata(data);
                        count++;
                    }
                    //Debug.Log("此条数据长度" + lenght + " 读取到：" + count);
                }
                else if (type.Type_ == OperationType.Login)
                {
                    Debug.Log("收到登录回调");

                    subLenght = m_ByteBuffer.ReadInt32();
                    data = m_ByteBuffer.ReadByte(subLenght);
                    LoginResponse login = LoginResponse.Parser.ParseFrom(data);
                    Debug.Log("当前服务器人数：" + login.Data.Count);
                    for (int i = 0; i < login.Data.Count; i++)
                    {
                        m_Main.LoginResponse(login.Data[i].Id, login.Data[i].Info,login.Data[i].Frame, login.Data[i].IsPhysics);
                    }

                    m_ByteBuffer.Clear();
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void Login(bool isPhysics)
    {
        ByteBuffer bf = new ByteBuffer();
        LoginRequest login = new LoginRequest();
        login.Name = "cube";
        login.IsPhysics = isPhysics;
        byte[] bt = login.ToByteArray();

        Type t = new Type();
        t.Type_ = OperationType.Login;
        byte[] b = t.ToByteArray();

        bf.WriteBytes(b);
        bf.WriteBytes(bt);
        Send(bf.mGetbuffer());
    }

    public void Send(byte[] bytes)
    {
        ByteBuffer bb = new ByteBuffer();
        bb.WriteBytes(bytes);
        var a = bb.mGetbuffer();
        try
        {
            m_ClientSocket.Send(a);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void Close()
    {
        try
        {
            if (m_ClientSocket != null)
                m_ClientSocket.Close();
            m_IsTaskRun = false;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
