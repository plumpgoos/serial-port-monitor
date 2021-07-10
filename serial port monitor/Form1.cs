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
using System.Media;
using System.IO;
using System.Xml.Serialization;

namespace serial_port_monitor
{
    
    public partial class Form1 : Form
    {

        public static int MILLISECONDS = 1000;

        public decimal offset;
        public decimal tare;
        public decimal raw;
        public decimal outval;
        public decimal circ;
        public decimal meters;
        public decimal feet;
        public decimal voltage;
        public long sheavenumval;
        public long maxcableval;
        public decimal lower;
        public decimal higher;
        public SerialPort readport;
        public SerialPort writeport;
        
        public int mode;
        public bool volt;
        public List<string> ports;

        public int hardcount;

        private System.Windows.Forms.Timer timer;
        public System.Windows.Forms.Timer flash;
        public System.Windows.Forms.Timer myTimer;
        public System.Windows.Forms.Timer logtimer;

        public StreamWriter sw;
        public bool logging;
        public FileInfo logfi;

        SoundPlayer alert;

        public Form1()
        {


            InitializeComponent();
            this.FormClosing += formClose;    

            label8.Text = "";
            //maxcable.Controls[0].Visible = false;
            //lowerud.Controls[0].Visible = false;
            //higherud.Controls[0].Visible = false;

            logging = false;

            raw = 0;
            volt = false;
            hardcount = 0;

            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = MILLISECONDS; // in miliseconds
            timer.Start();

            alert = new SoundPlayer(@"c:\Windows\Media\Windows Error.wav");
            lower = 0;
            higher = 0;
            myTimer = new System.Windows.Forms.Timer();
            myTimer.Interval = 5000;
            myTimer.Tick += (s, e) =>
            {
                label17.Hide();
                myTimer.Stop();
            };

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
            ReadSettings();
        }

        private void ReadSettings()
        {
            var settings = Properties.Settings.Default;

            lower = settings.lower;
            lowerud.Value = lower;

            higher = settings.higher;
            higherud.Value = higher;

            offset = settings.cableoffset;
            cableoffset.Value = offset;

            sheavenumval = settings.sheavenumber;
            sheavenumber.Text = sheavenumval.ToString();

            circ = settings.sheavecirc;
            sheavecirc.Value = circ;

            maxcableval = settings.maxcable;
            maxcable.Value = maxcableval;
            this.progressBar1.Maximum = (int)maxcableval;
            this.barmax.Text = maxcableval.ToString();

            mode = settings.format;
            switch (mode)
            {
                case 1:
                    radio_str.Checked = true;
                    break;
                case 2:
                    radio_mc.Checked = true;
                    break;
                default:
                    radio_none.Checked = true;
                    mode = 0;
                    break;
            }

            tare = settings.zero;
            this.label1.Text = "Zero: " + tare;

        }

        private void ReadSettings(Settings settings)
        {
            lower = settings.lower;
            lowerud.Value = lower;

            higher = settings.higher;
            higherud.Value = higher;

            offset = settings.cableoffset;
            cableoffset.Value = offset;

            sheavenumval = settings.sheavenumber;
            sheavenumber.Text = sheavenumval.ToString();

            circ = settings.sheavecirc;
            sheavecirc.Value = circ;

            maxcableval = settings.maxcable;
            maxcable.Value = maxcableval;
            this.progressBar1.Maximum = (int)maxcableval;
            this.barmax.Text = maxcableval.ToString();

            mode = settings.format;
            switch (mode)
            {
                case 1:
                    radio_str.Checked = true;
                    break;
                case 2:
                    radio_mc.Checked = true;
                    break;
                default:
                    radio_none.Checked = true;
                    mode = 0;
                    break;
            }

            tare = settings.zero;
            this.label1.Text = "Zero: " + tare;

        }

        private void WriteSettings()
        {
            var settings = Properties.Settings.Default;

            settings.lower = lower;
            settings.higher = higher;
            settings.cableoffset = offset;
            settings.sheavenumber = sheavenumval;
            settings.sheavecirc = circ;
            settings.maxcable = maxcableval;
            settings.format = mode;
            settings.zero = tare;

            settings.Save();
        }

