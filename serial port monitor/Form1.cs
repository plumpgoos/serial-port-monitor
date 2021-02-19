using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
namespace serial_port_monitor
{
    
    public partial class Form1 : Form
    {

        public static int MILLISECONDS = 1000;

        public Single offset;
        public Single tare;
        public Single last;
        public Single outval;
        public Single circ;
        public Single meters;
        public SerialPort readport;
        public SerialPort writeport;
        public int mode;
        public bool volt;
        public List<string> ports;

        public int hardcount;

        private System.Windows.Forms.Timer timer;
        public System.Windows.Forms.Timer flash;
        public System.Windows.Forms.Timer myTimer;

        public Form1()
        {


            InitializeComponent();
            this.FormClosing += formClose;

            label8.Text = "";
            numericUpDown3.Controls[0].Visible = false;

            this.tare = 0;
            last = 0;
            volt = false;
            hardcount = 0;

            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = MILLISECONDS; // in miliseconds
            timer.Start();

            //flash = new Timer();
            //flash.Tick += new EventHandler(flash_Tick);
            //flash.Interval = 2000; // in miliseconds

            ports = SerialPort.GetPortNames().ToList<string>();
            this.comboBox1.Items.AddRange(ports.ToArray());
            this.comboBox2.Items.AddRange(ports.ToArray());
            for (int i = 0; i < ports.Count; i++)
            {
                Console.WriteLine(ports[i]);
            }
        }

        private void formClose(object sender, EventArgs e)
        {
            //if (readport.IsOpen)
            //{
            //    readport.Close();
            //}
            //if (writeport.IsOpen)
            //{
            //    writeport.Close();
            //}
            Form2 portdialog = new Form2(this);
            portdialog.Show();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            DataSendHandler(outval);
        }

        private void flash_Tick(object sender, EventArgs e)
        {
            label8.Text = "";
            flash.Stop();
        }

        private SerialPort openPort(string port, SerialPort serialport)
        {
            if (serialport != null && serialport.IsOpen)
            {
                serialport.Close();
            }

            serialport = new SerialPort(port);

            serialport.BaudRate = 9600;
            serialport.Parity = Parity.None;
            serialport.StopBits = StopBits.One;
            serialport.DataBits = 8;
            serialport.Handshake = Handshake.None;
            serialport.RtsEnable = true;

            try
            {
                serialport.Open();
            }
            catch
            {
                return null;
            }

            //serialport.Open();
            Console.WriteLine("port opened: " + port);

            return serialport;
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            
            SerialPort sp = (SerialPort)sender;

            string indata = sp.ReadLine();

            Console.WriteLine(indata);

            

            //if (indata[0] == 'V')
            //{
            //    volt = true;
            //}
            //else if (volt)
            //{
            //    Console.WriteLine(indata);
            //    SetText3(indata);
            //    volt = false;
            //}
            //else
            //{
            //    SetText1(indata);
            //    SetText2(indata);
            //}

            int split = indata.IndexOf('V');
            if (split != -1)
            {
                string count = indata.Substring(0, split);

                int end = indata.IndexOf('\r');
                string voldat = end != -1 ? indata.Substring(split + 1, end - (split + 1)) : indata.Substring(split+1);

                SetText1(count);
                SetText2(count);
                SetText3(voldat);
                if (Single.Parse(count) != 0)
                {
                    SetText4("");
                }
                    
            } else
            {
                int end = indata.IndexOf('\r');
                SetText4(end != -1 ? indata.Substring(0,end) : indata);
                //flash = new Timer();
                //flash.Tick += new EventHandler(flash_Tick);
                //flash.Interval = 2000; // in miliseconds
                //flash.Start();
            }
        }
        
        private void DataSendHandler(Single value)
        {
            if (mode != 0 && writeport != null && writeport.IsOpen)
            {

                String f_text = null;

                if (mode == 1)
                {
                    f_text = strOut(value);
                }
                else
                {
                    f_text = mcOut(value);
                }

                writeport.Write(f_text);
                Console.WriteLine("output to writeport: " + f_text);
            }
        }

