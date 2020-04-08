using System;
using System.Collections.Generic;
using System.Linq;
//using System.Windows.Forms;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Threading;
//using System.Timers;
using HidSharp;
using HidLibrary;
//using HidSharp.DeviceHelpers;




namespace ID
{
    public class EventExchanger
    {
        HidDeviceLoader _loader;
        HidStream _stream;
        //        HidReport       hidReport;

        Byte CurButtons = 0;
        Byte PrevButtons = 0;

        const Byte Read = 0;
        const Byte Write = 1;
        const Byte Add = 2;

        Boolean DigEvent = false;
        Boolean FirstDigEvent = true;
        Boolean Timeout = false;

        Boolean OutputDeviceConnected = false;
        //        Boolean InputDeviceConnected  = false;

        IEnumerable<HidSharp.HidDevice> Out_DeviceList;

        //HidSharp.HidDevice[] Hid_Out_DeviceList;
        HidSharp.HidDevice Out_Dev; //, Selected_Hid_Out_Device;

        HidLibrary.HidDevice[] Hid_In_DeviceList;
        HidLibrary.HidDevice Selected_Hid_In_Device;

        Delegate Renc_Event_Callback;

        private static System.Timers.Timer TimeoutTimer;

        String strInputDeviceProdNameAndSNr = "";
        String strInputDeviceCaps = "";

        const String _Version = "0.99c";

        private short Usage,
                        UsagePage,
                        InputReportByteLength,      // total number of input bytes + rapport ID
                        NumberOfReportDataBytes,
                        OutputReportByteLength,
                        NumberInputButtonCaps,    // NumberOfInputButtonCaps of buttons in input byte #1
                        NumberInputValueCaps,     // number of axis values
                        NumberInputDataIndices,   // total number of data items, buttons included.
                        NumberOutputValueCaps,
                        NumberOutputDataIndices;


        Boolean InputValues_16_Bit;

        private static object EventBufferLock = new object();

        // Declaration of HID-data input buffer. Default size = 1, only 1 UInt to contain max 8 button-bits ...
        private UInt16[] EventBuffer = new UInt16[1];
        private UInt16[] PrevInputDataBuffer = new UInt16[1];

        private Boolean[] FirstEventEntered = new Boolean[1];



        // ===========================================================================================
        // ===========================================================================================



        public void TestCallback(int value)
        {
            //Renc_Event_Callback.DynamicInvoke(value);
        }




        public string SaveDelegate(Delegate RENC_Event)
        {
            Renc_Event_Callback = RENC_Event;

            //object[] paramToPass = new object[1];

            //paramToPass[0] = new int();

            //RENC_Event.DynamicInvoke(paramToPass);


            //Renc_Event_Callback.DynamicInvoke(99);

            return "Hello from DLL ...";
        }