        private void formClose(object sender, FormClosingEventArgs e)
        {
            //e.Cancel = true;
            //if (readport.IsOpen)
            //{
            //    readport.Close();
            //}
            //if (writeport.IsOpen)
            //{
            //    writeport.Close();
            //}
            //Form2 portdialog = new Form2(this);
            //portdialog.Show();
            WriteSettings();
            //e.Cancel = false;
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
            byte[] bytes = Encoding.UTF8.GetBytes(indata);
            string hexString = BitConverter.ToString(bytes);
            hexString = hexString.Replace("-", "");
            Console.WriteLine(hexString);

            if (indata.Equals("\r")) {
                Console.WriteLine("Empty input, skipping");
                return;
            }


            int split = indata.IndexOf('V');
            if (split != -1)
            {
                string count = indata.Substring(0, split);

                int end = indata.IndexOf('\r');
                string voldat = end != -1 ? indata.Substring(split + 1, end - (split + 1)) : indata.Substring(split+1);

                try
                {
                    decimal.Parse(count);
                    decimal.Parse(voldat);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse input: " + count + " " + voldat);
                    return;
                }

                decimal temp = decimal.Parse(count);
                raw = temp;
                outval = temp - tare;
                voltage = decimal.Parse(voldat) * (((decimal)5) / 1023);
                meters = (decimal)Math.Round((circ * outval / (decimal)39.37) + offset, 4);
                feet = (decimal)Math.Round(meters * (decimal)3.281, 4);

                    //string logline = String.Format("{0,-21} {1,-14} {2,-14}\n", DateTime.Now.ToString("s"), meters.ToString(), feet.ToString());
                LogDat();

                SetText1(count);
                SetText2(count);
                SetText3(voldat);
                if (decimal.Parse(count) != 0)
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
        
        private void LogDat()
        {
            if (!logging)
            {
                return;
            }

            logfi.Refresh();
            Console.WriteLine(logfi.Length);
            if (logfi.Length > 1000000)
            {
                LogStop();
                LogStart();
            }

            string logmode;
            switch (mode)
            {
                case 1:
                    logmode = "STR";
                    break;
                case 2:
                    logmode = "MC";
                    break;
                default:
                    logmode = "NONE";
                    break;
            }
            
            string logline = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}\n\n",
                DateTime.Now.ToString("s"), meters.ToString(), feet.ToString(), raw.ToString(), outval.ToString(), voltage.ToString(),
                offset.ToString(), logmode, sheavenumval.ToString(), circ.ToString(), maxcableval.ToString(), lower.ToString(), higher.ToString(), tare.ToString());
            sw.Write(logline);
            sw.Flush();
        }

        private void DataSendHandler(decimal value)
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

        private string strOut(decimal value)
        {
            string f = String.Format("{0:+000#;-000#;+0000;}\r\n", value);
            Console.WriteLine(f);
            return f;
        }

        private string mcOut(decimal value)
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

            //raw = decimal.Parse(text);
            raw = decimal.Parse(text);

