using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HidSharp;
using HidLibrary;



namespace ID
{
    public class EventExchanger
    {
        HidDeviceLoader _loader;
        HidStream _stream;
        HidReport hidReport;

        byte CurButtons  = 0;
        byte PrevButtons = 0;
        bool DigEvent    = false;
        bool Timeout     = false;

        bool OutputDeviceConnected = false;
        bool InputDeviceConnected = false;

        IEnumerable<HidSharp.HidDevice> Out_DeviceList;

        HidSharp.HidDevice[] Hid_Out_DeviceList;
        HidSharp.HidDevice Out_Dev, Selected_Hid_Out_Device;

        HidLibrary.HidDevice[] Hid_In_DeviceList;
        HidLibrary.HidDevice In_Dev, Selected_Hid_In_Device;

        System.Timers.Timer TimeoutTimer;

        const string _Version = "0.99b";



        // ===========================================================================================
        private  string MakeString(byte[] bytearray)
        // ===========================================================================================
        {
            string s = "";

            for (int i = 0; i < bytearray.Length; i++)
                if (i % 2 == 0)
                    if (bytearray[i] != 0)
                        s += (char)bytearray[i];
            return s;
        }
        // ===========================================================================================
        public string Version()
        // ===========================================================================================
        {
            return _Version;
        }
        // ===========================================================================================
        public string ProductName()
        // -------------------------------------------------------------------------------------------
        //    Returns a string, containing the product name of the output device.
        // -------------------------------------------------------------------------------------------
        {
            return Out_Dev.ProductName;
        }
        // ===========================================================================================
        public string DeviceInfo()
        // -------------------------------------------------------------------------------------------
        //    Returns a string, containing device info of the current output device.
        // -------------------------------------------------------------------------------------------
        {
            string ReturnString;


            if (OutputDeviceConnected)
            {
                ReturnString = "";

                ReturnString += Out_Dev.DevicePath + "\r\n" +   // string 
                                Out_Dev.MaxInputReportLength.ToString() + "\r\n" +   // int
                                Out_Dev.MaxOutputReportLength.ToString() + "\r\n" +   // int
                                Out_Dev.MaxFeatureReportLength.ToString() + "\r\n" +   // int
                                Out_Dev.Manufacturer + "\r\n" +   // string
                                Out_Dev.ProductID.ToString() + "\r\n" +   // int
                                Out_Dev.ProductName + "\r\n" +   // string
                                Out_Dev.ProductVersion.ToString() + "\r\n" +   // int
                                Out_Dev.SerialNumber + "\r\n" +   // string
                                Out_Dev.VendorID.ToString("x"); // + "\r\n" +   // int
            }
            else
                ReturnString = "EventExchanger.dll -> Device info is not accessible, device is not loaded ...";


            return ReturnString;
        }


