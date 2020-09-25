using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Timers;

namespace MindReading   //서버
{
    public delegate void setText(Control ctr, string text);
    public delegate void focus(Control ctr);
    public delegate void changeLabel(Control ctr, string text);
    public delegate void timer(Control ctr);

    public partial class Form1 : Form
    {
		private int drawMode = 1;
        private Point startPoint;   //시작 점
        private Point nowPoint; //현재 점
        public Pen myPen;      //펜
        public ArrayList saveData; //그림 객체 정보 저장
		public Pen eraser = new Pen(Color.White, 10);	

        public bool turn=true;	//차례
        public string[] answer = {
            "김소희", "김채원", "박은소리", "정크렛", "재크렛", "민크렛"
           ,"딸기","사과","파인애플","오렌지","키위","포도","스위트홈","인생"
          ,"노래방","청소용구함","가지","감자튀김","하회마을","고라니","라면","스파게티"
          ,"상어","치약","난로"
        };	//답 목록

        public Random random = new Random();//answer의 인덱스를 무작위로 바꿔줄 아름다운 객체
        public int index = 0;	//answer의 인덱스
        public int mycount = 60;	//타이머 시간
        public bool connected = false;	


        // 채팅 처리를 전담하는 Network 클래스 객체 변수 선언
        private Network net = null;
        private Thread server_th = null; // 채팅 서버 스레드 선언

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
			Graphics g = panel1.CreateGraphics();
            foreach (DrawData outData in saveData)
                outData.drawData(panel1.CreateGraphics());
        }

        protected override void OnNotifyMessage(Message m)	//그림을 그릴때 깜빡이는 현상을 줄이기 위한 메소드
        {
            if(m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
        public Form1()
        {
            InitializeComponent();
            //textBox1.Focus();
			myPen = new Pen(Color.Black);
			myPen.Width = 2;
			saveData = new ArrayList();

			net = new Network(this); // 네트워크 클래스 객체 생성
		}
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
			{
				//서버로 수행 중
				net.ServerStop(); //채팅 서버 실행 중지
				if (server_th.IsAlive) server_th.Abort(); //ServerStart 스레드를 종료
			 }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); //예외 메시지 출력
            }
        }

        private void button1_Click(object sender, EventArgs e)	//연결했을때
        {
            // 채팅 서버를 시작시키는 스레드를 생성
            if (textBox3.Text == "")
            {
                MessageBox.Show("닉네임을 입력해 주세요!");
            }
            else
            {
                label1.Text = textBox3.Text;
                server_th = new Thread(new ThreadStart(net.ServerStart));
                server_th.Start(); // 채팅 서버 시작

                textBox3.Enabled = false;
            }
        }

        public void Add_MSG(string msg)
        {
            setTextToControl(richTextBox1, msg);
            setFocus(textBox1);
        }
		public void setTextToControl(Control ctr, string text)
		{
			if (ctr.InvokeRequired)
			{
				setText st = new setText(setTextToControl);
				ctr.Invoke(st, ctr, text);
			}
			else
			{
				RichTextBox rt = (RichTextBox)ctr;
				rt.Focus();
				//ctr.Text += "\n" + text;
				rt.AppendText("\r\n" + text);
				rt.ScrollToCaret();
			}
		}
		public void setFocus(Control ctr)
		{
			if (ctr.InvokeRequired)
			{
				focus st = new focus(setFocus);
				ctr.Invoke(st, ctr);
			}
			else
			{
				ctr.Focus();
			}

		}
		public void timer_start()
		{
			timer(this);
		}

		public void timer(Control ctr)
		{
			if (ctr.InvokeRequired)
			{
				timer st = new timer(timer);
				ctr.Invoke(st, ctr);
			}
			else
			{
				System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
				t.Enabled = true;
				t.Interval = 1000;
				t.Tick += new EventHandler(timer1_Tick);
				t.Start();
			}
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                string msg = textBox1.Text.Trim();
                Add_MSG("["+label1.Text+"] " + msg);
                net.Send("chat:"+msg);
                textBox1.Text = "";
            }

        }

        public void label2Change(string text)
        {
            changeLabel(label2, text);
        }

        public void changeLabel(Control ctr, string text)
        {
            if (ctr.InvokeRequired)
            {
                changeLabel st = new changeLabel(changeLabel);
                ctr.Invoke(st, ctr);
            }
            else
            {
                ctr.Text = text;
            }
        }

		private void Form1_Load(object sender, EventArgs e)
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			ResizeRedraw = true;
		}
		private void 연필_Click(object sender, EventArgs e)
		{
            this.Cursor = Cursors.Arrow;
            drawMode = 1;
			Invalidate();
			Update();
		}

		private void 색깔_Click(object sender, EventArgs e)
		{
            drawMode = 1;
			colorDialog1.ShowDialog();
			myPen.Color = colorDialog1.Color;
		}

		private void 지우개_Click(object sender, EventArgs e)
		{
            DialogResult result = MessageBox.Show("모든 그림을 지웁니다", "경고", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            { 
                saveData.Clear();
                net.Send("clear");
                panel1.Invalidate();
                panel1.Update();
            }
	
		}

		private void panel1_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || turn == false) return;
			nowPoint = new Point(e.X, e.Y);
			startPoint = nowPoint;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || turn == false) return;
			nowPoint = new Point(e.X, e.Y);
			Graphics g = panel1.CreateGraphics();
			Invalidate();
			Update();
            
			if (drawMode == 1)
				g.DrawLine(myPen, startPoint, nowPoint);
			if (drawMode == 2)
				g.DrawLine(eraser, startPoint, nowPoint);
            //    if (e.X >= 195 && e.X <= 836 && e.Y >= 13 && e.Y <= 430)    //좌표 보내기
            //   {
            net.Send("paint:" + startPoint.X + ":" + startPoint.Y + ":" + nowPoint.X + ":" + nowPoint.Y + ":" + drawMode + ":" + myPen.Color.ToArgb().ToString());
            //     }
            DrawData inputData = new DrawData(startPoint, nowPoint, myPen,drawMode);
			saveData.Add(inputData);
			startPoint = nowPoint;
			g.Dispose();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (mycount == 0)
            {
                mycount = 60;
                turn = !turn;
                saveData.Clear();
                panel1.Invalidate();
                panel1.Update();
                MessageBox.Show("시간초과", "차례 뺏김ㅅㄱ");

                if (turn == true)
                {
                    index = random.Next(0,answer.Length-1);//0부터 answer의 마지막 인덱스까지 랜덤돌림
                    //그리고 index에 넣음
                    label5.Text = answer[index];
                }
                else
                    label5.Text = "";
            }
            else mycount--;
            label3.Text = "타이머 : " + mycount.ToString();
        }
    }
}