            Console.WriteLine("raw: " + raw);
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
                this.textBox1.Text = raw.ToString();
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
                outval = decimal.Parse(text) - tare;
                this.textBox2.Text = Math.Round(outval,4).ToString();
                DataSendHandler(outval);
                finalSet();
            }
        }

        //voltage
        private void SetText3(string text)
        {
            decimal temp = decimal.Parse(text);
            Console.WriteLine("voltage: " + temp);
            voltage = temp * (((decimal)5) / 1023);

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
            if (this.finalmeter.InvokeRequired)
            {
                SetTextCallback5 d = new SetTextCallback5(SetText5);
                this.Invoke(d, new object[] {  });
            }
            else
            {
                meters = (decimal)Math.Round((circ * outval / (decimal)39.37) + offset, 4);
                feet = (decimal)Math.Round(meters * (decimal)3.281, 4);

                SetText6();

                this.finalmeter.Text = Math.Round((circ*outval/ (decimal)39.37)+offset,4).ToString();
                int barval = (int)(meters);
                if (barval >= 0 && barval <= this.progressBar1.Maximum)
                this.progressBar1.Value = barval;
                Console.WriteLine("val: " + barval);

                if (meters <= lower+15 && meters >= lower-15)
                {
                    myTimer.Stop();
                    label17.Text = "Approaching Lower Gate";
                    label17.Show();
                    alert.Play();
                    
                    myTimer.Start();
                }
                else if (meters <= higher && meters >= higher - 15)
                {
                    myTimer.Stop();
                    label17.Text = "Approaching Higher Gate";
                    label17.Show();
                    alert.Play();
                    
                    myTimer.Start();
                } else if (meters >= higher)
                {
                    myTimer.Stop();
                    label17.Text = "Exceeded Higher Gate";
                    label17.Show();
                    alert.Play();
                }
            }
        }

        //feet
        private void SetText6()
        {
            if (this.finalfeet.InvokeRequired)
            {
                SetTextCallback5 d = new SetTextCallback5(SetText6);
                this.Invoke(d, new object[] { });
            }
            else
            {
                this.finalfeet.Text = Math.Round(meters * (decimal)3.281, 4).ToString();
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
            offset = (decimal)ud.Value;
            SetText1(raw.ToString());
            SetText2(raw.ToString());
        }

        //tare
        private void tare_Reset(object sender, EventArgs e)
        {
            tare = raw;
            this.label1.Text = "Zero: " + tare;
            SetText1(raw.ToString());
            SetText2(raw.ToString());
            Console.WriteLine("RESET");
        }


        //hardware
        private void button2_Click(object sender, EventArgs e)
        {
            if (readport != null && readport.IsOpen)
            {

                //myTimer = new System.Windows.Forms.Timer();
                //myTimer.Tick += new EventHandler(TimerEventProcessor);
                //myTimer.Interval = 1000;
                //myTimer.Start();
                readport.Write("RRRRRRRRRR\r\n");
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
                Console.WriteLine("RESET COUNT1");
                readport.Write("ZZZZZZZZZZ\r\n");
                Console.WriteLine("RESET COUNT2");
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
            circ = (decimal)ud.Value;
            finalSet();
        }

        private void finalSet()
        {
            Console.WriteLine("FINAL SET");
            SetText5();
            //SetText6();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            int val = (int)ud.Value;
            maxcableval = val;
            this.progressBar1.Maximum = val;
            //this.progressBar1.Value = this.progressBar1.Value;
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

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            lower = (decimal)ud.Value;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)sender;
            higher = (decimal)ud.Value;
        }

        private void sheavenumber_TextChanged(object sender, EventArgs e)
        {
            sheavenumval = long.Parse(((TextBox)sender).Text);
        }

        private void save_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "XML File|*.xml";
            saveFileDialog1.Title = "Save Settings";
            saveFileDialog1.ShowDialog();

            Settings tosave = new Settings();
            tosave.lower = lower;
            tosave.higher = higher;
            tosave.cableoffset = offset;
            tosave.sheavecirc = circ;
            tosave.zero = tare;
            tosave.sheavenumber = sheavenumval;
            tosave.maxcable = maxcableval;
            tosave.format = mode;

            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            using (FileStream fs = (FileStream)saveFileDialog1.OpenFile())
            {
                xs.Serialize(fs, tosave);
            }
        }


        private void load_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ".\\";
            openFileDialog.Filter = "XML File|*.xml";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            Settings settings;
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            if (openFileDialog.FileName != null && openFileDialog.FileName != "")
            {
                using (FileStream fs = (FileStream)openFileDialog.OpenFile())
                {
                    Console.WriteLine("FS: " + fs == null);
                    settings = xs.Deserialize(fs) as Settings;
                }

                ReadSettings(settings);
            }
        }

        public class Settings
        {
            public decimal lower;
            public decimal higher;
            public decimal cableoffset;
            public decimal sheavecirc;
            public decimal zero;
            public long sheavenumber;
            public long maxcable;
            public int format;
        }

        private void logtick(object sender, EventArgs e)
        {
            LogDat();
        }

        private void logstart_Click(object sender, EventArgs e)
        {
            if (!logging)
            {
                LogStart();
                logstart.Enabled = false;
                logstop.Enabled = true;
                Console.WriteLine("Logging started");
            }
        }

        private void LogStart()
        {
            logging = true;
            string logname = "log_" + DateTime.Now.ToString("s").Replace(':', '-') + ".txt";
            Console.WriteLine(logname);
            if (!File.Exists(logname))
                File.Create(logname).Close(); // Create file

            logfi = new FileInfo(logname);

            sw = File.AppendText(logname);
            string loghead = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}\n\n",
                "Time", "Meters", "Feet", "Raw", "Count", "Volt", "Cable Offset", "Output Format", "Sheave Block Number",
                "Sheave Circumference", "Max Cable Length", "Lower Gate", "Higher Gate", "Zero");
            sw.Write(loghead);
            sw.Flush();

            LogDat();
            logtimer = new System.Windows.Forms.Timer();
            logtimer.Tick += new EventHandler(logtick);
            logtimer.Interval = (int)(loginterval.Value * 60000); // in miliseconds
            logtimer.Start();
        }

        private void logstop_Click(object sender, EventArgs e)
        {
            if (logging)
            {
                LogStop();
                logstart.Enabled = true;
                logstop.Enabled = false;
                Console.WriteLine("Logging Stopped");
            }
        }

        private void LogStop()
        {
            sw.Dispose();
            logtimer.Stop();
            logging = false;
        }

        private void numericUpDown1_ValueChanged_1(object sender, EventArgs e)
        {
            if (logtimer != null)
            {
                LogDat();
                logtimer.Interval = (int)(((NumericUpDown)sender).Value*60000);
            }
        }
    }
}