        // ===========================================================================================
        public string GetInputDeviceInfo(int dummy)
        // ===========================================================================================
        {
            return "\r\n" + strInputDeviceProdNameAndSNr + "\r\n" + strInputDeviceCaps + "\r\n";
        }
        // ===========================================================================================
        private string MakeString(byte[] bytearray)
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
                        Return_Message += Out_Dev.ProductName + "\r\n" + "Serial number : " +
                            Out_Dev.SerialNumber + "  not opened ...";
                        throw new Exception("Failed to open detected device stream...");
                    }
                    else
                    {
                        OutputDeviceConnected = true;
                        Return_Message += Out_Dev.ProductName + "\r\n" + "Serial number : " +
                            Out_Dev.SerialNumber + " connected for output ... ";

                        SetLines(0);
                    }
                }
                i++;
            }

            return Return_Message;
        }
        // ==========================================================================================
        // ==========================================================================================
        public string Start_Input(string ProductName, string SerialNumber)
        // ===================================================================================
        {
            byte[] ProdArr;
            byte[] SerNumArr;

            string ProdStr, SerNumStr, Return_Message;
            string ConnectionResult = "   Input connection ";
            string AxisDatatype = "AxisDataType               = BYTE";

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

                            InputReportSize = Selected_Hid_In_Device.Capabilities.InputReportByteLength;
                            OutputReportSize = Selected_Hid_In_Device.Capabilities.OutputReportByteLength;

                            strInputDeviceProdNameAndSNr = "Input device " + ProdStr + " with serial number " + SerNumStr;


                            Usage = Selected_Hid_In_Device.Capabilities.Usage;
                            UsagePage = Selected_Hid_In_Device.Capabilities.UsagePage;

                            InputReportByteLength = Selected_Hid_In_Device.Capabilities.InputReportByteLength;
                            NumberOfReportDataBytes = (short)(InputReportByteLength - 1);
                            OutputReportByteLength = Selected_Hid_In_Device.Capabilities.OutputReportByteLength;
                            NumberInputButtonCaps = Selected_Hid_In_Device.Capabilities.NumberInputButtonCaps;
                            NumberInputValueCaps = Selected_Hid_In_Device.Capabilities.NumberInputValueCaps;
                            NumberInputDataIndices = Selected_Hid_In_Device.Capabilities.NumberInputDataIndices;
                            NumberOutputValueCaps = Selected_Hid_In_Device.Capabilities.NumberOutputValueCaps;
                            NumberOutputDataIndices = Selected_Hid_In_Device.Capabilities.NumberOutputDataIndices;


                            if (NumberOfReportDataBytes > 1)
                            {
                                InputValues_16_Bit = ((NumberOfReportDataBytes - 1) != (NumberInputDataIndices - 8));
                                if (InputValues_16_Bit) AxisDatatype = "AxisDataType               = UInt16";
                            }
                            else
                                AxisDatatype = "   ***** No axes used *****";

                            // Resize HID data related buffers according to the number of input data indices - 8 
                            // (each 'button-bit' representing 1 index)

                            Array.Resize(ref EventBuffer, (NumberInputDataIndices - 8) + 1);
                            Array.Resize(ref PrevInputDataBuffer, (NumberInputDataIndices - 8) + 1);
                            Array.Resize(ref FirstEventEntered, (NumberInputDataIndices - 8) + 1);

                            for (int ind = 0; i < ((NumberInputDataIndices - 8) + 1); i++)
                            {
                                EventBuffer[ind] = 0;
                                PrevInputDataBuffer[ind] = 0;
                                FirstEventEntered[ind] = false;
                            }


                            // Assemble relevant InputDeviceCaps in a string to be used as a part of 
                            // the return string in method GetInputDeviceInfo(int dummy).

                            strInputDeviceCaps =
                            "Usage                      = " + Usage.ToString("X2") + "\r\n" +
                            "UsagePage                  = " + UsagePage.ToString("X2") + "\r\n" +
                            "Input report length        = " + InputReportByteLength.ToString("X2") + "\r\n" +
                             AxisDatatype + "\r\n" +
                            "InputDataBuffer_UBound     = " + EventBuffer.GetUpperBound(0).ToString("X2") + "\r\n" +
                            "FirstEventEntered_UBound   = " + FirstEventEntered.GetUpperBound(0).ToString("X2") + "\r\n" +
                            "Output report length       = " + OutputReportByteLength.ToString("X2") + "\r\n" +
                            "NumberInputButtonCaps      = " + NumberInputButtonCaps.ToString("X2") + "\r\n" +
                            "NumberInputValueCaps       = " + NumberInputValueCaps.ToString("X2") + "\r\n" +
                            "NumberInputDataIndices     = " + NumberInputDataIndices.ToString("X2") + "\r\n" +
                            "NumberOutputValueCaps      = " + NumberOutputValueCaps.ToString("X2") + "\r\n" +
                            "NumberOutputDataIndices    = " + NumberOutputDataIndices.ToString("X2") + "\r\n";


                            Return_Message += "\r\n   Device ProductString : " + ProdStr + "\r\n   Serial number : "
                                                + SerNumStr + "\r\n" + ConnectionResult +
                                                " \r\n   InputReportSize : " + InputReportSize.ToString() +
                                                " \r\n   OutputReportSize : " + OutputReportSize.ToString(); ;


                            ConnectionResult += "successfully esthablished.";

                            // Enable device event monitoring.
                            Selected_Hid_In_Device.MonitorDeviceEvents = true;
                            // Allocate callback function ReadHandler to be called when 
                            // device event monitoring has read a report.
                            Selected_Hid_In_Device.ReadReport(ReadHandler);

                            break;


                        case false:

                            ConnectionResult += "failed.";

                            Return_Message = "Failed to open device ...";
                            break;
                    }
                }

                i++;
            }
            return Return_Message;
        }
        // ===================================================================================
        private void ReadHandler(HidReport report)
        // ===================================================================================
        /*
         ReadHandler is called when device event monitoring has detected an event and has read the report.
         This report is handed to ReadHandler and will be processed here ...
        */
        {
            CurButtons = report.Data[0];
            EventBuffer[0] = report.Data[0];



            //CurInputDataBuffer[] 
            //PrevInputDataBuffer[]
            //FirstEventEntered[]

            // Check if report contains more that 1 byte.
            // if only 1 byte has entered, it is default (button) data.
            if (NumberOfReportDataBytes > 1)
                for (uint i = 1; i <= NumberInputValueCaps; i++)
                {
                    AccessEventBuffer(Write, i, report.Data[((i - 1) * 2) + 1]);

                    if (InputValues_16_Bit)
                        AccessEventBuffer(Add, i, (UInt16)(report.Data[((i - 1) * 2) + 2] << 8));
                }


            DigEvent = CurButtons != PrevButtons;

            Selected_Hid_In_Device.ReadReport(ReadHandler);
        }
        // ===================================================================================
        public UInt16 AccessEventBuffer(Byte Mode, uint Index, UInt16 NewValue)
        // ===================================================================================
        {
            // Mode : 
            //       Read  = 0
            //       Write = 1
            //       Add   = 2

            lock (EventBufferLock)
            {
                switch (Mode)
                {
                    case Read:
                        return EventBuffer[Index];

                    case Write:
                        EventBuffer[Index] = NewValue;
                        break;
                    case Add:
                        EventBuffer[Index] += NewValue;
                        break;
                }
            }
            return 0;
        }
        // ===================================================================================
        public Int16 Get_Axis(uint AxisNumber)
        // ===================================================================================
        {
            if (NumberInputValueCaps == 0) return -1;
            if (AxisNumber > NumberInputValueCaps) return -2;

            return (Int16)AccessEventBuffer(Read, AxisNumber, 0);
        }
        // ===================================================================================
        public int WaitForDigEvents(Byte AllowedEventLines, int TimeoutMSecs)
        /* ===================================================================================
            Waits for a digital event (button press or another event on the 8 digital inputs)
            from the connected EventExchanger until a timeout has been reached.
            When an event occurs before timeout, it will be compared with the allowed events
            and processed when valid. The allowed changed bit(s) will be returned in an int.
            When a timeout occurred,  -1 will be returned.

            The first part initializes some booleans and equals previous button input with the
            the last detected input, current buttons.
            When the time-out parameter > 0, the time-out timer is initialized and started.
            Time-out evetnt callback 'OnTimeoutEvent(Object source, System.Timers.ElapsedEventArgs e)'
            is added to TimeoutTimer.Elapsed event handler.
            
            Then a wait loop is entered until DigEvent or Timeout is true.
            When a report is detected by the device-monitoring system, the readhandler processes the 
            report and sets variable DigEvent to true when this was the reason for the report to be sent.

            When this is the case, the allowed bits will be checked for changes by EXOR'ing previous and 
            current buttons. 

            Then a check is done if the changed bits are 1 or 0, being a button-press or button-release respectively.

            In case of press, the method returns a positive button number, at a release 
            a negative button number will be returned.

        Examples :

            1:

            PrevButtons            0000 0000     
            CurButtons             0000 0100     
            --------------------------------- ~ 
            ChangedBits            0000 0100

            ButtonNumber = changed bit position + 1 -> 2 + 1 = 3

            BitValue of CurButtons bit at changed bit position = 1 -> 
                                    ButtonPress = true , the button was pressed.

            2:

            PrevButtons            0000 0100    
            CurButtons             0000 0000     
            --------------------------------- ~ 
            ChangedBits            0000 0100

            ButtonNumber = changed-bit position + 1 -> 2 + 1 = 3

            BitValue of CurButtons bit at changed-bit position = 0 -> 
                                    ButtonPress = false, the already pressed button was released.
           ===================================================================================
        */
        {
            Byte ChangedBits = 0;

            bool UseTimeout = false;
            bool ButtonPress;

            int retval = -666;

            int i, ButtonNumber;

            DigEvent = false;
            FirstDigEvent = true;
            PrevButtons = CurButtons;

            // Check if timout is defined, this is only the case when the parameter > 0;
            UseTimeout = (TimeoutMSecs > 0);

            if (UseTimeout)
            {
                Timeout = false;
                TimeoutTimer = new System.Timers.Timer();
                TimeoutTimer.Interval = TimeoutMSecs;
                TimeoutTimer.Elapsed += OnTimeoutEvent; // Add event callback method.
                TimeoutTimer.AutoReset = true;
                TimeoutTimer.Enabled = true;
            }

            while (!DigEvent && !Timeout) { }

            if (!Timeout)
            {
                CurButtons &= AllowedEventLines;

                ChangedBits = (Byte)(CurButtons ^ PrevButtons);

                if (ChangedBits != 0)
                {
                    // Determine wich bit changed and convert position 0-7 to button number 1-8.
                    i = 0;
                    while (((ChangedBits & (1 << i)) != (1 << i)) && (i < 8)) { i++; }
                    // Button number = bitnumber (i) + 1.
                    ButtonNumber = i + 1;
                    // When the corresponding bit in CurButtons also is 1, 
                    // it was a button press, else it was a button release.
                    ButtonPress = ((CurButtons & (1 << i)) == (1 << i));

                    if (ButtonPress)
                        retval = ButtonNumber;   // return positive number for press.
                    else
                        retval = -ButtonNumber; // return positive number for release.
                }
            }
            else // Timeout ...
            {
                retval = -100;
            }

            if (UseTimeout)
            {
                Timeout = false;
                TimeoutTimer.Stop();
                TimeoutTimer.Dispose();
            }

            return retval;
        }
        // ===================================================================================
        private void OnTimeoutEvent(Object source, System.Timers.ElapsedEventArgs e)
        // ===================================================================================
        {
            if (FirstDigEvent)
            {
                Timeout = true;
                FirstDigEvent = false;
            }
        }






        // ===================================================================================
        // ===================================================================================
        // Constants to define command-codes given to connected EventExchanger via USB port.                                     
        // ===================================================================================
        // ===================================================================================
        const Byte CLEAROUTPUTPORT = 0;   // 0x00
        const Byte SETOUTPUTPORT = 1;   // 0x01
        const Byte SETOUTPUTLINES = 2;   // 0x02
        const Byte SETOUTPUTLINE = 3;   // 0x03
        const Byte PULSEOUTPUTLINES = 4;   // 0x04
        const Byte PULSEOUTPUTLINE = 5;   // 0x05

        const Byte SENDLASTOUTPUTBYTE = 10;   // 0x0A

        const Byte CONVEYEVENT2OUTPUT = 20;   // 0x14
        const Byte CONVEYEVENT2OUTPUTEX = 21;   // 0x15
        const Byte CANCELCONVEYEVENT2OUTPUT = 22;   // 0x16

        const Byte CANCELEVENTREROUTES = 30;   // 0x1E
        const Byte REROUTEEVENTINPUT = 31;   // 0x1F

        const Byte SETUPROTARYCONTROLLER = 40;        // 0x28
        const Byte SETROTARYCONTROLLERPOSITION = 41;  // 0x29

        const Byte CONFIGUREDEBOUNCE = 50;   // 0x32

        const Byte SETWS2811RGBLEDCOLOR = 60;  // 0x3C
        const Byte SENDLEDCOLORS = 61;          // 0x3D

        const Byte SWITCHALLLINESEVENTDETECTION = 100;   // 0x64
        const Byte SWITCHLINEEVENTDETECTION = 101;   // 0x65

        const Byte SETANALOGINPUTDETECTION = 102;   // 0x66
        const Byte REROUTEANALOGINPUT = 103;            // 0X67
        const Byte SETANALOGEVENTSTEPSIZE = 104;     // 0X68

        const Byte SWITCHDIAGNOSTICMODE = 200;   // 0xC8
        const Byte SWITCHEVENTTEST = 201;   // 0xC9

        const Byte RESTART = 255;   // 0xFF

        // ===========================================================================================
        // ===========================================================================================


        // ===========================================================================================
        public void SetAnalogEventStepSize(Byte NumberOfSamplesPerStep)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETANALOGEVENTSTEPSIZE, NumberOfSamplesPerStep, 0, 0, 0, 0, 0, 0, 0, 0 };

            _stream.Write(USBbytes);
        }
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
        public void RENC_SetUp(int Range, int MinimumValue, int Position, byte InputChange, byte PulseInputDivider)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETUPROTARYCONTROLLER, (byte)Range,        (byte)(Range >> 8),
                                                                  (byte)MinimumValue, (byte)(MinimumValue >> 8),
                                                                  (byte)Position,     (byte)(Position >> 8),
                                                                   InputChange, PulseInputDivider, 0 };
            _stream.Write(USBbytes);
        }
        // ===========================================================================================
        public void RENC_SetPosition(int Position)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETROTARYCONTROLLERPOSITION, (byte)Position, (byte)(Position >> 8),
                                                                        0, 0, 0, 0, 0, 0, 0 };
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
        public void ConveyEvent2Output (Byte EventLine, Byte OutputLine, Byte InitialBitValue,
                                        Byte Mode,      Int16 Duration)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, CONVEYEVENT2OUTPUT, EventLine, OutputLine, InitialBitValue,
                                        Mode, (Byte)Duration, (Byte)(Duration >> 8) , 0, 0, 0 };
            _stream.Write(USBbytes);
        }
        // ===========================================================================================
        public void SetLedColor (Byte RedValue, Byte GreenValue, Byte BlueValue, Byte LedNumber, Byte Mode)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SETWS2811RGBLEDCOLOR, RedValue, GreenValue, BlueValue, LedNumber, Mode, 0, 0, 0, 0};
            _stream.Write(USBbytes);
        }
        // ===========================================================================================
        public void SendColors (Byte NumberOfLeds, Byte Mode)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SENDLEDCOLORS, NumberOfLeds, Mode,0,0,0,0,0,0,0 };
            _stream.Write(USBbytes);
        }


        // ===========================================================================================
        public void ChangeInputLineStatus (Byte Mode, Byte LineNumber)
        // ===========================================================================================
        {
            if (!OutputDeviceConnected)
                throw new Exception("No USB EventExchanger started...");

            var USBbytes = new Byte[] { 0, SWITCHLINEEVENTDETECTION, Mode, LineNumber, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }









    }


    //internal class ReadHandlerDelegate
    //{
    //    private Action<HidReport> readHandler;

    //    public ReadHandlerDelegate(Action<HidReport> readHandler)
    //    {
    //        this.readHandler = readHandler;
    //    }
    //}
}
