// Сигналы на ардуино -- поменять характеристику ячейки, либо центр и амплитуду, либо мин и макс
// V1 - сравнения, AC, v2 -- сравнения, DC, v3 -- основной, AC, v4 -- основной, DC. sin(2a)~(V3/(V4*V1);

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Globalization;
using NPlot;
using RshCSharpWrapper;
using RshCSharpWrapper.RshDevice;

namespace PolarimeterApp
{
    public partial class Form1 : Form
    {
        SerialPort _serialPort;
        int State;
        int MaxPoints, TakeEachPoint, TakeEachPointNow;
        Int32 value1, value2, value3, value4, value5, value6;
        double time, val1, val2, val3, val4, val5;
        DateTime StartExperiment;
        String FileName;
        String XMinText, XMaxText, YMinText, YMaxText;
        Double XMin, XMax, YMin, YMax;

        StreamWriter OutputFile;
        double YMIN, YMAX;
        LinePlot NPlot1;
        double[] XX, YY;
        int NPoints;
        System.Char[] SerialWriteBuffer;
        int OperationMode;
        bool AutoRescale;

        int MODE_IDLE;
        int MODE_MAIN;
        int SCAN_CELL;

        private void button5_Click(object sender, EventArgs e)
        {
            _serialPort.Write("you rock\n");
        }

        char old_char = 'm';

        

        //Служебное имя платы, с которой будет работать программа.
        const string BOARD_NAME = "LAI24USB";
        //Внутренний объем блока данных.(внутренний буфер) Влияет на количество генерируемых прерываний в единицу времени.
        const uint IBSIZE = 32;
        //Фактически, от интенсивности генерируемых прерываний зависит производительность сбора.
        //Чем меньше прерываний, тем с большей вероятностью данные будут собраны без разрывов на высоких частотах.

        //Частота дискретизации (на канал). 
        const double SAMPLE_FREQ = 6.25;
        ////Количество внутренних буферов в конструируемом буфере данных.
        //const uint IBUFCNT = 10;

        //Код выполнения операции.
        RSH_API st;

        //Создание экземляра класса для работы с устройством
        Device device = new Device(BOARD_NAME);