        // ===========================================================================================
        public string GetProductNames()
        // ===========================================================================================
        {
            int EVTXCHCount = 0;

            if (OutputDeviceConnected)
                throw new Exception("Cannot list devices when Started");

            _loader = new HidDeviceLoader();

            Out_DeviceList = _loader.GetDevices(-1, -1, -1, "");

            // List Multiple Devices connected
            string ListOfProductNumbers = "";
            for (int i = 0; i < Out_DeviceList.Count(); i++)
            {
                Out_Dev = Out_DeviceList.ElementAt(i);
                if (Out_Dev.ProductName.Contains("EventExchanger"))
                {
                    if (EVTXCHCount++ > 1) ListOfProductNumbers += "/";
                    ListOfProductNumbers += Out_Dev.ProductName + ", " + Out_Dev.SerialNumber + "\r\n";
                }
            }
            Out_Dev = null;

            return ListOfProductNumbers;
        }
        // ==========================================================================================
        public string Attached()
        // ==========================================================================================
        {
            if (OutputDeviceConnected)
                throw new Exception("Cannot list devices when Started");

            _loader = new HidDeviceLoader();

            Out_DeviceList = _loader.GetDevices(-1, -1, -1, "");

            // List Multiple Devices connected
            string ListOfSerialNumbers = "";
            for (int i = 0; i < Out_DeviceList.Count(); i++)
            {
                Out_Dev = Out_DeviceList.ElementAt(i);
                if (i > 0) ListOfSerialNumbers += "/";
                ListOfSerialNumbers += Out_Dev.ProductName + "\r\n";
            }

            return ListOfSerialNumbers;
        }
        // ==========================================================================================
        public string Start_Output(string ProductName, string SerialNumber)
        // ==========================================================================================
        {
            int i;
            Boolean DeviceFound;
            string Return_Message;

            Return_Message = "EventExchanger.dll Message -> ";

            _loader = new HidDeviceLoader();

            Out_DeviceList = _loader.GetDevices(-1, -1, -1, "");

            i = 0;
            while (i < (Out_DeviceList.Count()))
            {
                DeviceFound = false;
                Out_Dev = Out_DeviceList.ElementAt(i);

                switch (SerialNumber != "")
                {
                    case false:
                        DeviceFound = Out_Dev.ProductName.Contains("EventExchanger") &&
                                      Out_Dev.ProductName.Contains(ProductName);
                        break;

                    case true:
                        DeviceFound = Out_Dev.ProductName.Contains("EventExchanger") &&
                                      Out_Dev.ProductName.Contains(ProductName) &&
                                      Out_Dev.SerialNumber.Contains(SerialNumber);
                        break;
                }

                if (DeviceFound)
                {
                    if (!Out_Dev.TryOpen(out _stream))
                    {
                        Return_Message += Out_Dev.ProductName + ", " + Out_Dev.SerialNumber + "  not opened ...";
                        throw new Exception("Failed to open detected device stream...");
                    }
                    else
                    {
                        OutputDeviceConnected = true;
                        Return_Message += Out_Dev.ProductName + ", " + Out_Dev.SerialNumber + " connected for output ... ";
                        SetLines(0);
                    }
                }
                i++;
            }

            return Return_Message;
        }
        // ==========================================================================================
        // ==========================================================================================
        public  string Start_Input(string ProductName, string SerialNumber)
        // ===================================================================================
        {
            byte[] ProdArr;
            byte[] SerNumArr;

            string ProdStr, SerNumStr, Return_Message;
            string ConnectionResult = " -> Connection ";

            int i;
            int InputReportSize = 0;
            int OutputReportSize = 0;

            Boolean DeviceFound;

            Return_Message = "EventExchanger.dll Message -> \r\n";

            Hid_In_DeviceList = HidLibrary.HidDevices.Enumerate().ToArray();

            i = 0;
            while (i < (Hid_In_DeviceList.Length))
            {
                DeviceFound = false;

                Hid_In_DeviceList[i].ReadProduct(out ProdArr);
                Hid_In_DeviceList[i].ReadSerialNumber(out SerNumArr);

                ProdStr = MakeString(ProdArr);
                SerNumStr = MakeString(SerNumArr);

                switch (SerialNumber != "")
                {
                    case false:
                        DeviceFound = ProdStr.Contains("EventExchanger") &&
                                      ProdStr.Contains(ProductName);
                        break;

                    case true:
                        DeviceFound = ProdStr.Contains("EventExchanger") &&
                                      ProdStr.Contains(ProductName) &&
                                      SerNumStr.Contains(SerialNumber);
                        break;
                }

                if (DeviceFound)
                {
                    Selected_Hid_In_Device = Hid_In_DeviceList[i];

                    Selected_Hid_In_Device.OpenDevice();

                    switch (Selected_Hid_In_Device.IsOpen)
                    {
                        case true:

                            ConnectionResult += "successfully esthablished.";
                            Selected_Hid_In_Device.MonitorDeviceEvents = true;
                            Selected_Hid_In_Device.ReadReport(ReadHandler);

                            InputReportSize = Selected_Hid_In_Device.Capabilities.InputReportByteLength;
                            OutputReportSize = Selected_Hid_In_Device.Capabilities.OutputReportByteLength;
                            break;

                        case false:

                            ConnectionResult += "failed.";
                            break;
                    }


                    Return_Message += " Device ProductString : " + ProdStr + "\r\n Serial number : "
                                    + SerNumStr + "\r\n" + ConnectionResult +
                                    " \r\n InputReportSize : " + InputReportSize.ToString() +
                                    " \r\n OutputReportSize : " + OutputReportSize.ToString(); ;
                }

                i++;
            }
            return Return_Message;
        }
        // ===================================================================================
        private  void ReadHandler(HidReport report)
        // ===================================================================================
        {
            CurButtons = report.Data[0];

            DigEvent = (CurButtons != PrevButtons);

            Selected_Hid_In_Device.ReadReport(ReadHandler);
        }
        // ===================================================================================
        public  int WaitForDigEvents(Byte AllowedEventLines, int TimeoutMSecs)
        // ===================================================================================
        {
            int  ChangedBits = 0;
            bool Exit        = false;
            bool UseTimeout  = true;


            // UseTimeout = (TimeoutMSecs > 0);
            //  Timeout = !UseTimeout;

            //if(UseTimeout)

            Timeout = false;

          //  SetTimeoutTimer(TimeoutMSecs);

            while (!Exit)
            {
                while (!DigEvent) // && !Timeout)

                //if (Timeout) // && UseTimeout)
                //    {
                //        TimeoutTimer.Stop();
                //        TimeoutTimer.Enabled = false;
                //        TimeoutTimer.Dispose();
                //        Exit = true;
                //    }


                if (!Exit)
                {
                    CurButtons &= AllowedEventLines;

                    ChangedBits = ((int)CurButtons ^ (int)PrevButtons);

                    if (ChangedBits != 0)
                        for (int i = 0; i < 8; i++)
                            if ((CurButtons & (1 << i)) != (1 << i))
                                ChangedBits &= ~(int)(1 << i);

                    if (ChangedBits != 0) Exit = true;

                    PrevButtons = CurButtons;
                    DigEvent = false;
                }

            }  // while (!Exit)

            return ChangedBits;
        }

