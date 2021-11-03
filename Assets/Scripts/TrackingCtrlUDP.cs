using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
#if UNITY_EDITOR
using System.Net;
using System.Net.Sockets;
using System.Threading;
#else
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif



public class TrackingCtrlUDP : TrackingCtrl
{
	//[Tooltip("Port to open on HoloLens to send or listen")]
	//public string HoloPort = "5001";


	[Tooltip("Port to send to")]
    public string OutPort = "5000";

	[Tooltip("Port to receive from")]
	public string InPort = "5001";

	//[Tooltip("Functions to invoke on packet reception")]
	//public UDPMessageEvent udpEvent = null;

	
	string recvData;
	byte[] msgData;

#if UNITY_EDITOR
	EndPoint serverEnd; //服务端
	IPEndPoint ipEnd; //服务端端口
	Socket socket; //目标socket
	Thread connectThread; //连接线程
	int recvLen; //接收的数据长度
	byte[] sendData = new byte[1024]; //发送的数据，必须为字节
#else
    private readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();
	DatagramSocket socket;
#endif
	void Start()
	{

	}
	/// <summary>
	/// everything start from here
	/// </summary>
	public override void  f_Init()
    {
		ConnectUDP(ServerIP,InPort,OutPort);
		Debug.Log("UDP");
	}

#if UNITY_EDITOR

	private void ConnectUDP(string IP, string inPort, string outPort)
	{
		//定义连接的服务器ip和端口，可以是本机ip，局域网，互联网
		ipEnd = new IPEndPoint(IPAddress.Parse(IP), int.Parse(outPort));
		//定义套接字类型,在主线程中定义
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		//定义服务端
		IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
		serverEnd = (EndPoint)sender;
		print("waiting for sending UDP dgram");

		//建立初始连接，这句非常重要，第一次连接初始化了serverEnd后面才能收到消息
		SendUDPMessage(ServerIP, outPort, "Client Ready!");

		//开启一个线程连接，必须的，否则主线程卡死
		connectThread = new Thread(new ThreadStart(Socket_MessageReceived));
		connectThread.Start();
	}
#else
	private async void ConnectUDP(string IP, string inPort, string outPort)
	{
	    socket = new DatagramSocket();
		socket.MessageReceived += Socket_MessageReceived;
		try
		{
			Windows.Networking.HostName serverHost = new Windows.Networking.HostName(ServerIP);
			await socket.BindServiceNameAsync(InPort);
			SendUDPMessage(ServerIP, OutPort, "Client Ready!");
		}
		catch (Exception e)
		{
			textBar.text = e.Message.ToString();
			return;
		}

		//if (ServerIP != null && OutPort != null && sendPingAtStart)
		//{
		//	if (PingMessage == null) {
		//		PingMessage = "";
		//	}
		//	SendUDPMessage(ServerIP, OutPort, Encoding.Unicode.GetBytes("Client Ready!"));
		//}
	}
#endif



#if UNITY_EDITOR
    public void SendUDPMessage(string HostIP, string HostPort, string msg)
	{
		//清空发送缓存
		sendData = new byte[1024];
		//数据类型转换
		sendData = Encoding.Unicode.GetBytes(msg);
		//发送给指定服务端
		socket.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
	}

#else
	public async void SendUDPMessage(string HostIP, string HostPort, string msg)
	{
		byte[] data = Encoding.Unicode.GetBytes(msg);
		await _SendUDPMessage(HostIP, HostPort, data);
	}
	private async System.Threading.Tasks.Task _SendUDPMessage(string externalIP, string externalPort, byte[] data)
	{
		using (var stream = await socket.GetOutputStreamAsync(new Windows.Networking.HostName(externalIP), externalPort))
		{
			using (var writer = new Windows.Storage.Streams.DataWriter(stream))
			{
				writer.WriteBytes(data);
				await writer.StoreAsync();

			}
		}
	}
#endif


    static MemoryStream ToMemoryStream(Stream input)
	{
		try
		{                                         // Read and write in
			byte[] block = new byte[0x1000];       // blocks of 4K.
			MemoryStream ms = new MemoryStream();
			while (true)
			{
				int bytesRead = input.Read(block, 0, block.Length);
				if (bytesRead == 0) return ms;
				ms.Write(block, 0, bytesRead);
			}
		}
		finally { }
	}

	// Update is called once per frame
	void Update()
	{
#if !UNITY_EDITOR
		while (ExecuteOnMainThread.Count > 0)
		{
			ExecuteOnMainThread.Dequeue().Invoke();
		}
#endif
		textBar.text = recvData;
	}

#if !UNITY_EDITOR
	private void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
	Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
	{
		//Read the message that was received from the UDP  client.
		Stream streamIn = args.GetDataStream().AsStreamForRead();
		MemoryStream ms = ToMemoryStream(streamIn);
		msgData = ms.ToArray();
		recvData = Encoding.Unicode.GetString(msgData);

		//if (ExecuteOnMainThread.Count == 0)
		//{
		//ExecuteOnMainThread.Enqueue(() =>
		//{
		//Debug.Log("ENQEUED ");
		//if (udpEvent != null)
		//udpEvent.Invoke(args.RemoteAddress.DisplayName, HoloPort, msgData);
		//});
		//}
		//}


	}
#else
	private void Socket_MessageReceived()
	{
		//进入接收循环
		while (true)
		{
			//对data清零
			msgData = new byte[1024];
			//获取客户端，获取服务端端数据，用引用给服务端赋值，实际上服务端已经定义好并不需要赋值
			recvLen = socket.ReceiveFrom(msgData, ref serverEnd);
			//print("message from: " + serverEnd.ToString()); //打印服务端信息
															//输出接收到的数据
			recvData = Encoding.Unicode.GetString(msgData, 0, recvLen);
		}
	}
#endif
}
