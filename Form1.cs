using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Proyecto1Vis
{
    public partial class Form1 : Form
    {
        
        public static SerialPort tserialport = new SerialPort();  //Puerto serial
        Dictionary<string, string> Tvals = new Dictionary<string, string>(); //Diccionario donde se almacenarán los valores

        //Contenedores de variables
        string currentval = "";
        string currentpwr = "";
        string currentpwm = "";
        string currentsp = "";
        string PID = "";
        string KP = "";
        string KD = "";
        string KI = "";

        //Contienen los puntos en los gráficos
        List<Cpoint> ChartDataList = new List<Cpoint>();
        List<Cpoint> ChartDataList2 = new List<Cpoint>();
        Queue<string> datalist = new Queue<string>();

        //Se ejecuta al abrir la ventana
        public Form1()
        {
            InitializeComponent();
            this.chart1.ChartAreas[0].AxisX.LabelStyle.Format = "ss";
            chart1.Series[0].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            this.chart2.ChartAreas[0].AxisX.LabelStyle.Format = "ss";
            chart2.ChartAreas[0].AxisY.Maximum = 110;
            chart2.Series[0].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
        }

        //Se ejecuta al cargar la ventana
        private void Form1_Load(object sender, EventArgs e)
        {
            string[] plist = System.IO.Ports.SerialPort.GetPortNames();
            PortBox.Items.AddRange(plist);
            UpdateValues();
        }

        //Huevo de pascua, al hacer doble click en el fondo de la ventana, cambia la imagen de fondo.
        private void Form1_dclick(object sender, EventArgs e)
        {
            if ((this.BackgroundImage == null))
            {
                this.BackgroundImage = global::Proyecto1Vis.Properties.Resources.pikachu;
            }
            else
            {
                this.BackgroundImage = null;
            }
        }

        //Subrutina que lanza un hilo que actualiza los valores constantemente
        private void UpdateValues()
        {
            //Expresión lambda que obtiene los valores desde el puerto serial y los almacena en una cola.
            Task.Run(() =>
            {
                while (true)
                {
                    if (tserialport.IsOpen)
                    {
                        string ttext = "";
                        try
                        {
                            ttext = tserialport.ReadLine();
                        }
                        catch (System.IO.IOException) { }
                        catch (System.UnauthorizedAccessException) { }

                        lock (datalist)
                        {
                            datalist.Enqueue(ttext);
                        }

                    }
                    System.Threading.Thread.Sleep(10); //Esperamos 10ms entre periodos de actualización para reducir el consumo de CPU.
                }                
            });

            //Expresión lambda que procesa los valores en la cola y los almacena en un diccionario.
            Task.Run(() =>
             {
                 while (true)
                 {
                     string ttext = "";
                     lock (datalist)
                     {
                         if (!(datalist.Count == 0)) { ttext = datalist.Dequeue(); }
                     }

                     if (ttext.Contains('='))
                     {
                         string valname = ttext.Split('=')[0].Trim();
                         string value = ttext.Split('=')[1].Trim();

                         if (!Tvals.ContainsKey(valname))
                         {
                             Tvals.Add(valname, value);
                         }
                         else
                         {
                             Tvals[valname] = value;
                         }
                     }


                     foreach (string key in Tvals.Keys)
                     {
                         switch (key)
                         {
                             case "Nivel":
                                 currentval = Tvals[key];
                                 break;
                             case "Nivel de potencia":
                                 currentpwr = Tvals[key];
                                 break;
                             case "PMW":
                                 currentpwm = Tvals[key];
                                 break;
                             case "Set point":
                                 currentsp = Tvals[key];
                                 break;
                             case "PID":
                                 PID = Tvals[key];
                                 break;
                             case "KP":
                                 KP = Tvals[key];
                                 break;
                             case "KI":
                                 KI = Tvals[key];
                                 break;
                             case "KD":
                                 KD = Tvals[key];
                                 break;
                             default:
                                 break;
                         }
                     }
                     System.Threading.Thread.Sleep(10); //Esperamos 10ms entre periodos de actualización para reducir el consumo de CPU.
                 }
             });

            //Expresión lambda que actualiza los gráficos y texto en pantalla según los diccionarios.
            Task.Run(() =>
            {
                while (true)
                {
                    if (!this.IsDisposed)
                    {
                        try
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                UpdateLabels();
                                UpdateCharts();
                                this.Update();
                            });
                        }
                        catch (ObjectDisposedException) { }
                        catch (InvalidOperationException) { }
                    }
                    System.Threading.Thread.Sleep(100);
                }
            });

        }

        //Subrutine que actualiza los valores de texto en la ventana
        private void UpdateLabels()
        {
            Dlabel.Text = currentval + " %";
            Plabel.Text = currentpwr + "%";
            label10.Text = currentsp;
            PMWLabel.Text = currentpwm;
            if (PID == "0") { label12.Text = "Modo: Fuzzy"; } else { label12.Text = "Modo: PID"; }
            KiVal.Text = KI;
            KpVal.Text = KP;
            KdVal.Text = KD;
        }

        //Subrutina que actualiza los gráficos de la ventana.
        private void UpdateCharts()
        {
            double tpot = 0;
            double tdist = 0;
            DateTime now = DateTime.Now;

            double.TryParse(currentpwr, out tpot);
            double.TryParse(currentval, out tdist);
            int pval2 = (int)Math.Round(tdist);
            int tpot2 = (int)Math.Round(tpot);
            if ((!(tpot2 > 100) && !(tpot2 < 0)))
            {
                progressBar1.Value = tpot2;
            }


            progressBar2.Minimum = 0;
            progressBar2.Maximum = 100;

            if (!(pval2 > 100) && !(pval2 < 0))
            {
                progressBar2.Value = pval2;
            }

            if (ChartDataList.Count > 800)
            {
                ChartDataList.Remove(ChartDataList[0]);
            }

            Cpoint tpoint = new Cpoint();
            tpoint.X = now;
            tpoint.Y = (float)tpot;

            if (tpoint.Y > 1200)
            {
                return;
            }
            ChartDataList.Add(tpoint);
            chart1.Series.Clear();
            chart1.Series.Add("Line Type");
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[0].XValueType = ChartValueType.DateTime;
            chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(5).ToOADate();

            for (int i = 0; i < ChartDataList.Count - 1; i++)
            {

                chart1.Series[0].Points.AddXY(ChartDataList[i].X, ChartDataList[i].Y);
            }

            if (ChartDataList2.Count > 800)
            {
                ChartDataList2.Remove(ChartDataList2[0]);
            }

            Cpoint tpoint2 = new Cpoint();
            tpoint2.X = now;
            tpoint2.Y = (float)tdist;
            ChartDataList2.Add(tpoint2);

            chart2.Series.Clear();
            chart2.Series.Add("Line Type");
            chart2.Series[0].IsVisibleInLegend = false;
            chart2.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart2.Series[0].XValueType = ChartValueType.DateTime;
            chart2.ChartAreas[0].AxisX.Maximum = now.AddSeconds(5).ToOADate();

            for (int i = 0; i < ChartDataList2.Count - 1; i++)
            {
                chart2.Series[0].Points.AddXY(ChartDataList2[i].X, ChartDataList2[i].Y);
            }
        }

        //Subrutina que se ejecuta al hacer click en el botón "abrir"
        private void OpenBtn_Click(object sender, EventArgs e)
        {
            tserialport.PortName = PortBox.SelectedItem.ToString();
            tserialport.BaudRate = 9600;
            try
            {
                tserialport.Open();
                CloseBtn.Enabled = true;
                OpenBtn.Enabled = false;
                PortBox.Enabled = false;
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                plusKi.Enabled = true;
                minusKi.Enabled = true;
                PlusKp.Enabled = true;
                minusKp.Enabled = true;
                PlusKd.Enabled = true;
                MinusKd.Enabled = true;
                numericUpDown1.Enabled = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error"); //Mostrar un cuadro de error con el texto exacto del error
            }
        }

        //Subrutina que se ejecuta al hacer click en el botón "cerrar"
        private void CloseBtn_Click(object sender, EventArgs e)
        {
            tserialport.Close();
            OpenBtn.Enabled = true;
            PortBox.Enabled = true;
            CloseBtn.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            plusKi.Enabled = false;
            minusKi.Enabled = false;
            PlusKp.Enabled = false;
            minusKp.Enabled = false;
            PlusKd.Enabled = false;
            MinusKd.Enabled = false;
            numericUpDown1.Enabled = false;
        }

        //Subrutina que se ejecuta al seleccionar un puerto en el cuadro desplegable
        private void PortBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            OpenBtn.Enabled = true;
        }

        //se ejecuta al cambiar el valor del SP
        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            byte[] tbyte = BitConverter.GetBytes((int)Math.Round(numericUpDown1.Value));
            tserialport.Write(">");
            tserialport.Write(tbyte, 0, 2);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            tserialport.Write("1"); //1F
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            tserialport.Write("2"); //20
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            tserialport.Write("3"); //21
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            tserialport.Write("4");
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            tserialport.Write("5");
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void PlusKi_Click(object sender, EventArgs e)
        {
            tserialport.Write("8");
        }

        private void MinusKi_Click(object sender, EventArgs e)
        {
            tserialport.Write("9");
        }

        private void PlusKp_Click(object sender, EventArgs e)
        {
            tserialport.Write("6");
        }

        private void MinusKp_Click(object sender, EventArgs e)
        {
            tserialport.Write("7");
        }

        private void PlusKd_Click(object sender, EventArgs e)
        {
            tserialport.Write(":");
        }

        private void MinusKd_Click(object sender, EventArgs e)
        {
            tserialport.Write(";");
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            tserialport.Write("=");
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            tserialport.Write("=");
        }
    }

    //Clase contenedora de puntos de tipo fecha,hora
    public class Cpoint
    {
        public DateTime X;
        public float Y;
    }


}
