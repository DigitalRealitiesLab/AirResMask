using System.Collections.Generic;
using UnityEngine;
using ArduinoBluetoothAPI;
using UnityEngine.SceneManagement;

public class ESP32ClientBluetooth : MonoBehaviour
{
    int currentValue = 0; // Martin Wolf: Last received value fom the sensor
    public int CurrentValue { get { return currentValue; } }

    int airResistance = 0;
    public int AirResistance { get { return airResistance; } set { airResistance = value; } }

    public int airResistanceSteps = 100; // Martin Wolf: How many different air resistance values can be set -> depends on step angles of the breathing resistance
    public int AirResistanceSteps { get { return airResistanceSteps; } }

    float offset = 32768.0f, scaleFactor = 120.0f; // Martin Wolf: Set from SFM3300 Data Sheet
    public float Offset { get { return offset; } }
    public float ScaleFactor { get { return scaleFactor; } }

    bool gotBluetoothValues = false;
    public bool GotBluetoothValues { get { return gotBluetoothValues; } }

    protected int priority = -1; // Martin Wolf: Helper variable to ensure that the first ESP32ClientBluetooth is retained throughout all scene changes

    int oldAirResistance = -1;

    BluetoothHelper helper;

    float helperResetTime = 2.0f; // Martin Wolf: Time after which the BluetoothHelper is reinstantiated to ensure connectivity if a new device is paired
    float helperResetTimer = 0.0f;

    Queue<byte> values;

    void Awake()
    {
        // Martin Wolf: Ensure that the first ESP32ClientBluetooth is retained throughout all scene changes
        ESP32ClientBluetooth[] clients = FindObjectsOfType<ESP32ClientBluetooth>();

        foreach(ESP32ClientBluetooth client in clients)
        {
            if (client != this && priority < client.priority)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (priority == -1)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            priority = 0;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        helper = BluetoothHelper.GetInstance("InhaleVR");

        helper.OnConnected += OnConnected;
        helper.OnConnectionFailed += OnConnectionFailed;
        helper.OnDataReceived += OnDataReceived;

        helper.setCustomStreamManager(new MyStreamManager());

        values = new Queue<byte>();
    }

    void Update()
    {
        if (helper != null && helperResetTimer < helperResetTime)
        {
            helperResetTimer += Time.deltaTime;
            if (helper.isDevicePaired())
            {
                if (!helper.isConnected())
                {
                    helper.Connect();
                }
                else if (gotBluetoothValues && oldAirResistance != airResistance && !helper.Available)
                {
                    SendAirResistance(airResistance);
                    oldAirResistance = airResistance;
                }
            }
        }
        else if (helper == null || helper.isDevicePaired())
        {
            gotBluetoothValues = false;
            helperResetTimer = 0.0f;
            helper = BluetoothHelper.GetNewInstance("InhaleVR");

            helper.OnConnected += OnConnected;
            helper.OnConnectionFailed += OnConnectionFailed;
            helper.OnDataReceived += OnDataReceived;

            helper.setCustomStreamManager(new MyStreamManager());
        }
        else
        {
            gotBluetoothValues = false;
        }
    }

    void OnDataReceived(BluetoothHelper helper)
    {
        helperResetTimer = 0.0f;
        while (helper.Available)
        {
            List<byte> newValues = new List<byte>(helper.ReadBytes());

            foreach (byte value in newValues)
            {
                values.Enqueue(value);
            }

            // Martin Wolf: Sensor data is transmitted as a two byte integer
            while (values.Count >= 2)
            {
                byte a, b;
                a = values.Dequeue();
                b = values.Dequeue();

                currentValue = (a << 8) | b;
                gotBluetoothValues = true;
            }
        }
    }

    void OnConnected(BluetoothHelper helper)
    {
        helperResetTimer = 0.0f;
        oldAirResistance = -1;
        helper.StartListening();
    }

    void OnConnectionFailed(BluetoothHelper helper)
    {
        // Martin Wolf: If connection fails it can usually not be established until the application is restarted
    }

    void SendAirResistance(int value)
    {
        helper.SendData(value.ToString());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        airResistance = 0;
        oldAirResistance = -1;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (helper != null && helper.isDevicePaired() && helper.isConnected() && gotBluetoothValues)
        {
            if (focus)
            {
                SendAirResistance(airResistance);
            }
            else
            {
                SendAirResistance(0);
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (helper != null && helper.isDevicePaired() && helper.isConnected() && gotBluetoothValues)
        {
            if (pause)
            {
                SendAirResistance(0);
            }
            else
            {
                SendAirResistance(airResistance);
            }
        }
    }
}
