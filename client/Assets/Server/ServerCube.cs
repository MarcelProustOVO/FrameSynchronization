using GameServer;
using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameServer
{
    public class ServerCube : MonoBehaviour
    {
        bool m_IsPhysics;
        private Rigidbody m_Rigidbody;
        ClientData m_ClientData;
        public void ServerInit(int id, bool isPhysics, ClientData data)
        {
            Debug.Log("cubeID：" + id);
            m_IsPhysics = isPhysics;
            m_ClientData = data;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.isKinematic = !m_IsPhysics;
        }

        public void Simulatio(Info info)
        {
            if (info == null)
                return;
            if (!m_IsPhysics)
            {
                ServerSimulation(info);
            }
            else
            {
                ServerSimulationForRigidbody(info);
            }
        }

        //服务器本身进行物理模拟
        public void ServerSimulationForRigidbody(Info info)
        {
            if (info.Dir != null)
                m_Rigidbody.AddForce(new Vector3(info.Dir.X, 0, info.Dir.Y), ForceMode.Force);
        }

        public void Reset(ref Info info)
        {
            info.Velocity = LoaclData.Vector2XYZW(m_Rigidbody.velocity);

            info.AngularVelocity = LoaclData.Vector2XYZW(m_Rigidbody.angularVelocity);

            info.Result = LoaclData.Vector2XYZW(m_Rigidbody.position);

            info.Rotate = LoaclData.Vector2XYZW(new Vector4(m_Rigidbody.rotation.x, m_Rigidbody.rotation.y, m_Rigidbody.rotation.z, m_Rigidbody.rotation.w));

            m_Rigidbody.velocity = LoaclData.VectorPrecision(m_Rigidbody.velocity);
            m_Rigidbody.angularVelocity = LoaclData.VectorPrecision(m_Rigidbody.angularVelocity);
            m_Rigidbody.position = LoaclData.VectorPrecision(m_Rigidbody.position);
            m_Rigidbody.rotation = LoaclData.QuaternionPrecision(m_Rigidbody.rotation);
        }

        //服务器进行的普通模拟
        private void ServerSimulation(Info info)
        {
            if (info == null)
                return;
            transform.position += new Vector3(info.Dir.X * 0.1f, 0, info.Dir.Y * 0.1f);
        }



        public void OnCollisionEnter(Collision collision)
        {
            //if (m_IsPhysics)
            //{
            //    GameServer.ClientData.Data data = new ClientData.Data();
            //    Info info = new Info();

            //    info.Velocity = ServerData.Vector2XYZW(m_Rigidbody.velocity);

            //    info.AngularVelocity = ServerData.Vector2XYZW(m_Rigidbody.angularVelocity);

            //    info.Result = ServerData.Vector2XYZW(transform.position);

            //    info.Rotate = ServerData.Vector2XYZW(new Vector4(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w));
            //    data.m_Info = info;
            //    data.m_FrameID = m_ClientData.m_Room.m_FrameId;
            //    m_ClientData.Puchm_ConcurSimulationDictionary(data);
            //}
        }
    }
}