        // ===================================================================================
        private  void SetTimeoutTimer(int TimeoutValue)
        // ===================================================================================
        {
            // Create a timer with a two second interval.
            TimeoutTimer = new System.Timers.Timer(1000);

            // Hook up the Elapsed event for the timer. 
            TimeoutTimer.Elapsed += OnTimeout;

            TimeoutTimer.AutoReset = true;
            
            TimeoutTimer.Enabled = true;
        }
        // ===================================================================================
        private  void OnTimeout(Object source, System.Timers.ElapsedEventArgs e)
        // ===================================================================================
        {
            Timeout = true;
        }







        // ===================================================================================
        // ===================================================================================
        // Constants to define command-codes given to connected EventExchanger via USB port.                                     
        // ===================================================================================
        // ===================================================================================
        const Byte CLEAROUTPUTPORT               =   0;   // 0x00
        const Byte SETOUTPUTPORT                 =   1;   // 0x01
        const Byte SETOUTPUTLINES                =   2;   // 0x02
        const Byte SETOUTPUTLINE                 =   3;   // 0x03
        const Byte PULSEOUTPUTLINES              =   4;   // 0x04
        const Byte PULSEOUTPUTLINE               =   5;   // 0x05

        const Byte SENDLASTOUTPUTBYTE            =  10;   // 0x0A

        const Byte CONVEYEVENT2OUTPUT            =  20;   // 0x14
        const Byte CONVEYEVENT2OUTPUTEX          =  21;   // 0x15
        const Byte CANCELCONVEYEVENT2OUTPUT      =  22;   // 0x16

        const Byte CANCELEVENTREROUTES           =  30;   // 0x1E
        const Byte REROUTEEVENTINPUT             =  31;   // 0x1F

        const Byte SETROTARYCONTROLLERPOSITION   =  40;   // 0x28

        const Byte CONFIGUREDEBOUNCE             =  50;   // 0x32

        const Byte SWITCHALLLINESEVENTDETECTION  = 100;   // 0x64
        const Byte SWITCHLINEEVENTDETECTION      = 101;   // 0x65

        const Byte SETANALOGINPUTDETECTION       = 102;   // 0x66
        const Byte REROUTEANALOGINPUT            = 103;   // 0X67

        const Byte SWITCHDIAGNOSTICMODE          = 200;   // 0xC8
        const Byte SWITCHEVENTTEST               = 201;   // 0xC9

        const Byte RESTART                       = 255;   // 0xFF
// ===========================================================================================
// ===========================================================================================


// ===========================================================================================
        public void SetLines(Byte OutValue)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETOUTPUTLINES, OutValue, 0, 0, 0, 0, 0, 0, 0, 0 };

            _stream.Write(USBbytes);
        }
// ===========================================================================================
        public void PulseLines(Byte OutValue, int DurationInMillisecs)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

                        var USBbytes = new Byte[] { 0, PULSEOUTPUTLINES, OutValue, (byte)DurationInMillisecs,
                                                                                   (byte)(DurationInMillisecs >> 8), 0, 0, 0, 0, 0, 0 };
            //Byte[] USBbytes ={ 0, PULSEOUTPUTLINES, OutValue, (byte)DurationInMillisecs,
            //                                                  (byte)(DurationInMillisecs >> 8), 0, 0, 0, 0, 0, 0 };


           //Selected_Hid_In_Device.Write(USBbytes);

           //Selected_Hid_In_Device.WriteReport();

            _stream.Write(USBbytes);
        }
// ===========================================================================================
        public void RerouteEventInput(Byte InputLine, Byte OutputBit)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, REROUTEEVENTINPUT, InputLine, OutputBit, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
// ===========================================================================================
        public void CancelEventReroutes(Byte dummy)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, CANCELEVENTREROUTES, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
// ===========================================================================================
        //        public void RENC_SetPosition(int Position)

        public void RENC_SetUp(int Range, int Position, byte ScaleFactor, byte StepCount)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETROTARYCONTROLLERPOSITION, (byte)Range,    (byte)(Range >> 8), 
                                                                        (byte)Position, (byte)(Position >> 8),
                                                                        ScaleFactor,    StepCount, 
                                                                        0, 0, 0 };
            _stream.Write(USBbytes);
        }
// ===========================================================================================
        public void Restart(Byte dummy)
// ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, RESTART, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
// ===========================================================================================





    }

    internal class ReadHandlerDelegate
    {
        private Action<HidReport> readHandler;

        public ReadHandlerDelegate(Action<HidReport> readHandler)
        {
            this.readHandler = readHandler;
        }
    }
}
