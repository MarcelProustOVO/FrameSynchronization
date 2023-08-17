using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class LoaclData : Singleton<LoaclData>
{
    public ConcurrentQueue<Data> m_InfoDataQueue = new ConcurrentQueue<Data>();
    public int LocalSimulateFrame
    {
        get;
        private set;
    }

    private int DelayTime;

    public void SetServerSimulateFrame(int frame, int delayTime)
    {
        DelayTime = delayTime;
        LocalSimulateFrame = frame + delayTime;
    }

    public int AddFrame()
    {
        // Debug.Log(LocalSimulateFrame+1);
        return ++LocalSimulateFrame;
    }

    //本地的帧率是否和服务器相差过大
    public void Examine(int serverFrame)
    {
        if (Mathf.Abs(LocalSimulateFrame - serverFrame) > DelayTime * 1.5F)
        {
            Debug.Log("帧率差距过大，重新设置");
            LocalSimulateFrame = serverFrame+5;
        }
    }

    //玩家推入
    public void Puch(Data data)
    {
        m_InfoDataQueue.Enqueue(data);
    }


    public void PushServerdata(byte[] bytes)
    {
        foreach (var item in m_InfoDataQueue)
        {
            item.ServerPush(bytes);
        }
    }
    static int PrecisionInt32 = 1000;
    static float PrecisionFloat = 1000.0f;
    public static XYZW Vector2XYZW(Vector4 vector)
    {
        int x = (int)(vector.x * PrecisionInt32);
        int y = (int)(vector.y * PrecisionInt32);
        int z = (int)(vector.z * PrecisionInt32);
        int w = (int)(vector.w * PrecisionInt32);
        XYZW xyzw = new XYZW();
        xyzw.X = x;
        xyzw.Y = y;
        xyzw.Z = z;
        xyzw.W = w;
        return xyzw;
    }

    public static Vector4 XYZW2Vector(XYZW xyzw)
    {
        float x = (float)(xyzw.X / PrecisionFloat);
        float y = (float)(xyzw.Y / PrecisionFloat);
        float z = (float)(xyzw.Z / PrecisionFloat);
        float w = (float)(xyzw.W / PrecisionFloat);

        return new Vector4(x, y, z, w);
    }

    public static Vector4 VectorPrecision(Vector4 vector4)
    {
        Vector4 v = new Vector4();
        v.x = ((int)(vector4.x * PrecisionInt32)) / PrecisionFloat;
        v.y = ((int)(vector4.y * PrecisionInt32)) / PrecisionFloat;
        v.z = ((int)(vector4.z * PrecisionInt32)) / PrecisionFloat;
        v.w = ((int)(vector4.w * PrecisionInt32)) / PrecisionFloat;
        return v;
    }

    public static Quaternion QuaternionPrecision(Quaternion quaternion) {
        Quaternion q = new Quaternion();
        q.x = ((int)(quaternion.x * PrecisionInt32)) / PrecisionFloat;
        q.y = ((int)(quaternion.y * PrecisionInt32)) / PrecisionFloat;
        q.z = ((int)(quaternion.z * PrecisionInt32)) / PrecisionFloat;
        q.w = ((int)(quaternion.w * PrecisionInt32)) / PrecisionFloat;
        return q;
    }
}
