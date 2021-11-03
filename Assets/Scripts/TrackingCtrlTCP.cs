using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;
#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage.Streams;
#endif

public class TrackingCtrlTCP : TrackingCtrl
{
    private Byte[] bytes = new Byte[256];
    private StreamWriter sw;
    private StreamReader sr;
    public string port = "5000";
    private string recvData = string.Empty;
    private string sentData = string.Empty;
  

    private bool exchanging = false;
    private bool exchangeStopRequested = false;
    private string lastPacket = null;

    int i = 0;
    Stream streamOut;
    Stream streamIn;


#if UNITY_EDITOR
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

#if !UNITY_EDITOR
    private DataReader dr;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

    /// <summary>
    /// everything start from here
    /// </summary>
    public override void f_Init()
    {
        ConnectTCP(ServerIP, port);
        Debug.Log("TCP");
    }

    public void Start()
    {
        //Server ip address and port

        //ConnectTCP(IP, port);

    }


  

#if UNITY_EDITOR
    private void ConnectTCP(string IP, string port)
    {
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(IP, Int32.Parse(port));
            stream = client.GetStream();
            sr = new StreamReader(stream);
            sw = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
        }
        catch (Exception e)
        {
        }
    }
#else
 private async void ConnectTCP(string IP, string port)
 {
 try
            {
                if (exchangeTask != null) StopExchange();

                socket = new Windows.Networking.Sockets.StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                await socket.ConnectAsync(serverHost, port);

                streamOut = socket.OutputStream.AsStreamForWrite();
                sw = new StreamWriter(streamOut) { AutoFlush = true };

                streamIn = socket.InputStream.AsStreamForRead();
                sr = new StreamReader(streamIn);
                dr = new DataReader(socket.InputStream);
                RestartExchange();
                //successStatus = "Connected!";
            }
            catch (Exception e)
            {
                //errorStatus = e.ToString();
            }
 }
#endif

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }


    public void Update()
    {
        if (lastPacket != null)
        {
            ReportDataToTrackingManager(lastPacket);
        }

    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (sw == null || sr == null) continue;
            exchanging = true;

#if UNITY_EDITOR
            sw.WriteLine(i.ToString());
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes, 0, client.SendBufferSize);
                recvData = Encoding.Unicode.GetString(bytes, 0, recv);
                Debug.Log("Read data: " + recvData);
                //if (recvData.EndsWith("\n")) break;

                //recvData = sr.ReadLine();


            }
#else
                //await sw.WriteLineAsync(i.ToString());
               // await sw.FlushAsync();
                //recvData = await sr.ReadLineAsync(); 

            //received = reader.ReadLine();
            byte[] bytes = new byte[1024];
             streamIn.Read(bytes, 0, 1024);
                recvData = Encoding.Unicode.GetString(bytes);
#endif

            lastPacket = recvData;
            Debug.Log("Read data: " + recvData);
            exchanging = false;
            i++;
        }
    }

    private void ReportDataToTrackingManager(string data)
    {

        return;
        if (data == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }

        var parts = data.Split(';');
        foreach (var part in parts)
        {
            ReportStringToTrackingManager(part);
        }
    }

    private void ReportStringToTrackingManager(string rigidBodyString)
    {
        var parts = rigidBodyString.Split(':');
        var positionData = parts[1].Split(',');
        var rotationData = parts[2].Split(',');

        int id = Int32.Parse(parts[0]);
        float x = float.Parse(positionData[0]);
        float y = float.Parse(positionData[1]);
        float z = float.Parse(positionData[2]);
        float qx = float.Parse(rotationData[0]);
        float qy = float.Parse(rotationData[1]);
        float qz = float.Parse(rotationData[2]);
        float qw = float.Parse(rotationData[3]);

        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = new Quaternion(qx, qy, qz, qw);


    }
    private void FixedUpdate()
    {
        if (recvData == null)
            return;
        textBar.text = recvData;
    }
    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            sw.Close();
            sr.Close();

            stream = null;
            exchangeThread = null;
        }
#else
        if (exchangeTask != null) {
            exchangeTask.Wait();
            socket.Dispose();
            sw.Dispose();
            sr.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        sw = null;
        sr = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

}