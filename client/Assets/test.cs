using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Physics.autoSimulation = false;
    }
    public float m_fs;
    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.W))
        //{
        //    GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, m_fs), ForceMode.Force);
        //    //Debug.Log(GetComponent<Rigidbody>().angularVelocity);
        //    //Physics.Simulate(Time.deltaTime);
        //    //Debug.Log(GetComponent<Rigidbody>().angularVelocity);
        //}

        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, -m_fs), ForceMode.Force);
        //    //Physics.Simulate(Time.deltaTime);
        //}
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    GetComponent<Rigidbody>().AddForce(new Vector3(-m_fs, 0, 0), ForceMode.Force);
        //    //Physics.Simulate(Time.deltaTime);
        //}
        //if (Input.GetKeyDown(KeyCode.D))
        //{
        //    GetComponent<Rigidbody>().AddForce(new Vector3(m_fs, 0, 0), ForceMode.Force);
        //    //Physics.Simulate(Time.deltaTime);
        //    //Physics.Simulate(Time.deltaTime);
        //}
    }
}