        public Form1()
        {
            MODE_IDLE = -1;
            MODE_MAIN = 0;
            SCAN_CELL = 1;
            State = 0;
            OperationMode = MODE_IDLE;
            InitializeComponent();
            textBox1.Text = "COM3";
            SerialWriteBuffer = new System.Char[1];
            SerialWriteBuffer[0] = 'm';
            AutoRescale = true;
            MaxPoints = 10000;
        }
        private void ChangeOperationMode(int NewMode)
        {
            if (OperationMode == NewMode) return;
            if (NewMode == MODE_IDLE)
            {
                if (OutputFile != null) OutputFile.Close();
                button2.Enabled = false;
                button3.Enabled = false;
                textBox1.Enabled = true;
                timer1.Enabled = false;
                OperationMode = MODE_IDLE;
            }
            if (NewMode == MODE_MAIN)
            {
                SerialWriteBuffer[0] = 'm';
                StartExperiment = DateTime.Now;
                FileName = String.Format("polar{0,4:0000}_{1,2:00}_{2,2:00}-{3,2:00}_{4,2:00}_{5,2:00}.out",
                                                                                StartExperiment.Year,
                                                                                StartExperiment.Month,
                                                                                StartExperiment.Day,
                                                                                StartExperiment.Hour,
                                                                                StartExperiment.Minute,
                                                                                StartExperiment.Second);
                if(OutputFile!=null) OutputFile.Close();

                OutputFile = new StreamWriter(FileName);        // create file
                OutputFile.Close();

                label1.Text = FileName;
                YMIN = 0; YMAX = 0;
                NPlot1 = new LinePlot();
                NPoints = 0;
                AutoRescale = true;
                XX = new double[1];
                YY = new double[1];
                NPlot1.AbscissaData = XX;
                NPlot1.DataSource = YY;
                plotSurface2D1.Clear();
                plotSurface2D1.Add(NPlot1);
                button2.Enabled = true;
                button3.Enabled = true;
                button3.Text = "Scan";
                timer1.Interval = 100;
                timer1.Enabled = true;
                textBox1.Enabled = false;
                OperationMode = MODE_MAIN;
                TakeEachPoint = 1;
                TakeEachPointNow = 0;
            }
            if (NewMode == SCAN_CELL)
            {
                //SerialWriteBuffer[0] = 'm';
                StartExperiment = DateTime.Now;
                AutoRescale = false;
                FileName = String.Format("scan{0,4:0000}_{1,2:00}_{2,2:00}-{3,2:00}_{4,2:00}_{5,2:00}.out",
                                                                                StartExperiment.Year,
                                                                                StartExperiment.Month,
                                                                                StartExperiment.Day,
                                                                                StartExperiment.Hour,
                                                                                StartExperiment.Minute,
                                                                                StartExperiment.Second);
                if (OutputFile != null) OutputFile.Close();
                OutputFile = new StreamWriter(FileName);
                OutputFile.Close();

                label1.Text = FileName;
                YMIN = -3.0; YMAX = 3.0;
                NPlot1 = new LinePlot();
                NPoints = 0;
                XX = new double[1];
                YY = new double[1];
                NPlot1.AbscissaData = XX;
                NPlot1.DataSource = YY;
                plotSurface2D1.Clear();
                plotSurface2D1.Add(NPlot1);

                plotSurface2D1.XAxis1.WorldMin = -0.5;
                plotSurface2D1.XAxis1.WorldMax = 10.5;
                plotSurface2D1.YAxis1.WorldMin = -3.0;
                plotSurface2D1.YAxis1.WorldMax = 0.5;

                XMin = -0.5;
                XMax = 10.5;
                YMin = -2.5;
                YMax = 0.5;

                textBox2.Text = YMax.ToString();
                textBox3.Text = YMin.ToString();
                textBox4.Text = XMin.ToString();
                textBox5.Text = XMax.ToString();

                plotSurface2D1.Refresh();
 
                button2.Enabled = true;
                button3.Enabled = true;
                button3.Text = "Stop Scan";
                timer1.Interval = 100;
                timer1.Enabled = true;
                textBox1.Enabled = false;
                OperationMode = SCAN_CELL;
            }



        }

