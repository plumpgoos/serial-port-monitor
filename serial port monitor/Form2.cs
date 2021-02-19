using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace serial_port_monitor
{
    public partial class Form2 : Form
    {
        public Form1 mainform;

        public Form2(Form1 main)
        {
            InitializeComponent();
            mainform = main;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
