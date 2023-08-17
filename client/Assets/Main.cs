using GameServer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    public Button m_ButtonServerLogin;
    public Button m_ButtonClinetLogin;
    public Button m_LoginButton;
    public Text m_Text;
    public Toggle m_Toggle;
    //[Header("�����Ϊ�ͻ����Ƿ���Ϊ��������")]
    bool m_IsPhysics;
    void Start()
    {
        m_ButtonServerLogin.onClick.AddListener(() =>
        {
            Server ser = Server.Instance;
            ser.Init("127.0.0.1", 8899);
            m_ButtonServerLogin.gameObject.SetActive(false);
            m_ButtonClinetLogin.gameObject.SetActive(false);
            m_Text.text = "������";
        });

        m_ButtonClinetLogin.onClick.AddListener(() =>
        {
            m_IsPhysics = m_Toggle.isOn;
            Client clinet = Client.Instance;
            clinet.Init(m_IsPhysics);
            m_LoginButton.onClick.AddListener(()=> {
                clinet.LoginRequest();
                m_LoginButton.gameObject.SetActive(false);
            });
            m_ButtonServerLogin.gameObject.SetActive(false);
            m_ButtonClinetLogin.gameObject.SetActive(false);
            m_LoginButton.gameObject.SetActive(true);
            m_Text.text = "�ͻ���"+ (m_IsPhysics?"������":"������");
        });
    }

    // Update is called once per frame
    void Update()
    {
    }
}