        public void ADC_init()
        {
            //=================== ИНФОРМАЦИЯ О ЗАГРУЖЕННОЙ БИБЛИОТЕКЕ ======================;
            string libVersion, libName, libCoreVersion, libCoreName;
            st = device.Get(RSH_GET.LIBRARY_VERSION_STR, out libVersion);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString();
                toolStripStatusLabel1.Text += ": Error while loading RSh lib; status= ";
                toolStripStatusLabel1.Text += st;
                return;
            }
            st = device.Get(RSH_GET.CORELIB_VERSION_STR, out libCoreVersion);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString();
                toolStripStatusLabel1.Text += ": Error while loading RSh lib core lib; status= ";
                toolStripStatusLabel1.Text += st;
                return;
            }
            st = device.Get(RSH_GET.CORELIB_FILENAME, out libCoreName);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString();
                toolStripStatusLabel1.Text += ": Error while loading RSh core lib; status= ";
                toolStripStatusLabel1.Text += st;
                return;
            }
            st = device.Get(RSH_GET.LIBRARY_FILENAME, out libName);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString();
                toolStripStatusLabel1.Text += ": Error while loading RSh lib; status= ";
                toolStripStatusLabel1.Text += st;
                return;
            }
            toolStripStatusLabel1.Text = "Successfully loaded libraries and drivers; ";
            //===================== ПРОВЕРКА СОВМЕСТИМОСТИ =================================;
            uint caps = (uint)RSH_CAPS.SOFT_PGATHERING_IS_AVAILABLE;
            //Проверим, поддерживает ли устройство функцию сбора данных в непрерывном режиме.
            st = device.Get(RSH_GET.DEVICE_IS_CAPABLE, ref caps);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString() + "Device is not capable; status= " + st;
                return;
            }
            //========================== ИНИЦИАЛИЗАЦИЯ =====================================;
            //Подключаемся к устройству. Нумерация начинается с 1.
            st = device.Connect(1);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString() + "Error when connecting to device; status= " + st;
                return;
            }
            //Структура для инициализации параметров работы устройства. 
            RshInitDMA p = new RshInitDMA();
            //Запуск устройства программный. 
            p.startType = (uint)RshInitDMA.StartTypeBit.Program;
            //Режим непрерывного сбора данных.
            p.dmaMode = (uint)RshInitDMA.DmaModeBit.Persistent;
            //Размер внутреннего блока данных, по готовности которого произойдёт прерывание.
            p.bufferSize = IBSIZE;
            //Частота дискретизации.
            p.frequency = SAMPLE_FREQ;
            //Сделаем все 4 канала активными;
            for (int i = 0; i <= 3; i++)
            {
                p.channels[i].control = (uint)RshChannel.ControlBit.Used;
                //Зададим коэффициент усиления для i-го канала.
                p.channels[i].gain = 1;
            }
            //Инициализация устройства (передача выбранных параметров сбора данных)
            //После инициализации неправильные значения в структуре будут откорректированы.
            st = device.Init(p);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString() + "Error when initializing device; status= " + st;
                return;
            }
            toolStripStatusLabel1.Text += "Successfully initialized device; ";

            //Время ожидания(в миллисекундах) до наступления прерывания. Прерывание произойдет при полном заполнении буфера.
            uint waitTime = 100000;
            st = device.Start();
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString() + "Error when starting data collection; status= " + st;
                return;
            }

            st = device.Get(RSH_GET.WAIT_BUFFER_READY_EVENT, ref waitTime);
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = DateTime.Now.ToString() + "Error with interruption time; status= " + st;
                return;
            }
            toolStripStatusLabel1.Text += "Successfully started data collection";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (State == 0)
            {
                //initializing ADC;
                ADC_init();
                //some work with arduino;
                _serialPort = new SerialPort();
                _serialPort.PortName = textBox1.Text;
                _serialPort.DataBits = 8;
                _serialPort.Parity = Parity.None;
                _serialPort.StopBits = StopBits.One;
                _serialPort.BaudRate = 9600;
                try
                {
                    _serialPort.Open();
                    _serialPort.Write("m\n");
                    toolStripStatusLabel1.Text = "Opened interface";
                    button1.Text = "Close";
                    State = 1;
                    ChangeOperationMode(MODE_MAIN);
                }

                //           _serialPort.RtsEnable = true;
                catch (System.IO.IOException)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                    toolStripStatusLabel1.Text += ": The port is in an invalid state";
                }
                catch (UnauthorizedAccessException)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                    toolStripStatusLabel1.Text += ": Access is denied to the port.";
                }
                catch (ArgumentOutOfRangeException)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                    toolStripStatusLabel1.Text += ": Wrong serial port configuration.";
                }
                catch (ArgumentException)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                    toolStripStatusLabel1.Text += ": Port name not supported.";
                }
                catch (InvalidOperationException)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                    toolStripStatusLabel1.Text += ": Port already open.";
                }


                // /* // DEBUG   
                //// Create a new SerialPort object with default settings.
                //_serialPort = new SerialPort();

                //_serialPort.PortName = textBox1.Text;
                //_serialPort.DataBits = 8;
                //_serialPort.Parity = Parity.None;
                //_serialPort.StopBits = StopBits.One;
                //_serialPort.BaudRate = 9600;
                ////            _serialPort.RtsEnable = true;

                //// Set the read/write timeouts
                //_serialPort.ReadTimeout = 5000;
                //_serialPort.WriteTimeout = 5000;
                //try
                //{
                //    _serialPort.Open();
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": Opened interface";
                //    button1.Text = "Close";
                //    State = 1;
                //    ChangeOperationMode(MODE_MAIN);
                //}
                //catch (System.IO.IOException)
                //{
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": The port is in an invalid state";
                //}
                //catch (UnauthorizedAccessException)
                //{
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": Access is denied to the port.";
                //}
                //catch (ArgumentOutOfRangeException)
                //{
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": Wrong serial port configuration.";
                //}
                //catch (ArgumentException)
                //{
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": Port name not supported.";
                //}
                //catch (InvalidOperationException)
                //{
                //    toolStripStatusLabel1.Text = DateTime.Now.ToString();
                //    toolStripStatusLabel1.Text += ": Port already open.";
                //}
                // */ // DEBUG                     
                //               textBox1.Text = "COM4";

                //                button1.Text = "Close";`
                //                State = 1;
                //                ChangeOperationMode(MODE_MAIN);
                // end DEBUG
            }
            else if (State == 1)
            {
                _serialPort.Write("i\n");
                _serialPort.Close(); //closing Arduino;
                device.Stop();//closing ADC;
                button1.Text = "Open";
                toolStripStatusLabel1.Text = DateTime.Now.ToString();
                toolStripStatusLabel1.Text += ": Closed interface";
                State = 0;
                ChangeOperationMode(MODE_IDLE);
            }
        }

        private double CalculateMean(double[] buffer, int i)
        {
            double result = 0.0;
            for (int j = 0; j < buffer.Length; j++)
            {
                if (j % 4 == i) result += buffer[j];
            }
            result /= (buffer.Length / 4);
            return result;
        }
        private void GetData(string mode = "")
        {
            double[] dataBuffer = new double[4];
            if (mode == "mean")
            {
                dataBuffer = new double[IBSIZE * 4];
            }
            st = device.GetData(dataBuffer); // Заполним буфер данными.
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = "Error when collecting data from device, status= " + st;
                return;
            }

            val1 = CalculateMean(dataBuffer, 0);
            val2 = CalculateMean(dataBuffer, 1);
            val3 = CalculateMean(dataBuffer, 2);
            val4 = CalculateMean(dataBuffer, 3);
            label11.Text = val1.ToString();
            label10.Text = val2.ToString();
            label9.Text = val3.ToString();
            label8.Text = val4.ToString();
            time = (DateTime.Now - StartExperiment).Ticks / 10000000.0;
            if ((Math.Abs(val2) <= 1.0e-5) || (Math.Abs(val4) <= 1.0e-5)) val5 = 0;
            else val5 = 8880.0 * val1 / (val2 * val4);
            label16.Text = val5.ToString();

            OutputFile = new StreamWriter(FileName, true);
            OutputFile.WriteLine(String.Format("{0} {1} {2} {3} {4} {5}", time, val1, val2, val3, val4, val5));
            OutputFile.Close();

            if ((NPoints < MaxPoints) && (((TakeEachPointNow++) == TakeEachPoint)))
            {
                if (val5 < YMIN) YMIN = val5;
                if (val5 > YMAX) YMAX = val5;
                NPoints++;
                Array.Resize(ref XX, NPoints);
                Array.Resize(ref YY, NPoints);
                XX[NPoints - 1] = time;
                YY[NPoints - 1] = val5;
                NPlot1.DataSource = YY;
                NPlot1.AbscissaData = XX;
                plotSurface2D1.Refresh();   // added new points
                TakeEachPointNow = 0;
            }
            if (NPoints == MaxPoints)
            {
                for (int i = 0; i < NPoints / 2; i++)
                {
                    XX[i] = XX[2 * i];
                    YY[i] = YY[2 * i];
                }
                NPoints /= 2;
                TakeEachPoint *= 2;
                TakeEachPointNow = 0;
                Array.Resize(ref XX, NPoints);
                Array.Resize(ref YY, NPoints);
            }
            if ((time > plotSurface2D1.XAxis1.WorldMax) && AutoRescale) PlotRescale();
            // DEBUG 
            //_serialPort.Write(SerialWriteBuffer, 0, 1);
        
        }
        private void ParseString(string InStr)
        {
            String[] words=InStr.Split(' ');
            double[] dataBuffer = new double[IBSIZE*4];
            st = device.GetData(dataBuffer); // Заполним буфер данными.
            if (st != RSH_API.SUCCESS)
            {
                toolStripStatusLabel1.Text = "Error when collecting data from device, status= " + st;
                //return;
            }

            val1 = CalculateMean(dataBuffer, 0);
            val2 = CalculateMean(dataBuffer, 1);
            val3 = CalculateMean(dataBuffer, 2);
            val4 = CalculateMean(dataBuffer, 3);
            label11.Text = val1.ToString();
            label10.Text = val2.ToString();
            label9.Text = val3.ToString();
            label8.Text = val4.ToString();
            try
            {
                
                if (words[0] == "m")
                {
                    ChangeOperationMode(MODE_MAIN);
                    //try
                    //{
                    //    value1 = Int32.Parse(words[1]);
                    //    value2 = Int32.Parse(words[2]);
                    //    value3 = Int32.Parse(words[3]);
                    //    value4 = Int32.Parse(words[4]);
                    //    value5 = Int32.Parse(words[5]);
                    //    value6 = Int32.Parse(words[6]);
                    //}
                    //catch
                    //{

                    //}

                    time = (DateTime.Now - StartExperiment).Ticks / 10000000.0;


                    //val1 = (value1 - 8388608) * 5000.0 / 16777216.0;
                    //val2 = (value2 - 8388608) * 5.0 / 16777216.0;
                    //val3 = (value3 - 8388608) * 5.0 / 16777216.0;
                    //val4 = (value4 - 8388608) * 5000.0 / 16777216.0;

                    if ((Math.Abs(val2) <= 1.0e-5) || (Math.Abs(val4) <= 1.0e-5)) val5 = 0;
                    else val5 = 8880.0 * val1 / (val2 * val4);

                    //label2.Text = value1.ToString();
                    //label3.Text = value2.ToString();
                    //label4.Text = value3.ToString();
                    //label5.Text = value4.ToString();
                    //label6.Text = value5.ToString();
                    //label7.Text = value6.ToString();

                    //label11.Text = val1.ToString();
                    //label10.Text = val2.ToString();
                    //label9.Text = val3.ToString();
                    //label8.Text = val4.ToString();

                    label16.Text = val5.ToString();

                    OutputFile = new StreamWriter(FileName, true);
                    OutputFile.WriteLine(String.Format("{0} {1} {2} {3} {4} {5} {6} {7} ", time, value1, value2, value3, value4, value5, value6, val5));
                    OutputFile.Close();


                    if ((NPoints < MaxPoints)&&(((TakeEachPointNow++) == TakeEachPoint)))
                    {
                            if (val5 < YMIN) YMIN = val5;
                            if (val5 > YMAX) YMAX = val5;
                            NPoints++;
                            Array.Resize(ref XX, NPoints);
                            Array.Resize(ref YY, NPoints);
                            XX[NPoints - 1] = time;
                            YY[NPoints - 1] = val5;
                            NPlot1.DataSource = YY;
                            NPlot1.AbscissaData = XX;
                            plotSurface2D1.Refresh();   // added new points
                            TakeEachPointNow = 0;
                    }
                    if (NPoints == MaxPoints)
                    {
                        for (int i = 0; i < NPoints / 2; i++)
                        {
                            XX[i] = XX[2 * i];
                            YY[i] = YY[2 * i];
                        }
                        NPoints /= 2;
                        TakeEachPoint *= 2;
                        TakeEachPointNow = 0;
                        Array.Resize(ref XX, NPoints);
                        Array.Resize(ref YY, NPoints);
                    }
                    if ((time > plotSurface2D1.XAxis1.WorldMax) && AutoRescale) PlotRescale();
                    // DEBUG 
                    
                    
 //                   toolStripStatusLabel1.Text = SerialWriteBuffer[0].ToString();
                }

                if (words[0] == "s")
                {
                    ChangeOperationMode(SCAN_CELL);
                    int voltage = Int32.Parse(words[1]);

                    //value6 = Int32.Parse(words[1]);
                    //value3 = Int32.Parse(words[2]);
                    //if (value3 == 16777215) return;
                    //if (value3 == 0) return;

                    time = (DateTime.Now - StartExperiment).Ticks / 10000000.0;


                    //val1 = value6 * 10.0 / 1024;
                    //val3 = (value3 - 8388608) * 5.0 / 16777216.0;

                    //                val5 = 100.0 * val1 / (val2 * val4);

                    label2.Text = "-";
                    label3.Text = "-";
                    //label4.Text = value3.ToString();
                    label5.Text = "-";
                    //label6.Text = value6.ToString();
                    label7.Text = "-";

                    //label11.Text = val1.ToString();
                    label11.Text = voltage.ToString();
                    label10.Text = "-";
                    //label9.Text = val3.ToString();
                    label9.Text = val2.ToString();
                    label8.Text = "-";

                    label16.Text = "-";

                    OutputFile = new StreamWriter(FileName, true);
                    OutputFile.WriteLine(String.Format("{0} {1} {2} {3} {4} ", value6, value3, val1, val3, time));
                    OutputFile.Close();


                    if (NPoints <= MaxPoints)
                    {
                        if (val3 < YMIN) YMIN = val3;
                        if (val3 > YMAX) YMAX = val3;
                        NPoints++;
                        Array.Resize(ref XX, NPoints);
                        Array.Resize(ref YY, NPoints);
                        XX[NPoints - 1] = val1;
                        YY[NPoints - 1] = val3;
                        NPlot1.DataSource = YY;
                        NPlot1.AbscissaData = XX;
                        plotSurface2D1.Refresh();  // added new points
                    }
                    //                if (time > plotSurface2D1.XAxis1.WorldMax) PlotRescale();
                }
            }
            catch (System.FormatException)
            {
            }
            SerialControl();
        }

        private void SerialControl()
        {
            //if (SerialWriteBuffer[0] != old_char)
            //{
            //    old_char = SerialWriteBuffer[0];
            //    toolStripStatusLabel2.Text = SerialWriteBuffer[0].ToString();
            //    _serialPort.Write(SerialWriteBuffer, 0, 1);
            //}
            // passing ac and dc parts of reference channel to Arduino;
            string write_str;

            if (OperationMode == MODE_MAIN) write_str = SerialWriteBuffer[0].ToString() + " " + Convert.ToInt32(val1).ToString() + " " + Convert.ToInt32(val2).ToString() + "\n";
            else write_str = SerialWriteBuffer[0].ToString()+"\n";
            _serialPort.Write(write_str);
            toolStripStatusLabel1.Text = write_str;
            
        }

        private void PlotRescale()
        {
            if (AutoRescale)
            {
                if (OperationMode == MODE_MAIN)
                {
                    plotSurface2D1.XAxis1.WorldMin = 0;
                    plotSurface2D1.XAxis1.WorldMax = time;
                    plotSurface2D1.YAxis1.WorldMin = YMIN;
                    plotSurface2D1.YAxis1.WorldMax = YMAX;

                    plotSurface2D1.XAxis1.WorldMin -= plotSurface2D1.XAxis1.WorldLength * 0.1;
                    plotSurface2D1.XAxis1.WorldMax += plotSurface2D1.XAxis1.WorldLength * 0.5;
                    plotSurface2D1.YAxis1.WorldMin -= plotSurface2D1.YAxis1.WorldLength * 0.1;
                    plotSurface2D1.YAxis1.WorldMax += plotSurface2D1.YAxis1.WorldLength * 0.1;

                    textBox2.Text = plotSurface2D1.YAxis1.WorldMax.ToString("F0");
                    textBox3.Text = plotSurface2D1.YAxis1.WorldMin.ToString("F0");
                    textBox4.Text = plotSurface2D1.XAxis1.WorldMin.ToString("F0");
                    textBox5.Text = plotSurface2D1.XAxis1.WorldMax.ToString("F0");

                    XMin = plotSurface2D1.XAxis1.WorldMin;
                    XMax = plotSurface2D1.XAxis1.WorldMax;
                    YMin = plotSurface2D1.YAxis1.WorldMin;
                    YMax = plotSurface2D1.YAxis1.WorldMax;

                    textBox2.BackColor = SystemColors.Menu;
                    textBox3.BackColor = SystemColors.Menu;
                    textBox4.BackColor = SystemColors.Menu;
                    textBox5.BackColor = SystemColors.Menu;

                    plotSurface2D1.Refresh();
                }
                if (OperationMode == SCAN_CELL)
                {
                    plotSurface2D1.XAxis1.WorldMin = -0.5;
                    plotSurface2D1.XAxis1.WorldMax = 10.5;
                    plotSurface2D1.YAxis1.WorldMin = -2.5;
                    plotSurface2D1.YAxis1.WorldMax = 0.5;

                    XMin = -0.5;
                    XMax = 10.5;
                    YMin = -2.5;
                    YMax = 0.5;

                    textBox2.Text = YMax.ToString("F0");
                    textBox3.Text = YMin.ToString("F0");
                    textBox4.Text = XMin.ToString("F0");
                    textBox5.Text = XMax.ToString("F0");
 
                    textBox2.BackColor = SystemColors.Menu;
                    textBox3.BackColor = SystemColors.Menu;
                    textBox4.BackColor = SystemColors.Menu;
                    textBox5.BackColor = SystemColors.Menu;

                    plotSurface2D1.Refresh();
                }
            }
            else
            {
                plotSurface2D1.XAxis1.WorldMin = XMin;
                plotSurface2D1.XAxis1.WorldMax = XMax;
                plotSurface2D1.YAxis1.WorldMin = YMin;
                plotSurface2D1.YAxis1.WorldMax = YMax;

                textBox2.BackColor = SystemColors.Window;
                textBox3.BackColor = SystemColors.Window;
                textBox4.BackColor = SystemColors.Window;
                textBox5.BackColor = SystemColors.Window;

                plotSurface2D1.Refresh();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label18.Text = ((DateTime.Now - StartExperiment).Ticks/10000000.0).ToString();
            string str;
            // /* //DEBUG
            if (_serialPort.IsOpen)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    try
                    {
                        str = _serialPort.ReadLine();
                        toolStripStatusLabel2.Text = str;
                        ParseString(str);
                    }
                    catch (TimeoutException)
                    {
                        toolStripStatusLabel2.Text = "Timeout";
                    }
                }
            }
            // */ // DEBUG
            //           ParseString("m 8312537 6100722 4193604 8887691 369 376");
            //GetData(/*mode: "mean"*/);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            AutoRescale = true;
            PlotRescale();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (OperationMode == MODE_MAIN)
            {
                SerialWriteBuffer[0] = 's';
            }
            if (OperationMode == SCAN_CELL)
            {
                SerialWriteBuffer[0] = 'm';
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            AutoRescale = false;
            try
            {
                YMax = Double.Parse(textBox2.Text);
            }
            catch (System.FormatException) { textBox2.Text = YMax.ToString("F0"); }
            try
            {
                YMin = Double.Parse(textBox3.Text);
            }
            catch (System.FormatException) { textBox3.Text = YMin.ToString("F0"); }
            try
            {
                XMin = Double.Parse(textBox4.Text);
            }
            catch (System.FormatException) { textBox4.Text = XMin.ToString("F0"); }
            try
            {
                XMax = Double.Parse(textBox5.Text);
            }
            catch (System.FormatException) { textBox5.Text = XMax.ToString("F0"); }
            PlotRescale();

        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (_serialPort !=null && _serialPort.IsOpen)
            {
                _serialPort.Write("i\n");
                _serialPort.Close();
            }
            device.Stop();//closing ADC;
        }






        }
}
