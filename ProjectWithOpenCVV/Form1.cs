using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;

namespace ProjectWithOpenCVV
{
    struct WaveParam
    {
        public int pos;
        public int value;
        public int timeNotFind;
        public int name;
    }

    public partial class Form1 : Form
    {
        CvCapture capture;
        bool captureOpened;
        Bitmap tempBM;
        int PropCentreImageX;
        int PropCentreImageY;
        int tempStartPointX;
        int tempStartPointY;
        int tempEndPointX;
        int tempEndPointY;
        double PropAngleImage;
        double PropCoefForDist;
        List<KeyValuePair<int, int>> BufferForXRight = new List<KeyValuePair<int, int>>();
        List<WaveParam> StoredListXRight = new List<WaveParam>();
        List<WaveParam> HistoryListXRight = new List<WaveParam>();
        SerialPort mySerialPort = new SerialPort();
        double thisVoltageKV;

        public Form1()
        {
            InitializeComponent();
            captureOpened = false;
            toolTip1.SetToolTip(PropertyScreen, "Правая кнопка мыши - указание центра\nЛевая кнопка мыши + зажатие - указание длинны");
            this.Size = new Size(572, 361);
            toolStripStatusLabel1.Text = "Укажите источник видео для анализа";

            string[] ports = SerialPort.GetPortNames();

            mySerialPort.BaudRate = 9600;
            // mySerialPort.PortName = ports[0];  /*НЕ ЗАБЫТЬ РАСКОММЕНТИТЬ*/
        }

