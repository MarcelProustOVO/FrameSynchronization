using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour, CubeParent
{
    // Start is called before the first frame update

    Data m_Data;
    bool m_IsPhysics;
    Rigidbody m_Rigidbody;
    Info m_InputInfo;
    
    //是否接受快照，快照存储区间只在操作之后 到模拟结束之间，
    private bool AcceptTheSnapshot = false;


    public Data Init(int id, bool isPhysics)
    {
        m_IsPhysics = isPhysics;
        Debug.Log("cubeID：" + id);
        transform.position = Vector3.zero;
        m_Data = new Data(id,isPhysics);
        LoaclData.Instance.Puch(m_Data);
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.isKinematic = !m_IsPhysics;
        m_InputInfo = new Info();
        m_InputInfo.Dir = new XYZW();
        m_InputInfo.Id = id;
        return m_Data;
    }

    // Update is called once per frame
    bool UP;
    bool DOWN;
    bool Rigt;
    bool LEFT;

    public void InputMessage()
    {
        if (Input.GetKey(KeyCode.W))
        {
            UP = true;
        }
        else
        {
            UP = false;
        }


        if (Input.GetKey(KeyCode.S))
        {
            DOWN = true;
        }
        else
        {
            DOWN = false;
        }


        if (Input.GetKey(KeyCode.A))
        {
            LEFT = true;
        }
        else
        {
            LEFT = false;
        }


        if (Input.GetKey(KeyCode.D))
        {
            Rigt = true;
        }
        else
        {
            Rigt = false;
        }

        Vector2 collect = Vector2.zero;

        if (UP)
        {
            collect.y = 1;
        }

        if (DOWN)
        {
            collect.y = -1;
        }

        if (Rigt)
        {
            collect.x = 1;
        }

        if (LEFT)
        {
            collect.x = -1;
        }
        
        if (collect != Vector2.zero)
        {
            AcceptTheSnapshot = true;
            //切换本地
            m_InputInfo.OperationNumber++;
            int power = 0;
            if (!m_IsPhysics)
                power = 1;
            else
                power = 30;
            m_InputInfo.Dir.X = (int) collect.x * power;
            m_InputInfo.Dir.Y = (int) collect.y * power;
            m_InputInfo.Frame = LoaclData.Instance.LocalSimulateFrame;
            LocalSimulation(ref m_InputInfo);
        }
    }

    /// <summary>
    /// 本地操作之后，讲操作的快照保存起来。
    /// </summary>
    /// <param name="info"></param>
    private void LocalSimulation(ref Info info)
    {
        if (info == null)
            return;
        if (!m_IsPhysics)
        {
            transform.position += new Vector3(info.Dir.X * 0.1f, 0, info.Dir.Y * 0.1f);
            m_Data.m_LastInfo.Result.X += info.Dir.X;
            m_Data.m_LastInfo.Result.Y += info.Dir.Y;
            m_Data.m_LastInfo.Frame = LoaclData.Instance.LocalSimulateFrame;
            m_Data.m_LastInfo.OperationNumber = m_InputInfo.OperationNumber;
            m_Data.LocalSnapshootPush(m_Data.m_LastInfo.Clone());
        }
        else
        {
            m_Rigidbody.AddForce(new Vector3(info.Dir.X, 0, info.Dir.Y), ForceMode.Force);
        }
        m_Data.LocalPush(m_InputInfo.Clone());
    }
    
    /// <summary>
    /// 在执行操作之后的物理模拟结果，以及在本地的物理最终结果。
    /// </summary>
    public void Result()
    {
        if (!m_IsPhysics)
        {
            return;
        }

        m_Rigidbody.velocity = LoaclData.VectorPrecision(m_Rigidbody.velocity);
        m_Rigidbody.angularVelocity = LoaclData.VectorPrecision(m_Rigidbody.angularVelocity);
        m_Rigidbody.position = LoaclData.VectorPrecision(m_Rigidbody.position);
        m_Rigidbody.rotation = LoaclData.QuaternionPrecision(m_Rigidbody.rotation);
        //
        Info a = new Info();
        a.Velocity = LoaclData.Vector2XYZW(m_Rigidbody.velocity);
        a.AngularVelocity = LoaclData.Vector2XYZW(m_Rigidbody.angularVelocity);
        a.Result = LoaclData.Vector2XYZW(m_Rigidbody.position);
        a.Rotate = LoaclData.Vector2XYZW(new Vector4(m_Rigidbody.rotation.x, m_Rigidbody.rotation.y,
            m_Rigidbody.rotation.z, m_Rigidbody.rotation.w));
        a.Frame = LoaclData.Instance.LocalSimulateFrame;
        a.OperationNumber = m_InputInfo.OperationNumber;
        //记录下本地模拟最后一帧的物理状态 用于之后的重置
        m_Data.m_LastInfo = a;
        //记录下操作之后的那一帧快照，用于和服务器进行比对
        if (AcceptTheSnapshot)
        {
            Debug.Log("推入！！！！");
            m_Data.LocalSnapshootPush(a);
            AcceptTheSnapshot = false;
        }
    }


    /// <summary>
    /// 检查本地是否预测成功
    /// </summary>
    /// <param name="info"></param>
    /// <returns>预测成功就返回 true</returns>
    private bool ServerExecute(Info info)
    {
        if (info == null)
            return false;

        if (!m_IsPhysics)
        {

            //只有预测失败才需要本地重新设置
            if (!m_Data.LocalSnapshootComparison(info))
            {
                transform.position = new Vector3(info.Result.X * 0.1f, 0, info.Result.Y * 0.1f);
                return false;
            }
            return true;
        }
        else
        {
            //只有预测失败才需要本地重新设置
            if (!m_Data.LocalSnapshootComparison(info))
            {
                m_Rigidbody.position = LoaclData.XYZW2Vector(info.Result);
                Vector4 tempV = LoaclData.XYZW2Vector(info.Rotate);
                m_Rigidbody.rotation = new Quaternion(tempV.x, tempV.y, tempV.z, tempV.w);
                m_Rigidbody.velocity = LoaclData.XYZW2Vector(info.Velocity);
                m_Rigidbody.angularVelocity = LoaclData.XYZW2Vector(info.AngularVelocity);
                return false;
            }
            return true;
        }
    }

    //重置操作
    private void ServerSimulation(Info info = null)
    {
        if (!m_IsPhysics)
        {
            if (info == null)
                return;
            transform.position += new Vector3(info.Dir.X * 0.1f, 0, info.Dir.Y * 0.1f);
        }
    }

    private void ServerSimulationPly(Info info = null)
    {
        if (m_IsPhysics)
        {
            Debug.Log("引用本地最新状态");
            m_Rigidbody.velocity = LoaclData.XYZW2Vector(info.Velocity);
            m_Rigidbody.angularVelocity = LoaclData.XYZW2Vector(info.AngularVelocity);
            m_Rigidbody.position = LoaclData.XYZW2Vector(info.Result);
            Vector4 tempV = LoaclData.XYZW2Vector(info.Rotate);
            m_Rigidbody.rotation = new Quaternion(tempV.x, tempV.y, tempV.z, tempV.w);
        }
        else
        {
            transform.position = new Vector3(info.Result.X * 0.1f, 0, info.Result.Y * 0.1f);
        }
    }


    public void ExecuteServer()
    {
        if (m_Data == null)
            return;
        Info info = m_Data.ServerPeek();
        if (info == null)
            return;

        // 本地帧检测，如果和服务器帧数相聚过大，重新设置。
        // LoaclData.Instance.Examine(info.Frame);

        bool isExecute = false;
        //如果本地预测一致
        bool isFit = false;

        while (info != null && LoaclData.Instance.LocalSimulateFrame >= info.Frame)
        {
            // Debug.Log("确认序号:" + info.OperationNumber + "   " + info.Dir + "   " + info.Result);
            isFit = ServerExecute(info);
            //Debug.Log(transform.position + " " + info.Result);

            m_Data.LocalInfoDataQueueOperationNumber(info.OperationNumber);

            m_Data.ServerPop();

            info = m_Data.ServerPeek();

            isExecute = true;
        }
        //
        // if (isExecute && isFit)
        // {
        //     //在得到服务器最新一帧的状态下重新对本地操作在一帧之内进行模拟。快照赋予
        //     ServerSimulationPly(m_Data.m_LastInfo);
        // }
    }
}