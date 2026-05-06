using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        decimal temperatuur = 0;
        decimal temperatuur2 = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;

                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");
            }
            catch (Exception)
            { }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch (Exception)
            {
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPortArduino.IsOpen)
                {
                    //ik heb een verbinding -> de gebruiker wil deze verbreken
                    serialPortArduino.Close();
                    radioButtonVerbonden.Checked = false;
                    buttonConnect.Text = "Connect";
                    labelStatus.Text = "Status: Disconnected";

                }
                else
                {
                    //ik heb geen verbinding -> de gebruiker wil een verbinding maken
                    serialPortArduino.PortName =(string) comboBoxPoort.SelectedItem;
                    serialPortArduino.BaudRate = Int32.Parse((string) comboBoxBaudrate.SelectedItem);
                    serialPortArduino.DataBits =(int) numericUpDownDatabits.Value;

                    if (radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                    else if (radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                    else if (radioButtonParityNone.Checked) serialPortArduino.Parity = Parity.None;
                    else if (radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                    else if (radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;

                    if (radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                    else if (radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                    else if (radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;

                    if (radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                    else if (radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                    else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                    else if (radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;

                    serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;
                    serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;

                    serialPortArduino.Open();
                    string commando = "ping";
                    serialPortArduino.WriteLine(commando);
                    string antwoord = serialPortArduino.ReadLine();
                    antwoord = antwoord.TrimEnd();
                    if (antwoord == "pong")
                    {
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = "Status: Connected";
                    }
                    else
                    {
                        serialPortArduino.Close();
                        labelStatus.Text = "Error: verkeerd antwoord";
                    }
                }
            }
            catch(Exception exception)
            {
                labelStatus.Text = "error:" + exception.Message;
                serialPortArduino.Close ();
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
            }

        }

        private void timerOefening5_Tick_1(object sender, EventArgs e)
        {
            try
            {
                string command;
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    
                    int analogvalue0 = LeesAnalogePin0(serialPortArduino);
                    int analogvalue1 = LeesAnalogePin1(serialPortArduino);
                    Decimal a = 40m / 1023m;

                    if (analogvalue0 != -1)
                    {
                        temperatuur = (analogvalue0 * a) + 5;
                        labelGewensteTemp.Text = Math.Round(temperatuur, 1) + " °C";
                    }
                    if (analogvalue1 != -1)
                    {
                        temperatuur2 = (analogvalue1 * a) + 5;
                        labelHuidigeTemp.Text = Math.Round(temperatuur2, 1) + " °C";
                    }
                    if (temperatuur>= temperatuur2)
                    {
                        command = "set d2 high";
                    }
                    else
                    {
                        command = "set d2 low";
                    }
                    serialPortArduino.WriteLine(command);
                }
            }
            catch (Exception exception)
            {
                labelStatus.Text = "error:" + exception.Message;
            }
        }

        private void tabControl_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string command;
            try
            {
                if (tabControl.SelectedTab == tabPageOefening5 && serialPortArduino.IsOpen)
                {
                    timerOefening5.Start();

                }
                else
                {
                    timerOefening5.Stop();
                    command = "set d2 low";
                    serialPortArduino.WriteLine(command);

                }
            }
            catch (Exception exception)
            {
                labelStatus.Text = "error:" + exception.Message;
            }
        }
        private int LeesAnalogePin0(SerialPort serialPort)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.WriteLine("get a0");

                    string response = serialPort.ReadLine().Trim();

                    if (response.StartsWith("a0:"))
                    {
                        string waardeString = response.Split(':')[1].Trim();

                        if (int.TryParse(waardeString, out int waarde))
                        {
                            return waarde;
                        }
                    }
                }
            }
            catch
            {
            }

            return -1;
        }
        private int LeesAnalogePin1(SerialPort serialPort)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.WriteLine("get a1");

                    string response = serialPort.ReadLine().Trim();

                    if (response.StartsWith("a1:"))
                    {
                        string waardeString = response.Split(':')[1].Trim();

                        if (int.TryParse(waardeString, out int waarde))
                        {
                            return waarde;
                        }
                    }
                }
            }
            catch
            {
            }

            return -1;
        }
    }
}