        private void BUT_Pick_File_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Video files|*.avi";
            openFileDialog1.Title = "Select a Video File";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                capture = CvCapture.FromFile(openFileDialog1.FileName);
                if (capture.FrameCount > 0)
                {
                    lABEl_indicator_source.Text = "Файл";
                    lABEl_indicator_source.BackColor = Color.Green;
                    Label_ParamSource.Text = "Ширина = " + capture.FrameWidth.ToString() + "\nВысота = " + capture.FrameHeight.ToString() + "\nКоличество кадров = " + capture.FrameCount.ToString();
                    PropCentreImageX = capture.FrameWidth / 2;
                    PropCentreImageY = capture.FrameHeight / 2;
                    captureOpened = true;
                    toolStripStatusLabel1.Text = "Источник указан верно, переходите к настройкам параметров";
                }
                else
                {
                    lABEl_indicator_source.Text = "Файл поврежден";
                    lABEl_indicator_source.BackColor = Color.Red;
                    captureOpened = false;
                }
            }
        }

        private void ComBox_DropDown(object sender, EventArgs e)
        {
            ComBox_CamList.Items.Clear();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    ComBox_CamList.Items.Add(i.ToString());
                }
                catch
                {
                }
            }
        }

        private void CamList_PickCam(object sender, EventArgs e)
        {
            try
            {
                capture = CvCapture.FromCamera(ComBox_CamList.SelectedIndex);
                lABEl_indicator_source.Text = "Камера";
                lABEl_indicator_source.BackColor = Color.Green;
                Label_ParamSource.Text = "Ширина = " + capture.FrameWidth.ToString() + "\nВысота = " + capture.FrameHeight.ToString();
                PropCentreImageX = capture.FrameWidth / 2;
                PropCentreImageY = capture.FrameHeight / 2;
                captureOpened = true;
            }
            catch
            {
                lABEl_indicator_source.Text = "Камера не обнаружена";
                lABEl_indicator_source.BackColor = Color.Red;
                captureOpened = false;
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0 && captureOpened)
            {
                capture.GrabFrame();


                IplImage frame = capture.RetrieveFrame();
                try
                {
                    tempBM = BitmapConverter.ToBitmap(frame);
                }
                catch
                {
                    if (lABEl_indicator_source.Text == "Файл") capture = CvCapture.FromFile(openFileDialog1.FileName);
                    return;

                }
                PreviueScreen.Image = tempBM;
            }
            if (tabControl1.SelectedIndex == 1 && captureOpened)
            {
                capture.GrabFrame();
                IplImage frame = capture.RetrieveFrame();
                try
                {
                    tempBM = BitmapConverter.ToBitmap(frame);
                }
                catch
                {
                    if (lABEl_indicator_source.Text == "Файл") capture = CvCapture.FromFile(openFileDialog1.FileName);
                    return;
                }
                int tempA = Convert.ToInt32(VisibleProp_SmoothPower.Value);
                tempA = tempA % 2 == 1 ? tempA : tempA - 1;
                frame.Smooth(frame, SmoothType.Blur, tempA, tempA);
                CvColor BlueDiff = new CvColor(255, 255, 0);
                CvColor RedDiff = new CvColor(0, 255, 255);
                CvColor GreenDiff = new CvColor(255, 0, 255);
                if (radioButton3.Checked) frame -= RedDiff;
                if (radioButton4.Checked) frame -= GreenDiff;
                if (radioButton5.Checked) frame -= BlueDiff;
                if (checkBox3.Checked)
                {
                    frame.Normalize(frame, 0, 255, NormType.MinMax);
                }

                frame.Line(PropCentreImageX, 0, PropCentreImageX, frame.Height, new CvScalar(0, 255, 0));
                frame.Line(0, PropCentreImageY, frame.Width, PropCentreImageY, new CvScalar(0, 0, 255));
                frame.Line(PropCentreImageX, PropCentreImageY, (int)(PropCentreImageX + 600 * Math.Cos(PropAngleImage)), (int)(PropCentreImageY + 600 * Math.Sin(PropAngleImage)), new CvScalar(0, 255, 255));
                frame.Line(tempStartPointX, tempStartPointY, tempEndPointX, tempEndPointY, new CvScalar(255, 0, 0), 2);
                tempBM = BitmapConverter.ToBitmap(frame);
                PropertyScreen.Image = tempBM;
            }
            if (tabControl1.SelectedIndex == 2 && captureOpened && ModeGetsFrame_ByTimer.Checked)
            {
                But_NextFrame_Click(sender, e);
            }
        }

        private void EventMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && captureOpened)
            {
                PropCentreImageX = e.X * (capture.FrameWidth / 320);
                PropCentreImageY = e.Y * (capture.FrameHeight / 240);
            }
            if (e.Button == MouseButtons.Left && captureOpened)
            {
                tempStartPointX = e.X * (capture.FrameWidth / 320);
                tempStartPointY = e.Y * (capture.FrameHeight / 240);
            }
        }

        private void EventMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && captureOpened)
            {
                tempEndPointX = e.X * (capture.FrameWidth / 320);
                tempEndPointY = e.Y * (capture.FrameHeight / 240);
            }
        }

        private void EventMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && captureOpened)
            {
                tempEndPointX = e.X * (capture.FrameWidth / 320);
                tempEndPointY = e.Y * (capture.FrameHeight / 240);
                VisibleProp_DistanceOnImage.Value = Convert.ToDecimal(Math.Abs(tempEndPointX - tempStartPointX) + Math.Abs(tempEndPointY - tempStartPointY));
                PropCoefForDist = Math.Abs(tempEndPointX - tempStartPointX) + Math.Abs(tempEndPointY - tempStartPointY);
            }
            if (e.Button == MouseButtons.Right && captureOpened)
            {
                PropAngleImage = ((double)(PropCentreImageY - e.Y * (capture.FrameHeight / 240)) / (double)(PropCentreImageX - e.X * (capture.FrameWidth / 320)));
                MessageBox.Show(PropAngleImage.ToString());
            }
        }

        private void PropDistanceChange(object sender, EventArgs e)
        {
            PropCoefForDist = (Convert.ToDouble(VisibleProp_DistanceOnImage.Value) / (Math.Abs(tempEndPointX - tempStartPointX) + Math.Abs(tempEndPointY - tempStartPointY)));
        }

        private void EventTabChange(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                this.Size = new Size(572, 361);
            }
            if (tabControl1.SelectedIndex == 1)
            {
                this.Size = new Size(864, 361);
            }
            if (tabControl1.SelectedIndex == 2)
            {
                if (ModeGetsFrame_ByButton.Checked && captureOpened) But_NextFrame_Click(sender, e);
                this.Size = new Size(938, 444);
            }
        }

        private void But_NextFrame_Click(object sender, EventArgs e)
        {
            String InfoString = "";
            double AverageOffset = 0;
            if (tabControl1.SelectedIndex == 2 && captureOpened)
            {
                capture.GrabFrame();
                IplImage frame = capture.RetrieveFrame();
                try
                {
                    tempBM = BitmapConverter.ToBitmap(frame);
                }
                catch
                {
                    if (lABEl_indicator_source.Text == "Файл") capture = CvCapture.FromFile(openFileDialog1.FileName);
                    return;
                }
                int tempA = Convert.ToInt32(VisibleProp_SmoothPower.Value);
                tempA = tempA % 2 == 1 ? tempA : tempA - 1;
                frame.Smooth(frame, SmoothType.Blur, tempA, tempA);

                if (checkBox3.Checked)
                {
                    CvColor BlueDiff = new CvColor(255, 255, 0);
                    CvColor RedDiff = new CvColor(0, 255, 255);
                    CvColor GreenDiff = new CvColor(255, 0, 255);
                    if (radioButton3.Checked) frame -= RedDiff;
                    if (radioButton4.Checked) frame -= GreenDiff;
                    if (radioButton5.Checked) frame -= BlueDiff;
                    frame.Normalize(frame, 0, 255, NormType.MinMax);
                }

                IplImage HistX = new IplImage(frame.Width, 266, BitDepth.U8, 3);
                int[] HistXData = new int[frame.Width];
                for (int x = 0; x < frame.Width; x++)
                {
                    CvColor c = frame[PropCentreImageY, x];
                    HistX.Line(x, 266, x, 0, new CvScalar(0, 0, 0));

                    if (radioButton3.Checked)
                    {
                        HistX.Line(x, 255, x, 255 - c.R, new CvScalar(0, 0, c.R));
                        HistXData[x] = c.R;
                    }
                    if (radioButton4.Checked)
                    {
                        HistX.Line(x, 255, x, 255 - c.G, new CvScalar(0, c.G, 0));
                        HistXData[x] = c.G;
                    }
                    if (radioButton5.Checked)
                    {
                        HistX.Line(x, 255, x, 255 - c.B, new CvScalar(c.B, 0, 0));
                        HistXData[x] = c.B;
                    }
                    if (radioButton6.Checked)
                    {
                        HistX.Line(x, 255, x, 255 - (c.R + c.B + c.G) / 3, new CvScalar(c.B, c.G, c.R));
                        HistXData[x] = (c.R + c.B + c.G) / 3;
                    }
                }
                int Max = 0;
                int posMax = 0;
                for (int x = 0; x < frame.Width - 2; x++)
                {
                    if (HistXData[x] > Max)
                    {
                        posMax = x;
                        Max = HistXData[x];
                    }
                }
                var listXRight = new List<KeyValuePair<int, int>>();
                listXRight.Add(new KeyValuePair<int, int>(posMax, Max));
                int posScan = posMax;
                bool isFindMax = false;
                while (posScan < frame.Width)
                {
                    int deltaScan = posScan + 10;
                    int HistoryPosScan = posScan;
                    while (deltaScan - posScan < 300 || deltaScan < frame.Width)
                    {
                        int newMin = HistXData[posScan];
                        int posNewMin = posScan;
                        for (int i = posScan; i < deltaScan; i++)
                        {
                            if (!isFindMax && i < frame.Width)
                            {
                                if (HistXData[i] < newMin)
                                {
                                    newMin = HistXData[i];
                                    posNewMin = i;
                                }
                            }
                            if (isFindMax && i < frame.Width)
                            {
                                if (HistXData[i] > newMin)
                                {
                                    newMin = HistXData[i];
                                    posNewMin = i;
                                }
                            }
                        }
                        if (posNewMin < deltaScan - numericUpDown6.Value)
                        {
                            if (Math.Abs(HistXData[posNewMin] - listXRight[listXRight.Count - 1].Value) < numericUpDown5.Value)
                            {
                                deltaScan += Convert.ToInt32(numericUpDown7.Value);
                                if (deltaScan > frame.Width) break;
                                continue;
                            }
                            posScan = posNewMin;
                            // ошибка здесь!!!
                            if (HistXData[deltaScan] > numericUpDown11.Value) listXRight.Add(new KeyValuePair<int, int>(posNewMin, HistXData[posNewMin]));
                            isFindMax = !isFindMax;
                            break;
                        }
                        else
                        {
                            deltaScan += Convert.ToInt32(numericUpDown7.Value);
                            if (deltaScan > frame.Width) break;
                        }
                    }
                    if (posScan == HistoryPosScan) break;
                }
                HistX.Line(listXRight[0].Key, 255, listXRight[0].Key, 255 - listXRight[0].Value, new CvScalar(255, 255, 255));
                for (int i = 1; i < listXRight.Count; i++)
                {
                    if (i % 2 == 0) HistX.Line(listXRight[i].Key, 255, listXRight[i].Key, 255 - listXRight[i].Value, new CvScalar(255, 0, 0));
                    if (i % 2 == 1) HistX.Line(listXRight[i].Key, 0, listXRight[i].Key, 255 - listXRight[i].Value, new CvScalar(0, 255, 0));
                }
                BufferForXRight = listXRight;
                if (checkBox2.Checked)
                {
                    for (int j = 0; j < StoredListXRight.Count; j++)
                    {
                        bool flag = true;
                        for (int i = 0; i < listXRight.Count; i += 2)
                        {
                            if (Math.Abs(StoredListXRight[j].value - listXRight[i].Value) + Math.Abs(StoredListXRight[j].pos - listXRight[i].Key) < numericUpDown8.Value)
                            {
                                WaveParam temp;
                                temp.pos = Convert.ToInt32(((listXRight[i].Key - StoredListXRight[j].pos) * Convert.ToDouble(numericUpDown9.Value)) + StoredListXRight[j].pos);
                                temp.value = Convert.ToInt32(((listXRight[i].Value - StoredListXRight[j].value) * Convert.ToDouble(numericUpDown9.Value)) + StoredListXRight[j].value);
                                temp.name = StoredListXRight[j].name;
                                temp.timeNotFind = 0;
                                StoredListXRight[j] = temp;
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            if (StoredListXRight[j].timeNotFind > numericUpDown10.Value)
                            {
                                StoredListXRight.RemoveAt(j);
                                j = 0;
                                if (StoredListXRight.Count == 0)
                                {
                                    checkBox2.BackColor = Color.Red;
                                    checkBox2.Checked = false;
                                }
                            }
                            else
                            {
                                WaveParam temp;
                                temp.pos = StoredListXRight[j].pos;
                                temp.value = StoredListXRight[j].value;
                                temp.name = StoredListXRight[j].name;
                                temp.timeNotFind = StoredListXRight[j].timeNotFind + 1;
                                StoredListXRight[j] = temp;
                            }
                        }
                    }
                    for (int i = 0; i < StoredListXRight.Count; i++)
                    {
                        int color = Convert.ToInt32(255 - ((double)StoredListXRight[i].timeNotFind / Convert.ToDouble(numericUpDown10.Value)) * 255);
                        HistX.PutText(StoredListXRight[i].name.ToString(), new CvPoint(StoredListXRight[i].pos - 3, 263), new CvFont(FontFace.HersheyDuplex, 0.3, 0.3), new CvScalar(color, color, color));
                    }
                    int countForPercent = 0;
                    for (int i = 0; i < StoredListXRight.Count; i++)
                    {
                        for (int j = 0; j < HistoryListXRight.Count; j++)
                        {
                            if (StoredListXRight[i].name == HistoryListXRight[j].name)
                            {
                                if (StoredListXRight[i].pos - HistoryListXRight[j].pos <= 0 && i != 0)
                                {
                                    int BaseDelta = HistoryListXRight[j].pos - HistoryListXRight[j - 1].pos;
                                    int ThisDelta = HistoryListXRight[j].pos - StoredListXRight[i].pos;
                                    double PercenteFillDelta = ((double)ThisDelta / (double)BaseDelta) * 100.0;
                                    if (PercenteFillDelta > 100) HistX.Line(StoredListXRight[i].pos, 255 - StoredListXRight[i].value, HistoryListXRight[j].pos, 255 - HistoryListXRight[j].value, new CvScalar(0, 255, 0), 3);
                                    countForPercent++;
                                    AverageOffset += PercenteFillDelta;
                                }
                                if (StoredListXRight[i].pos - HistoryListXRight[j].pos > 0 && i != StoredListXRight.Count - 1)
                                {
                                    int BaseDelta = HistoryListXRight[j + 1].pos - HistoryListXRight[j].pos;
                                    int ThisDelta = StoredListXRight[i].pos - HistoryListXRight[j].pos;
                                    double PercenteFillDelta = ((double)ThisDelta / (double)BaseDelta) * 100.0;
                                    if (PercenteFillDelta > 100) HistX.Line(StoredListXRight[i].pos, 255 - StoredListXRight[i].value, HistoryListXRight[j].pos, 255 - HistoryListXRight[j].value, new CvScalar(255, 0, 0), 3);
                                    countForPercent++;
                                    AverageOffset += PercenteFillDelta;
                                }
                                HistX.Line(StoredListXRight[i].pos, 255 - StoredListXRight[i].value, HistoryListXRight[j].pos, 255 - HistoryListXRight[j].value, new CvScalar(255, 255, 255));
                            }
                        }

                    }
                    if (countForPercent != 0) AverageOffset /= countForPercent;
                    else AverageOffset = 0;
                }
                List<double> StackCoef = new List<double>();
                double coefGeomProgr = 0;
                if (listXRight.Count > 2) coefGeomProgr = listXRight[1].Key - listXRight[0].Key;
                for (int i = 1; i < listXRight.Count; i++)
                {
                    if (i + 1 < listXRight.Count - 1)
                    {
                        double tempCoef = listXRight[i + 1].Key - listXRight[i].Key;
                        StackCoef.Add(tempCoef / coefGeomProgr);
                    }
                }
                coefGeomProgr = 0;
                for (int i = 0; i < StackCoef.Count; i++)
                {
                    coefGeomProgr += StackCoef[i];
                }
                coefGeomProgr = coefGeomProgr / StackCoef.Count;
                InfoString += (coefGeomProgr).ToString("F2") + " - Coef\n";
                tempBM = BitmapConverter.ToBitmap(HistX);
                HistByX_Screen.Image = tempBM;
            }
            InfoString += "Правые изохромы\n";
            InfoString += BufferForXRight.Count + " - Количество обнаруженных\n";
            if (checkBox2.Checked) InfoString += StoredListXRight.Count + " - Количество сопровождаемых\n";
            else InfoString += "Фиксация не произведена\n";
            if (checkBox2.Checked) InfoString += HistoryListXRight.Count - StoredListXRight.Count + " - Количество потерянных\n";
            if (checkBox2.Checked) InfoString += AverageOffset.ToString("F0") + "% - Средняя сдвижка изохром\n";
            label3.Text = InfoString;
            if (checkBox2.Checked && AverageOffset < 100) { progressBar1.Value = (int)AverageOffset; groupBox6.BackColor = Color.Transparent; }
            if (checkBox2.Checked && AverageOffset > 100)
            {
                progressBar1.Value = 100; groupBox6.BackColor = Color.Orange;
                if (button1.Enabled == false)
                {
                    label13.Text = thisVoltageKV.ToString() + " KV\n";
                    thisVoltageKV = 0;
                    setVoltage(thisVoltageKV);
                    timer2.Enabled = false;
                    mySerialPort.Close();
                    button1.Enabled = true;
                    checkBox2.Checked = false;
                    StartFolow(new object(), new EventArgs());
                }
            }
        }


        private void Event_FPS_Change(object sender, EventArgs e)
        {
            timer1.Interval = Convert.ToInt32(1000 / numericUpDown4.Value);
        }

        private void SmothChange(object sender, ScrollEventArgs e)
        {
            label6.Text = "Сглаживание " + VisibleProp_SmoothPower.Value.ToString() + " точек";
        }

        private void StartFolow(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                StoredListXRight.Clear();
                HistoryListXRight.Clear();
                if (BufferForXRight.Count > 0)
                {
                    for (int i = 0; i < BufferForXRight.Count; i += 2)
                    {
                        WaveParam temp;
                        temp.pos = BufferForXRight[i].Key;
                        temp.value = BufferForXRight[i].Value;
                        temp.name = (i / 2) + 1;
                        temp.timeNotFind = 0;
                        StoredListXRight.Add(temp);
                        HistoryListXRight.Add(temp);
                    }
                    checkBox2.Checked = true;
                    checkBox2.BackColor = Color.Green;
                }
                else
                {
                    checkBox2.Checked = false;
                    checkBox2.BackColor = Color.Red;
                }
            }
            else
            {
                checkBox2.BackColor = Color.Red;
            }
        }


        public void setVoltage(double KVt)
        {
            double CurrentVoltage = KVt;
            int outputData = (int)(Math.Pow(((double)CurrentVoltage), 3) * 1.5613 - Math.Pow(((double)CurrentVoltage), 2) * 67.071 + ((double)CurrentVoltage) * 2765.2 - 255.38);
            label11.Text = outputData.ToString();
            if (outputData < 0 || outputData > 65000) outputData = 0;
            label12.Text = KVt.ToString();
            mySerialPort.Write(outputData.ToString() + "\n");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            mySerialPort.Open();
            //double CurrentVoltage = 0.01f;
            //int outputData
            thisVoltageKV = 0;
            timer2.Interval = (int)(numericUpDown13.Value * 1000);
            //mySerialPort.Write(((int)(Math.Pow(((double)CurrentVoltage), 3) * 1.5613 - Math.Pow(((double)CurrentVoltage), 2) * 67.071 + ((double)CurrentVoltage) * 2765.2 - 255.38)).ToString() + "\n");
            timer2.Enabled = true;
            button1.Enabled = false;
            //countVoltageChange = (int)(numericUpDown12.Value/ numericUpDown13.Value);
            //deltaVolts = (int)(numericUpDown13.Value * 10);
            //currentVoltage = 0;
        }

        private void NextVoltage(object sender, EventArgs e)
        {
            thisVoltageKV += (double)numericUpDown14.Value;
            if (thisVoltageKV > (double)numericUpDown12.Value)
            {
                thisVoltageKV = 0;
                setVoltage(thisVoltageKV);
                timer2.Enabled = false;
                button1.Enabled = true;
                mySerialPort.Close();
            }
            else
            {
                setVoltage(thisVoltageKV);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            thisVoltageKV = 0;
            setVoltage(thisVoltageKV);
            timer2.Enabled = false;
            mySerialPort.Close();
            button1.Enabled = true;

            label14.Text = "D = " + numericUpDown1.Value.ToString();
            label15.Text = "L = " + numericUpDown2.Value.ToString();
            label16.Text = "λ = " + numericUpDown3.Value.ToString();
            label21.Text = label13.Text;

            if (radioButton1.Checked == true)
            {
                label22.Text = "r22" + ((double)numericUpDown3.Value * (double)numericUpDown1.Value) / (2 * 3.14 * (double)numericUpDown2.Value * Convert.ToDouble(label13.Text));
            }
            else
            {
                label22.Text = "rэф" + ((double)numericUpDown3.Value * (double)numericUpDown1.Value) / (3.14 * (double)numericUpDown2.Value * Convert.ToDouble(label13.Text));
            }

        }

        private void tabPage4_Click(object sender, EventArgs e)
        {
            StreamWriter sw = new StreamWriter("C:\\Test.txt");
            sw.WriteLine(label14.Text);
            sw.WriteLine(label15.Text);
            sw.WriteLine(label16.Text);
            sw.WriteLine(label21.Text);
            sw.WriteLine(label22.Text);
            sw.WriteLine("Save clear");
            sw.Close();
        }
    }
}

//Сдвигать У картинку так, чтобы изохромы были друг под другом - по поиску центра масс
//Чтобы прога реагировала не только на красный цвет +
//Отслеживать появление новых волн и сообщать об этом
//показывать смещение графичеки
//Строить трехмерный график
//Объединение волн - это косяк
//сделать чтение по фоткам
//Анализить и еще левые волны и мат. находить коэфиценты гипербол +-
//Для успешного трекинга нужно по крайней мере 2 успешно сопровождаемых изохромы +