        private string strOut(Single value)
        {
            string f = String.Format("{0:+000#;-000#;+0000;}\r\n", value);
            Console.WriteLine(f);
            return f;
        }

        private string mcOut(Single value)
        {
            value = Math.Abs(value);
            string f = String.Format("L={0:0000.00}\r\n", value);
            Console.WriteLine(f);
            return f;
        }

        delegate void SetTextCallback1(string text);
        delegate void SetTextCallback2(string text);
        delegate void SetTextCallback3(string text);
        delegate void SetTextCallback4(string text);
        delegate void SetTextCallback5();
        delegate void SetTextCallback6();

        //raw count
        private void SetText1(string text)
        {

            //last = Single.Parse(text);
            last = Single.Parse(text);

            Console.WriteLine("raw: " + last);
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback1 d = new SetTextCallback1(SetText1);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox1.Text = last.ToString();
            }
        }

        //processed count
        private void SetText2(string text)
        {

            if (this.textBox2.InvokeRequired)
            {
                SetTextCallback2 d = new SetTextCallback2(SetText2);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                outval = Single.Parse(text) - tare + offset;
                this.textBox2.Text = Math.Round(outval,2).ToString();
                DataSendHandler(outval);
                finalSet();
            }
        }

        //voltage
        private void SetText3(string text)
        {
            Single voltage = Single.Parse(text);
            Console.WriteLine("voltage: " + voltage);
            voltage *= (((Single)5) / 1023);

            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox3.InvokeRequired)
            {
                SetTextCallback3 d = new SetTextCallback3(SetText3);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox3.Text = "V" + Math.Round(voltage,2);
            }
        }

        //flash
        private void SetText4(string text)
        {
            if (this.label8.InvokeRequired)
            {
                SetTextCallback4 d = new SetTextCallback4(SetText4);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.label8.Text = text;
            }
        }

        //meters
        private void SetText5()
        {
            if (this.textBox5.InvokeRequired)
            {
                SetTextCallback5 d = new SetTextCallback5(SetText5);
                this.Invoke(d, new object[] {  });
            }
            else
            {
                meters = (Single)Math.Round(circ * outval / 39.37, 3);
                this.textBox5.Text = Math.Round(circ*outval/39.37,3).ToString();
                int barval = (int)(meters * 1000);
                if (barval >= 0 && barval <= this.progressBar1.Maximum)
                this.progressBar1.Value = barval;
                Console.WriteLine("val: " + barval);
            }
        }

        //feet
        private void SetText6()
        {
            if (this.textBox6.InvokeRequired)
            {
                SetTextCallback5 d = new SetTextCallback5(SetText6);
                this.Invoke(d, new object[] { });
            }
            else
            {
                this.textBox6.Text = Math.Round(circ * outval / 12, 3).ToString();
            }
        }

        //readport
        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            //ComboBox box = (ComboBox)sender;
            //readport = openPort(box.SelectedItem.ToString(), readport);
            //if (readport == null)
            //{
            //    box.SelectedItem = null;
            //} else
            //{
            //    readport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            //}
        }

        //writeport
        private void comboBox2_SelectionChangeCommitted(object sender, EventArgs e)
        {
            //ComboBox box = (ComboBox)sender;
            //writeport = openPort(box.SelectedItem.ToString(), writeport);
            //if (writeport == null)
            //{
            //    box.SelectedItem = null;
            //}
        }

        //output format
        private void radioButtons_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Checked)
            {
                // Only one radio button will be checked
                Console.WriteLine("Changed: " + rb.Name);
                if (rb.Name.Equals("radio_none"))
                {
                    mode = 0;
                }
                else if (rb.Name.Equals("radio_str"))
                {
                    mode = 1;
                }
                else
                {
                    mode = 2;
                }
            }
        }

        //offset
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            offset = (Single)ud.Value;
            SetText1(last.ToString());
            SetText2(last.ToString());
        }

        //tare
        private void tare_Reset(object sender, EventArgs e)
        {
            tare = last;
            this.label1.Text = "Zero: " + tare;
            SetText1(last.ToString());
            SetText2(last.ToString());
            Console.WriteLine("RESET");
        }


        //hardware
        private void button2_Click(object sender, EventArgs e)
        {
            if (readport != null && readport.IsOpen)
            {

                myTimer = new System.Windows.Forms.Timer();
                myTimer.Tick += new EventHandler(TimerEventProcessor);
                myTimer.Interval = 1000;
                myTimer.Start();
                //readport.Write("R\r\n");
                //readport.Write("R\r\n");
                //readport.Write("R\r\n");
                //readport.Write("R\r\n");

            }
        }

        private void TimerEventProcessor(object sender, EventArgs e)
        {
            hardcount++;
            //readport.Write("R");
            for (int i = 0; i < 5; i++)
            {
                readport.Write("R");
                Console.WriteLine("R");
                Thread.Sleep(20);
            }

            Console.WriteLine("hardcount: " + hardcount);
            if (hardcount >= 3)
            {
                hardcount = 0;
                readport.Write("R\r\n");
                myTimer.Stop();
            }
        }

        //software
        private void button3_Click(object sender, EventArgs e)
        {
            if (readport != null && readport.IsOpen)
            {
                readport.Write("ZZZZ\r\n");
            }
        }

        private void connect1_Click(object sender, EventArgs e)
        {

            
            if (readport == null || !readport.IsOpen)
            {
                //comboBox1.SelectedItem = null;
                readport = openPort(comboBox1.SelectedItem.ToString(), readport);
                if (readport != null)
                {
                    connect1.Text = "Disconnect";
                    readport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                }
            } else
            {
                readport.Close();
                Console.WriteLine("port closed: " + readport.PortName);
                readport = null;
                connect1.Text = "Connect";
            }
            
        }

        private void connect2_Click(object sender, EventArgs e)
        {
            if (writeport == null || !writeport.IsOpen)
            {
                //comboBox1.SelectedItem = null;
                writeport = openPort(comboBox2.SelectedItem.ToString(), writeport);
                if (writeport != null)
                {
                    connect2.Text = "Disconnect";
                }
            }
            else
            {
                writeport.Close();
                Console.WriteLine("port closed: " + writeport.PortName);
                writeport = null;
                connect2.Text = "Connect";
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            circ = (Single)ud.Value;
            finalSet();
        }

        private void finalSet()
        {
            SetText5();
            SetText6();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            int val = (int)ud.Value;
            this.progressBar1.Maximum = val * 1000;
            this.progressBar1.Value = this.progressBar1.Value;
            this.barmax.Text = (ud.Value).ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string[] tempport = SerialPort.GetPortNames();
            string curread = (string) this.comboBox1.SelectedItem;
            string wriread = (string) this.comboBox2.SelectedItem;
            this.comboBox1.Items.Clear();
            this.comboBox2.Items.Clear();

            //foreach (string s in tempport) {
            //    if (!ports.Contains(s))
            //    {
            //        ports.Add(s);
            //        this.comboBox1.Items.Add(s);
            //        this.comboBox2.Items.Add(s);
            //    }
            //}

            //this.comboBox1.Items.AddRange(ports);
            //this.comboBox2.Items.AddRange(ports);

            ports = SerialPort.GetPortNames().ToList<string>();
            this.comboBox1.Items.AddRange(ports.ToArray());
            this.comboBox2.Items.AddRange(ports.ToArray());

            if (ports.Contains(curread))
            {
                this.comboBox1.SelectedItem = curread;
            }
            if (ports.Contains(wriread))
            {
                this.comboBox2.SelectedItem = wriread;
            }
        }
    }
}
