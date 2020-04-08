using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
//using HidSharp.DeviceHelpers;
using System.Threading;



namespace ID
{
    public class EventExchanger 
    {
        HidDeviceLoader _loader;
        HidStream _stream;
        bool LOADED = false;
        HidDevice Dev;
        IEnumerable<HidDevice> deviceList;
        const string _Version = "0.99b";

        public string Version()
        {
            return _Version;
        }

        public string ProductName()
        {
            return Dev.ProductName;
        }

        public string GetProductNames()
        {
            int EVTXCHCount = 0;

            if (LOADED)
                throw new Exception("Cannot list devices when Started");

            _loader = new HidDeviceLoader();

            deviceList = _loader.GetDevices(-1, -1, -1, "");

            // List Multiple Devices connected
            string ListOfProductNumbers = "";
            for (int i = 0; i < deviceList.Count(); i++)
            {
                Dev = deviceList.ElementAt(i);
                if (Dev.ProductName.Contains("EventExchanger"))
                {
                    if (EVTXCHCount++ > 1) ListOfProductNumbers += "/";
                    ListOfProductNumbers += Dev.ProductName;
                }
            }
            return ListOfProductNumbers;
        }

        
        public string Attached()
        { 
            if (LOADED)
                throw new Exception("Cannot list devices when Started");

            _loader = new HidDeviceLoader();

            deviceList = _loader.GetDevices(-1, -1, -1, "");

            // List Multiple Devices connected
            string ListOfSerialNumbers = "";
            for (int i = 0; i < deviceList.Count(); i++)
            {
                    Dev = deviceList.ElementAt(i);
                    if (i > 0) ListOfSerialNumbers += "/";
                    ListOfSerialNumbers += Dev.ProductName;
            }
            return ListOfSerialNumbers;
        }

/*
        public void Start()
        {
            _loader = new HidDeviceLoader();

            deviceList = _loader.GetDevices(-1, -1, -1, "");
            deviceList.Concat(_loader.GetDevices(-1, -1, -1, ""));

            // No Device connected
            if (!deviceList.Any())
            {
                deviceList.Concat(_loader.GetDevices(-1, -1, -1, ""));
                throw new Exception("No USB EVT-02/3 attached...");
            }
 
            // Multiple Devices connected
            if (deviceList.Count() > 1)
            {
                string ListOfSerialNumbers = " - ";
                for (int i = 0; i < deviceList.Count(); i++)
                {
                    Dev = deviceList.ElementAt(i);
                    ListOfSerialNumbers += Dev.SerialNumber + " / ";
                }
                throw new Exception("Multiple USB EVT-02/3 attached... Call start with a serialnumber (as a string)! " + ListOfSerialNumbers);
            }

            Dev = deviceList.ElementAt(0);
            if (!Dev.TryOpen(out _stream))
            {
                throw new Exception("Failed to open detected and opened device stream...");
            }
            LOADED = true; 
            SetLines(0);
        }
*/

        public string Start(string ProductName)
        {
            int     i;
            string  Return_Message;
            Boolean DeviceFound;

            _loader = new HidDeviceLoader();

            deviceList = _loader.GetDevices(-1, -1, -1, "");

            i = 0; DeviceFound = false;
            while (i < (deviceList.Count()) && !DeviceFound)
            {
                Dev = deviceList.ElementAt(i);
                if (Dev.ProductName.Contains("EventExchanger") && Dev.ProductName.Contains(ProductName))
                {
                    Dev = deviceList.ElementAt(i);
                    if (!Dev.TryOpen(out _stream))
                    {
                        throw new Exception("Failed to open detected and opened device stream...");
                    }
                    else
                    {
                        LOADED = true;
                        DeviceFound = true;
                        SetLines(0);
                    }
                }
                i++;
            }
        if (DeviceFound)
            Return_Message = Dev.ProductName + " has been started ...";
          else Return_Message = "Requested device (" + ProductName + ") not found ...";

            return Return_Message;
        }

        /*
                public void Start(string serialnumber)
                {
                    _loader = new HidDeviceLoader();

                    deviceList = _loader.GetDevices(-1, -1, -1, serialnumber);

                    if (!deviceList.Any())
                    {
                        throw new Exception("No USB EVT-02/3 "+ serialnumber + " attached...");
                    }

                    Dev = deviceList.ElementAt(0);
                    if (!Dev.TryOpen(out _stream))
                    {
                        throw new Exception("Failed to open detected and opened device stream...");
                    }            
                    LOADED = true;
                    SetLines(0);
                }
        */


        // ===================================================================================
        // Constants to define command-codes given to connected EventExchanger via USB port.                                     
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

        const Byte CONFIGUREDEBOUNCE             =  50;   // 0x32

        const Byte SWITCHALLLINESEVENTDETECTION  = 100;   // 0x64
        const Byte SWITCHLINEEVENTDETECTION      = 101;   // 0x65

        const Byte SETANALOGINPUTDETECTION       = 102;   // 0x66
        const Byte REROUTEANALOGINPUT            = 103;   // 0X67

        const Byte SWITCHDIAGNOSTICMODE          = 200;   // 0xC8
        const Byte SWITCHEVENTTEST               = 201;   // 0xC9

        const Byte RESTART                       = 255;   // 0xFF
// ===================================================================================


        public void SetLines(Byte OutValue)
        {
            if (!LOADED)
                throw new Exception("No USB EVT-02/3 started...");

            var USBbytes = new Byte[] { 0, SETOUTPUTLINES, OutValue, 0, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
        public void PulseLines(Byte OutValue, int DurationInMillisecs)
        {
            if (!LOADED)
                throw new Exception("No USB EVT-02/3 started...");

            var USBbytes = new Byte[] { 0, PULSEOUTPUTLINES, OutValue, (byte)DurationInMillisecs, (byte)(DurationInMillisecs >> 8), 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
        public void RerouteEventInput(Byte InputLine, Byte OutputBit)
        {
            if (!LOADED)
                throw new Exception("No USB EVT-02/3 started...");

            var USBbytes = new Byte[] { 0, REROUTEEVENTINPUT, InputLine, OutputBit, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
        public void CancelEventReroutes(Byte dummy)
        {
            if (!LOADED)
                throw new Exception("No USB EVT-02/3 started...");

            var USBbytes = new Byte[] { 0, CANCELEVENTREROUTES, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }
        public void Restart(Byte dummy)
        {
            if (!LOADED)
                throw new Exception("No USB EVT-02/3 started...");

            var USBbytes = new Byte[] { 0, RESTART, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            _stream.Write(USBbytes);
        }






    }
}
