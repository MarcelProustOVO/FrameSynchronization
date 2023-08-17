using GameServer;
using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeOther : MonoBehaviour, CubeParent
{
    Data m_Data;
    bool m_IsPhysics;
    private Rigidbody m_Rigidbody;
    public void Init(int id, Info info, bool isPhysics)
    {
        m_IsPhysics = isPhysics;
        Debug.Log("cubeID：" + id);
        m_Data = new Data(id,isPhysics);
        LoaclData.Instance.Puch(m_Data);
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.isKinematic = !m_IsPhysics;
        if (isPhysics)
        {
            ServerSimulationForRigidbody(info);
        }
        else
        {
            ServerServerSimulation(info);
        }
    }

    private void ServerServerSimulation(Info info)
    {
        if (info == null)
            return;
        if (info.Result != null)
            transform.position = new Vector3(info.Result.X * 0.1f, 0, info.Result.Y * 0.1f);
    }

    //服务器下发进行的物理模拟
    private void ServerSimulationForRigidbody(Info info)
    {
        if (info == null)
            return;

        if (info.Result != null)
        {
            m_Rigidbody.position = LoaclData.XYZW2Vector(info.Result);
        }

        if (info.Rotate != null)
        {
            Vector4 tempV = LoaclData.XYZW2Vector(info.Rotate);
            m_Rigidbody.rotation = new Quaternion(tempV.x, tempV.y, tempV.z, tempV.w);
        }

        if (info.Velocity != null)
            m_Rigidbody.velocity = LoaclData.XYZW2Vector(info.Velocity);

        if (info.AngularVelocity != null)
            m_Rigidbody.angularVelocity = LoaclData.XYZW2Vector(info.AngularVelocity);
    }

    //本地执行服务器的模拟
    public void ExecuteServer()
    {
        if (m_Data == null)
            return;
        Info info = m_Data.ServerPeek();
        
        while (info != null && LoaclData.Instance.LocalSimulateFrame >= info.Frame)
        {
            if (!m_IsPhysics)
                ServerServerSimulation(info);
            else
                ServerSimulationForRigidbody(info);
            m_Data.ServerPop();
            info = m_Data.ServerPeek();
        }
    }

    public void InputMessage()
    {
        //throw new System.NotImplementedException();
    }

    public void Result()
    {
        //throw new System.NotImplementedException();
    }
}
