using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using PCANDevice;

using Force.Crc32;
using System.Collections.Concurrent;



namespace can_loader
{
    public partial class Form1 : Form
    {
        static Form mainForm;
        static TextBox rxBox;
        static TextBox txBox;

        static ConcurrentQueue<CANMessage> CANQueue = new ConcurrentQueue<CANMessage>();
        System.Threading.ManualResetEvent mre;

        static Dictionary<byte, ManualResetEvent> mreDict = new Dictionary<byte, ManualResetEvent>();
        static Dictionary<byte, byte> responseCodeDict = new Dictionary<byte, byte>();
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mainForm = this;
            rxBox = rxTextBox;
            txBox = txTextBox;

            UInt32[] testArray = new uint[8]; ;
            for (UInt32 i = 0; i < 8; i++)
                testArray[i] = i;

            UInt32 checksum = CRC32C(testArray, 8);
            Thread.Sleep(1);
        }

        PCANDevice.PCANManager pcan;
        bool canIsStarted = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (!canIsStarted)
            {
                List<ushort> interfaces = PCANDevice.PCANManager.GetAllAvailable();
                pcan = new PCANDevice.PCANManager();

                var oo = pcan.Connect(interfaces[0], PCANDevice.TPCANBaudrate.PCAN_BAUD_500K);
                pcan.AddReceiveCallback(callback);
                pcan.ActivateAutoReceive();

                canIsStarted = true;
            }
            
        }


        static ulong rxCounter = 0;
        public static int callback(object[] args)
        {
            TPCANMsg msg = (TPCANMsg)args[0];

            
            string messageText = rxCounter.ToString() + "," + msg.ID.ToString("X2") + ":[";

            for (int i = 0; i < msg.LEN; i++)
            {
                messageText += " " + msg.DATA[i].ToString("X2");
            }
            messageText += "]";

            
            rxBox.Invoke((MethodInvoker)(() => rxBox.AppendText(messageText + Environment.NewLine)));
            
            
            rxCounter++;

            
            CANQueue.Enqueue(new CANMessage((ushort)msg.ID, msg.LEN, msg.DATA));

            
            return 0;
        }

        private void txTextBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void txTextBox_DragDrop(object sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.Data.GetData)
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
        UInt32[] binData;
        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.DefaultExt = ".bin";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            /*DialogResult res = openFileDialog1.ShowDialog();
            if (res != DialogResult.OK)
                return;

            string filepath = openFileDialog1.FileName;
            */


            //byte[] byteAr = System.IO.File.ReadAllBytes(filepath);
            //for (int i = 0; i < byteAr.Length; i += 4)
            //binData = uint32_to_bytes(CANMessage.arr_SensorPackage_bin);
            //binData = CANMessage.arr_SensorPackage_22000_bin;
            binData = CANMessage.arr_SensorPackage_8000_bin;
            
            string binText = "";
            for (int i = 0; i < binData.Length; i++)
            {
                binText += ((byte) binData[i]).ToString("X2") + " ";
                binText += ((byte)(binData[i]>>8)).ToString("X2") + " ";
                binText += ((byte)(binData[i]>>16)).ToString("X2") + " ";
                binText += ((byte) (binData[i]>>24)).ToString("X2") + " ";
            }

            binTextBox.Text = binText;


        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //send 1 frame

            pcanSend(0x11, 1, new byte[] { 1 });
        }

        void pcanSend(int id, byte len, byte[] data)
        {
            string messageText = id.ToString("X2") + ":[";

            for (int i = 0; i < len; i++)
            {
                messageText += " " + data[i].ToString("X2");
            }
            messageText += "]";

           

            pcan.SendFrame(id, len, data);
        }

        void pcanSend(CANMessage msg)
        {
            string messageText = msg.ID.ToString("X2") + ":[";

            for (int i = 0; i < msg.DLC; i++)
            {
                messageText += " " + msg.Data[i].ToString("X2");
            }
            messageText += "]";


            //txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText(messageText + Environment.NewLine)));

            pcan.SendFrame(msg.ID, msg.DLC, msg.Data);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //send 5 frames
            for (int i = 0; i < 5; i++)
            {
                byte num = (byte)(i * 8);
                byte[] data = new byte[8];
                for (int j = 0; j < 8; j++)
                    data[j] = (byte)(i * 8 + j);

                //pcan.SendFrame(0x70 + i, 8, data);
                pcanSend((byte)(0x70 + i), 8, data);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //send N frames
            for (int i = 0; i < 50; i++)
            {
                byte num = (byte)(i * 8);
                byte[] data = new byte[8];
                for (int j = 0; j < 8; j++)
                    data[j] = (byte)(i * 8 + j);

                //pcan.SendFrame(0x70 + i, 8, data);
                pcanSend(0x70 + i, 8, data);
            }
        }

        
        





        private void button7_Click(object sender, EventArgs e)
        {
            UInt32[] byteAr = new UInt32[512];
            for (int i = 0; i < byteAr.Length; i++)
                byteAr[i] = (UInt32) (i);

            UInt32 offset = 0;

            Thread newThread = new Thread(()=>doChunkTransfer(byteAr, offset));
            newThread.Start();


            //now we need to wait for it to respond
        }
        

        private void button8_Click(object sender, EventArgs e)
        {
            //send echo command
            pcanSend(microcontrollerID, 1, new byte[] { 0x22 });
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //send reset command
            //pcanSend(microcontrollerID, 1, new byte[] { 0x55 });
            new Thread(() => { 
            
                bool success = SendResetCommand();

                if (success)
                {
                    txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText("Reset ACK'd" + Environment.NewLine)));
                }
                else
                    txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText("Reset Not ACK'd!" + Environment.NewLine)));

            }).Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(binData == null)
            {
                binData = CANMessage.arr_SensorPackage_8000_bin;
            }
            
            if(binData.Length > 0)
            {
                Thread newThread = new Thread(() => doBinTransfer(binData));
                newThread.Start();
            }

        }

        private byte[] uint32_to_bytes(UInt32[] binByteArray)
        {
            byte[] newAr = new byte[binByteArray.Length * 4];
            for (int i = 0; i < binByteArray.Length; i++)
            {
                newAr[i*4] = (byte) binByteArray[i];
                newAr[i*4+1] = (byte)(binByteArray[i]>>8);
                newAr[i*4+2] = (byte)(binByteArray[i]>>16);
                newAr[i*4+3] = (byte)(binByteArray[i]>>24);


            }
            return newAr;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            txTextBox.Invoke((MethodInvoker)(() => txBox.Clear()));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            rxTextBox.Invoke((MethodInvoker)(() => rxBox.Clear()));
        }


        private void button15_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                mreDict[0x44] = new ManualResetEvent(false);
                pcanSend(microcontrollerID, 1, new byte[] { 0x44 });

                bool timeout = mreDict[0x44].WaitOne(2000);

                if (timeout)
                {
                    txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText("Message Success" + Environment.NewLine)));
                }
                else
                    txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText("Message Failure" + Environment.NewLine)));
            }).Start();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            new Thread(() => { sendJumpCommand(); }).Start();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            LOG("Testing " + 10000 + " transfers..." + Environment.NewLine);

            new Thread(() =>
           {
               TestTransfers(10000);
           }).Start();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            new Thread(() => { transferTimeoutTest(); }).Start();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            new Thread(() => {

                sendJumpToBootloaderCommand();

            }).Start();
        }


        //ALL CODE TO TRANSLATE TO C IS AFTER THIS POINT


        public UInt16 microcontrollerID = 0x101;
        private bool sendJumpToBootloaderCommand()
        {
            LOG("Sending JUMP2BOOT CMD" + Environment.NewLine);

            CANMessage jumpCommand = new CANMessage(microcontrollerID, 2, new byte[] { 0x51, 0 });
            CANWrite(jumpCommand);

            CANMessage receivedMessage;
            bool success = CheckForACK(1000, 0x51, out receivedMessage);


            if (success)
            {
                LOG("Jump CMD ACK'd!" + Environment.NewLine);
                return true;
            }
            else
            {
                LOG("Jump NOT ACK'd!" + Environment.NewLine);
                return false;
            }
        }

        private bool transferTimeoutTest()
        {
            LOG("Transfer Timeout Test Started!" + Environment.NewLine);
            UInt16 numBytes = 0x800;
            UInt16 numWords = (UInt16)(numBytes / 4);
            byte numWordsLower = (byte)numWords;
            byte numWordsUpper = (byte)(numWords >> 8);

            UInt32 startAddress = 0x08008000;

            UInt32[] transferData = new UInt32[512];
            for (UInt32 i = 0; i < 512; i++)
                transferData[i] = i;

            UInt32 memoryOffset = 0;

            byte[] initiateTransferData = new byte[] {0x11,
            numWordsLower, numWordsUpper, 0, (byte) startAddress, (byte) (startAddress>>8),
            (byte) (startAddress>>16), (byte) (startAddress>>24) };


            //send transfer initiation command
            CANMessage transferInitiationCommand = new CANMessage(microcontrollerID, 8, initiateTransferData);
            CANWrite(transferInitiationCommand);

            //now wait for ACK (or NACK)
            CANMessage receivedMessage;
            bool success = CheckForACK(100, 0x11, out receivedMessage);

            if (success)
            {


                //now send the data 32 bits at a time
                byte[] byteAr = new byte[8];
                for (int i = 0; i < numWords; i++)
                {

                    UInt32 offset = (UInt32)(memoryOffset + i * 4);

                    byteAr[0] = 0x12;
                    byteAr[1] = (byte)(offset);
                    byteAr[2] = (byte)(offset >> 8);
                    byteAr[3] = (byte)(offset >> 16);
                    byteAr[4] = (byte)(transferData[i]);
                    byteAr[5] = (byte)(transferData[i] >> 8);
                    byteAr[6] = (byte)(transferData[i] >> 16);
                    byteAr[7] = (byte)(transferData[i] >> 24);

                    CANMessage transferWordMessage = new CANMessage(microcontrollerID, 8, byteAr);
                    CANWrite(transferWordMessage);
                    Thread.Sleep(1); // this will cause transfer to timeout near the end of the transfer

                }


                LOG("Finished Sending, Waiting For ACK..." + Environment.NewLine);

                bool success2 = CheckForACK(250, 0x12, out receivedMessage);
                if (success2)
                {
                    LOG("TRANSFER SUCCESSFUL");
                    return true;
                }
                else
                {
                    LOG("TRANSFER TIMEOUT");
                    return false;
                }

            }
            else
                return false;
        }

        //what happens when other types of packets are sent to the micro
        private void transferOtherPacketsTest()
        {
            
            
        }

        private bool TestTransfers(int numTransfers)
        {
            int failCount = 0;
            for (int i = 0; i < numTransfers; i++)
            {
                if (!doBinTransfer(CANMessage.arr_SensorPackage_22000_NEWEST_bin))
                    failCount++;
            }

            if (failCount > 0)
                return false;
            else
                return true;
        }

        unsafe UInt32 CRC32C(UInt32[] message, UInt32 length)
        {
            int i, j;
            long bite, crc;
            long mask;

            i = 0;
            crc = 0xFFFFFFFF;
            while (i < length)
            {
                bite = message[i];            // Get next byte.
                crc = crc ^ bite;
                for (j = 7; j >= 0; j--)
                {    // Do eight times.
                    mask = -((long)(crc & 1L));
                    crc = (crc >> 1) ^ (0xEDB88320 & mask);
                }
                i = i + 1;
            }
            return (UInt32)~crc;
        }

        private bool SendResetCommand()
        {
            CANMessage resetCommand = new CANMessage(microcontrollerID, 1, new byte[] { 0xC0 });
            CANWrite(resetCommand);

            CANMessage receivedMessage;
            return CheckForACK(250, 0xC0, out receivedMessage);
        }

        private void LOG(string text)
        {
            txTextBox.Invoke((MethodInvoker)(() => txBox.AppendText(text)));
        }
        

        private int GetTimeMilliseconds()
        {
            return Environment.TickCount ;
        }

        private bool CheckForACK(int timeoutMillis, byte commandByte)
        {
            CANMessage temp;
            return CheckForACK(timeoutMillis, commandByte, out temp);
        }
        private bool CheckForACK(int timeoutMillis, byte commandByte, out CANMessage receivedMessage)
        {
            int timeoutTime = GetTimeMilliseconds();
           
            while (GetTimeMilliseconds() - timeoutTime < timeoutMillis)
            {
                if (CANRead(out receivedMessage))
                {
                    if (receivedMessage.ID == microcontrollerID)
                    {
                        if (receivedMessage.DLC > 1)
                        {
                            if (receivedMessage.Data[0] == commandByte)
                            {
                                if (receivedMessage.Data[1] == 0xAA)
                                {
                                    return true;
                                }
                                else if (receivedMessage.Data[1] == 0xEE)
                                {
                                    return false;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                OSDelay(1);
            }
            receivedMessage = null;
            return false;
        }

        
        private bool isCANMessageAvailable()
        {
            if (CANQueue.IsEmpty)
                return false;
            else
                return true;
        }


        private bool CANRead(out CANMessage msg)
        {
            return CANQueue.TryDequeue(out msg);
        }

        private void CANWrite(CANMessage msg)
        {
            pcanSend(msg);
        }
        
        private void OSDelay(uint milliseconds)
        {
            Thread.Sleep((int)milliseconds);
        }


        
        

        private bool sendJumpCommand()
        {
            UInt32 startAddress = 0x08008000;
            LOG("Sending JUMP CMD" + Environment.NewLine);

            
            CANMessage msg = new CANMessage(microcontrollerID, 8, new byte[] { 0xC7, 0, 0, 0, (byte)(startAddress ), (byte)(startAddress >> 8), (byte)(startAddress  >> 16), (byte)(startAddress >> 24) });
            CANWrite(msg);

            CANMessage receivedMessage;
            bool success = CheckForACK(1000, 0xC7, out receivedMessage);


            if (success)
            {
                LOG("Jump CMD ACK'd!" + Environment.NewLine);
            }
            else
            {
                LOG("Jump NOT ACK'd!" + Environment.NewLine);
            }
            return success;
        }

        private bool doChunkTransfer(UInt32[] transferData, UInt32 memoryOffset)
        {
            //2kb transfer
            byte transferInitiationCommandByte = 0xC1;
     
            if (transferData.Length != 512)
            {
                throw new Exception("CHUNK SIZE MUST BE 2kB WHICH MEANS ARRAY MUST HAVE LEN=512");
            }

            //first, send a message which initiates the transfer and tells the micro how many kB to expect
            LOG("Initiating Transfer..." + Environment.NewLine);

            UInt16 numBytes = 0x800;
            UInt16 numWords = (UInt16)(numBytes / 4);
            byte numWordsLower = (byte)numWords;
            byte numWordsUpper = (byte)(numWords >> 8);

            UInt32 startAddress = 0x08008000;

            byte[] initiateTransferData = new byte[] {transferInitiationCommandByte,
            numWordsLower, numWordsUpper, 0, (byte) startAddress, (byte) (startAddress>>8),
            (byte) (startAddress>>16), (byte) (startAddress>>24) };

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch stopwatch2 = new System.Diagnostics.Stopwatch();

            //send transfer initiation command
            CANMessage transferInitiationCommand = new CANMessage(microcontrollerID, 8, initiateTransferData);
            CANWrite(transferInitiationCommand);
            stopwatch.Restart();
            stopwatch2.Restart();

            //now wait for ACK (or NACK)
            CANMessage receivedMessage;
            bool success = CheckForACK(250, 0xC1, out receivedMessage);

            if (success)
            {
                stopwatch.Stop();

                //LOG("Transfer Initiated " + stopwatch.ElapsedMilliseconds + "mS" + Environment.NewLine);

                stopwatch.Restart();
                //now send the data 32 bits at a time
                byte[] byteAr = new byte[8];
                for (int i = 0; i < numWords; i++)
                {

                    UInt32 offset = (UInt32)(memoryOffset + i * 4);

                    byteAr[0] = 0xC2;
                    byteAr[1] = (byte)(offset);
                    byteAr[2] = (byte)(offset >> 8);
                    byteAr[3] = (byte)(offset >> 16);
                    byteAr[4] = (byte)(transferData[i]);
                    byteAr[5] = (byte)(transferData[i] >> 8);
                    byteAr[6] = (byte)(transferData[i] >> 16);
                    byteAr[7] = (byte)(transferData[i] >> 24);

                    CANMessage transferWordMessage = new CANMessage(microcontrollerID, 8, byteAr);
                    CANWrite(transferWordMessage);

                }

                stopwatch.Stop();
                LOG("Finished Sending, Waiting For ACK..." + stopwatch.ElapsedMilliseconds + " ms" + Environment.NewLine);
                stopwatch.Restart();
                bool success2 = CheckForACK(500, 0xC2, out receivedMessage);
                if (success2)
                {
                    stopwatch.Stop();
                    LOG("Received Transfer ACK!" + stopwatch.ElapsedMilliseconds + "mS" + Environment.NewLine);
                    

                    UInt32 checksum = CRC32C(transferData, (uint)transferData.Length);
                    byte[] checksumAr = new byte[8] { 0xC3, 0, 0, 0, (byte)(checksum & 0x00FF), (byte)((checksum & 0xFF00) >> 8), (byte)((checksum & 0x00FF0000) >> 16), (byte)(checksum >> 24) };
                    CANMessage checksumMessage = new CANMessage(microcontrollerID, 8, checksumAr);
                    CANWrite(checksumMessage);
                    stopwatch.Restart();

                    bool success3 = CheckForACK(100, 0xC3, out receivedMessage);
                    if (success3)
                    {
                        stopwatch.Stop();

                        LOG("Chunk Checksum Passed! " + stopwatch.ElapsedMilliseconds + "ms" + Environment.NewLine);

                        stopwatch.Restart();
                        bool success4 = CheckForACK(100, 0xC4, out receivedMessage);
                        if (success4)
                        {
                            stopwatch.Stop();
                            LOG("Flash Write Complete! " + + stopwatch.ElapsedMilliseconds + "ms" + Environment.NewLine);
                            stopwatch.Restart();
                            bool success5 = CheckForACK(100, 0xC5, out receivedMessage);
                            if (success5)
                            {
                                stopwatch.Stop();
                                stopwatch2.Stop();
                                LOG("Flash Verify Complete! " + stopwatch.ElapsedMilliseconds + " ms" + Environment.NewLine);
                                LOG("Duration: " + stopwatch.ElapsedMilliseconds + " ms" + Environment.NewLine);
                                return true;
                                
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        LOG("Checksum Failed." + Environment.NewLine);
                        return false;
                    }
                }
                else
                {
                    LOG("Transfer Not ACK'd.." + Environment.NewLine);
                    return false;
                }
            }
            else
            {
                LOG("Transfer Failed To Initiate." + Environment.NewLine);
                return false;
            }
        }

        
        private bool doBinTransfer(UInt32[] binArray)
        {
            UInt32 numberOfWords = (UInt32)binArray.Length;

            //all transfers are broken up into an integer number of 2kB chunks, corresponding to
            //the stm32l451's 2kB flash page size. 
            UInt16 numberOfChunks = (UInt16)(numberOfWords / 512);
            if (numberOfWords % 512 != 0)  //we're going to have a partial page
                numberOfChunks++;

            //this expanded array will fit an integer number of chunks
            UInt32[] expandedArray = new uint[numberOfChunks * 512];

            LOG("Starting BIN xfer.." + Environment.NewLine);
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            
            //behold the chunk iterator
            for (int i = 0; i < numberOfChunks; i++)
            {
                stopwatch.Restart();
                UInt32[] tempArray = new UInt32[512];
                for (int j = 0; j < 512; j++)
                {

                    //if our bin isn't a multiple of 2kB long, add 0xFF to the end
                    if (binArray.Length <= (i * 512 + j))
                        tempArray[j] = 0xFF;
                    else
                        tempArray[j] = binArray[i * 512 + j];

                    expandedArray[i * 512 + j] = tempArray[j];
                }


                bool success = doChunkTransfer(tempArray, (UInt32)(i * 2048));
                stopwatch.Stop();
                LOG(stopwatch.ElapsedMilliseconds + "ms");

                if (success)
                {
                    //chunk completed successfully
                }
                else
                {
                    //unsuccessful chunk.  break out of this transfer. the SP will now be in standby mode
                    LOG("Bin Xfer Failed!" + Environment.NewLine);
                    return false;
                }
            }


            //now we need to compute the full CRC and send a 0x16 transfer verification message
            UInt32 fullCRC = CRC32C(expandedArray, (UInt32)expandedArray.Length);

            byte[] checksumAr = new byte[8] { 0xC6, ((byte) numberOfChunks), (byte)(numberOfChunks>>8), 0, (byte)(fullCRC & 0x00FF),
                (byte)((fullCRC & 0xFF00) >> 8), (byte)((fullCRC & 0x00FF0000) >> 16), (byte)(fullCRC >> 24) };
            
            CANMessage checksumMessage = new CANMessage(microcontrollerID, 8, checksumAr);
            CANWrite(checksumMessage);

            CANMessage receivedMessage;
            bool checksumSuccess = CheckForACK(250, 0xC6, out receivedMessage);

            if (checksumSuccess)
            {
                LOG("Bin Xfer Complete!" + Environment.NewLine);
                return true;
            }
            else
            {
                LOG("Bin Xfer Checksum Failed!" + Environment.NewLine);
                return false;
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //sp reset message
            new Thread(() => {



                bool success = SendSPResetCommand();

                if (success)
                {
                    LOG("ACK Received");
                }
                else
                    LOG("No ACK Received");
            }).Start();
        }

        private bool SendSPResetCommand()
        {
            CANMessage SPResetMsg = new CANMessage(microcontrollerID, 1, new byte[] { 0x8 });
            CANWrite(SPResetMsg);


            return true;
        }
        private void button19_Click(object sender, EventArgs e)
        {
            //infinite loop command
            new Thread(() => {
                CANMessage SPResetMsg = new CANMessage(microcontrollerID, 1, new byte[] { 0x52 });
                CANWrite(SPResetMsg);


                bool success = CheckForACK(250, 0x52, out SPResetMsg);

                if (success)
                {
                    LOG("ACK Received");
                }
                else
                    LOG("No ACK Received");
            }).Start();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                LOG("Beginning upload test");
                int numberOfFailures = 0;
                for (int i = 0; i < 50; i++)
                {

                    bool res = SendResetCommand();

                    if (!res)
                    {
                        LOG("RESET ERROR!!!");
                        Thread.Sleep(200);
                        res = SendResetCommand();
                        numberOfFailures++;
                    }

                    bool result = doBinTransfer(CANMessage.arr_SensorPackage_22000_NEWEST_bin);

                    if(result)
                    {
                    }
                    else
                    {
                        LOG("TRANSFER ERROR!!!");
                        numberOfFailures++;
                        //doBinTransfer(CANMessage.arr_SensorPackage_22000_NEWEST_bin);
                    }

                    res = sendJumpCommand();

                    if(!res)
                    {
                       // Thread.Sleep(3000);
                      //  res = sendJumpCommand();
                        LOG("JUMP ERROR!!!");
                        numberOfFailures++;
                    }

                    Thread.Sleep(4000);

                    res = SendSPResetCommand();

                    if (!res)
                    {
                        //Thread.Sleep(1000);
                        //res = SendSPResetCommand();
                        //numberOfFailures++;
                        //LOG("SP RESET ERROR!!!");

                    }

                    Thread.Sleep(2000);
                    CANMessage SPResetMsg = new CANMessage(microcontrollerID, 1, new byte[] { 0xC8 });
                    CANWrite(SPResetMsg);


                    //bool success = CheckForACK(250, 0xC8, out SPResetMsg);


                }

                LOG("Test finished with " + numberOfFailures + " errors.");
            }).Start();
        }

        private void button21_Click(object sender, EventArgs e)
        {
            //infinite loop command
            new Thread(() => {
                CANMessage SPResetMsg = new CANMessage(microcontrollerID, 1, new byte[] { 0xC8 });
                CANWrite(SPResetMsg);


                bool success = CheckForACK(250, 0xC8, out SPResetMsg);

                if (success)
                {
                    LOG("ACK Received");
                }
                else
                    LOG("No ACK Received");
            }).Start();
        }
    }
}
 