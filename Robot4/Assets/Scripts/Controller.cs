using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Controller : MonoBehaviour
{
    public Dropdown mode_dropdown;
    public Dropdown baudrate_dropdown;
    public Dropdown port_dropdown;
    public Button connect_button;
    public Button zeroPort;
    public Button stopButton;
    public Button programButton;
    public Button zero_position_Button;
    public Slider M1_slider;
    public Slider M2_slider;
    public Slider M3_slider;
    public Slider M4_slider;
    public Text M1_Text;
    public Text M2_Text;
    public Text M3_Text;
    public Text M4_Text;

    // ********** Serial Driver *************//
    List<string> port_name_list = new List<string>();
    int selected_port_index = 0;
    List<SerialPortUtility.SerialPortUtilityPro> SerialDeviceList = new List<SerialPortUtility.SerialPortUtilityPro>();
    SerialPortUtility.SerialPortUtilityPro.OpenSystem openMode = SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB;
    // ***********************//

    void Start()
    {
        Debug.Log("START");
    }
    void Update()
    {
    }

    public void ChangeMode(Dropdown mode_selected)
    {
        if (mode_selected.value == 1)
        {
            Debug.Log("Usb Serial Mode");
            openMode = SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB;
            port_dropdown.interactable = true;
            mode_dropdown.interactable = false;
            ScanDeviceList();
        }
        else if (mode_selected.value == 2)
        {
            Debug.Log("Bluetooth Serial Mode");
            openMode = SerialPortUtility.SerialPortUtilityPro.OpenSystem.BluetoothSSP;
            port_dropdown.interactable = true;
            mode_dropdown.interactable = false;
            ScanDeviceList();
        }
        port_dropdown.interactable = true;
    }
    public void ChangePort(Dropdown port_selected)
    {
        if (port_selected.value != 0)
        {
            selected_port_index = port_selected.value - 1;
            connect_button.interactable = true;
            Debug.Log("selected_port_index = " + selected_port_index.ToString());

            baudrate_dropdown.interactable = true;
            connect_button.interactable = true;
        }
    }
    public void ChangeBaudrate(Dropdown mode_selected)
    {
        // Fixed Baudrate
    }
    public void ConnectButtonClick()
    {
        OpenSerialPort(); // Connect Serial Port

        programButton.interactable        = true;
        stopButton.interactable           = true;
        zero_position_Button.interactable = true;
    }

    public void StopAll()
    {
        StartCoroutine(StopAllCoroutine());
    }
    public void Program()
    {
        StartCoroutine(ProgramCoroutine());
    }
    public void ZeroPosition()
    {
        StartCoroutine(ZeroPositionCoroutine());
    }

    public void UpdateText(int motor_id)
    {
        int angle = 0;

        switch (motor_id)
        {
            case 1: // Left hand
                angle = (int)M1_slider.value;
                M1_Text.text = angle.ToString();
                break;
            case 2: // Left leg
                angle = (int)M3_slider.value;
                M3_Text.text = angle.ToString();
                break;
            case 3: // Right hand
                angle = (int)M2_slider.value;
                M2_Text.text = angle.ToString();
                break;
            case 4: // Right leg
                angle = (int)M4_slider.value;
                M4_Text.text = angle.ToString();
                break;
        }

        // Serial send cmd
        if ((angle > -70) && (angle < 70))
        {
            string rotate_angle = "";
            if (angle >= 10) { rotate_angle = "+" + angle.ToString(); }
            else if (angle >= 0) { rotate_angle = "+0" + angle.ToString(); }
            else if (angle <= -10) { angle = -angle; rotate_angle = "-" + angle.ToString(); }
            else if (angle < 0) { angle = -angle; rotate_angle = "-0" + angle.ToString(); }

            switch (motor_id)
            {
                case 1:
                    SerialDeviceList[selected_port_index].Write("WA" + rotate_angle);
                    break;
                case 2:
                    SerialDeviceList[selected_port_index].Write("WB" + rotate_angle);
                    break;
                case 3:
                    SerialDeviceList[selected_port_index].Write("WC" + rotate_angle);
                    break;
                case 4:
                    SerialDeviceList[selected_port_index].Write("WD" + rotate_angle);
                    break;
            }
        }
    }
    
    // ********** Serial Driver *************//
    public void ScanDeviceList()
    {
        //Because processing of plugin carries out by start, it is necessary to generate by Awake. 

        SerialPortUtility.SerialPortUtilityPro.DeviceInfo[] devicelist = SerialPortUtility.SerialPortUtilityPro.GetConnectedDeviceList(openMode);

        if (devicelist == null) return;

        foreach (SerialPortUtility.SerialPortUtilityPro.DeviceInfo d in devicelist)
        {
            String GameObjectName = "NONE";
            if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB) GameObjectName = "VID:" + d.Vendor + ", PID:" + d.Product;
            else if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.BluetoothSSP) GameObjectName = d.SerialNumber;
            else if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.PCI) GameObjectName = d.Vendor;
            GameObject obj = new GameObject(GameObjectName);
            SerialPortUtility.SerialPortUtilityPro serialPort = obj.AddComponent<SerialPortUtility.SerialPortUtilityPro>();
            Debug.Log("Port " + d.PortName);

            //config
            serialPort.SetDebugConsoleMonitorView(true);
            serialPort.OpenMethod = openMode;
            serialPort.VendorID = d.Vendor;
            serialPort.ProductID = d.Product;
            serialPort.SerialNumber = d.SerialNumber;
            serialPort.BaudRate = 115200;

            serialPort.ReadProtocol = SerialPortUtility.SerialPortUtilityPro.MethodSystem.Streaming;
            serialPort.ReadCompleteEventObject.AddListener(this.RxCallBack);	//read function
            serialPort.RecvDiscardNull = true;

            serialPort.SetDebugConsoleMonitorView(false);
            serialPort.IsAutoOpen = false;
            SerialDeviceList.Add(serialPort);

        #if UNITY_EDITOR
            port_name_list.Add(d.PortName);
        #elif UNITY_ANDROID
            if      (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB) {port_name_list.Add(d.Product);}
            else if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.BluetoothSSP) {port_name_list.Add(d.SerialNumber);}                  
        #elif UNITY_IOS
            port_name_list.Add(d.Product);
        #else
            port_name_list.Add(d.PortName);
        #endif
        }
        port_dropdown.AddOptions(port_name_list);
    }
    public void RxCallBack(object data)
    {
        var text = data as string;
        //RX_Text.text += text;
        Debug.Log("Received");
    }
    public void ClearRxScreen()
    {
    }
    void OnDestory()
    {
        if (SerialDeviceList[selected_port_index] != null) SerialDeviceList[selected_port_index].Close();
    }
    public void OpenSerialPort()
    {
        SerialDeviceList[selected_port_index].Open();
        connect_button.interactable = false;
        baudrate_dropdown.interactable = false;
        port_dropdown.interactable = false;

        connect_button.GetComponentInChildren<Text>().text = "Connected";
        Debug.Log("Open Serial Port");
    }
    public void MotorCMD(int motor_id, int angle)
    {
        if ((angle > -70) && (angle < 70))
        {
            string rotate_angle = "";
            if (angle >= 10) { rotate_angle = "+" + angle.ToString(); }
            else if (angle >= 0) { rotate_angle = "+0" + angle.ToString(); }
            else if (angle <= -10) { angle = -angle; rotate_angle = "-" + angle.ToString(); }
            else if (angle < 0) { angle = -angle; rotate_angle = "-0" + angle.ToString(); }

            switch (motor_id)
            {
                case 1:
                    SerialDeviceList[selected_port_index].Write("WA" + rotate_angle);
                    break;
                case 2:
                    SerialDeviceList[selected_port_index].Write("WB" + rotate_angle);
                    break;
                case 3:
                    SerialDeviceList[selected_port_index].Write("WC" + rotate_angle);
                    break;
                case 4:
                    SerialDeviceList[selected_port_index].Write("WD" + rotate_angle);
                    break;
            }
        }

    }
    IEnumerator StopAllCoroutine()
    {
        SerialDeviceList[selected_port_index].Write("WAS00");
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WBS00");
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WCS00");
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WDS00");
    }
    IEnumerator ZeroPositionCoroutine()
    {
        SerialDeviceList[selected_port_index].Write("WA000");
        M1_Text.text = "0";
        M1_slider.value = 0;
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WB000");
        M2_Text.text = "0";
        M2_slider.value = 0;
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WC000");
        M3_Text.text = "0";
        M3_slider.value = 0;
        yield return new WaitForSeconds((float)0.2);

        SerialDeviceList[selected_port_index].Write("WD000");
        M4_Text.text = "0";
        M4_slider.value = 0;
    }
    IEnumerator ProgramCoroutine()
    {
        int number_loop = 2;
        // Step 1
        MotorCMD(1, 0); yield return new WaitForSeconds((float)0.1);
        MotorCMD(2, 0); yield return new WaitForSeconds((float)0.1);
        MotorCMD(3, 0); yield return new WaitForSeconds((float)0.1);
        MotorCMD(4, 0); yield return new WaitForSeconds((float)0.1);
        yield return new WaitForSeconds(2);

        for (int i = 0; i < number_loop; i++)
        {
            // Step 2.1
            MotorCMD(4, 30); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.2);

            // Step 3.1
            MotorCMD(3, 20); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.5);

            // Step 4.1
            MotorCMD(1, 0); yield return new WaitForSeconds((float)0.1);
            MotorCMD(2, 0); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.6);
            MotorCMD(1, -25); yield return new WaitForSeconds((float)0.1);
            MotorCMD(2, -25); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)1);

            // Step 5.1
            MotorCMD(3, 0); yield return new WaitForSeconds((float)0.1);
            MotorCMD(4, 0); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.6);

            //---Half ---//

            // Step 2.2
            MotorCMD(3, -30); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.2);

            // Step 3.2
            MotorCMD(4, -20); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.5);

            // Step 4.2
            MotorCMD(1, 0); yield return new WaitForSeconds((float)0.1);
            MotorCMD(2, 0); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.6);
            MotorCMD(1, 25); yield return new WaitForSeconds((float)0.1);
            MotorCMD(2, 25); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)1);

            // Step 5.2
            MotorCMD(3, 0); yield return new WaitForSeconds((float)0.1);
            MotorCMD(4, 0); yield return new WaitForSeconds((float)0.1);
            yield return new WaitForSeconds((float)0.6);
        }

        // Step 2.2
        MotorCMD(4, 30); yield return new WaitForSeconds((float)0.1);
        yield return new WaitForSeconds((float)0.2);

        // Step 3.2
        MotorCMD(3, 20); yield return new WaitForSeconds((float)0.1);
        yield return new WaitForSeconds((float)0.6);

        // Step 4.2
        MotorCMD(1, 0); yield return new WaitForSeconds((float)0.1);
        MotorCMD(2, 0); yield return new WaitForSeconds((float)0.1);
        yield return new WaitForSeconds((float)0.6);

        // Step 5.2
        MotorCMD(3, 0); yield return new WaitForSeconds((float)0.1);
        MotorCMD(4, 0); yield return new WaitForSeconds((float)0.1);
        yield return new WaitForSeconds((float)0.6);

        // Stop all motor
        SerialDeviceList[selected_port_index].Write("WAS00");
        yield return new WaitForSeconds((float)0.1);
        SerialDeviceList[selected_port_index].Write("WBS00");
        yield return new WaitForSeconds((float)0.1);
        SerialDeviceList[selected_port_index].Write("WCS00");
        yield return new WaitForSeconds((float)0.1);
        SerialDeviceList[selected_port_index].Write("WDS00");

    }
    // ***********************//
}
