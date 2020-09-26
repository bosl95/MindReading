using System;
using System.Net; //네트워크 처리
using System.Net.Sockets; // 소켓 처리
using System.Threading; // 스레드 처리
using System.Text; // 문자열 처리
using System.IO;
using System.Drawing;

namespace MindReading
{
    class Network
    {
        Form1 wnd = null;
        Thread th = null; //스레드 처리
                          // Ver 3.0에 추가된 부분
        TcpListener server = null; // 서버 소켓(접속을 받는 소켓)
        TcpClient client = null; // 통신 소켓
        NetworkStream stream = null; //네트워크 스트림
        StreamReader reader = null; //읽기 문자 스트림
        StreamWriter writer = null; //쓰기 문자 스트림
        int index = 0;  //

        delegate void CrossThreadSafetySetText(System.Windows.Forms.Control ctl, String text);


        private void CSafeSetText(System.Windows.Forms.Control ctl, String text)
        {

            /*
             * InvokeRequired 속성 (Control.InvokeRequired, MSDN)
             *   짧게 말해서, 이 컨트롤이 만들어진 스레드와 현재의 스레드가 달라서
             *   컨트롤에서 스레드를 만들어야 하는지를 나타내는 속성입니다.  
             * 
             * InvokeRequired 속성의 값이 참이면, 컨트롤에서 스레드를 만들어 텍스트를 변경하고,
             * 그렇지 않은 경우에는 그냥 변경해도 아무 오류가 없기 때문에 텍스트를 변경합니다.
             */
            if (ctl.InvokeRequired)
                ctl.Invoke(new CrossThreadSafetySetText(CSafeSetText), ctl, text);
            else
                ctl.Text = text;
        }

        public delegate void CrossThreadSafetyPanel(System.Windows.Forms.Panel panel);
        public void ControlToPanel(System.Windows.Forms.Panel panel)
        {
            if (panel.InvokeRequired)
            {
                panel.Invoke((System.Windows.Forms.MethodInvoker)delegate { ControlToPanel(panel); });
            }
            else
            {
                panel.Invalidate(); //<-- here is where the exception is raised
                panel.Update();
            }
        }

        public Network(Form1 wnd)
        { // 생성자
            this.wnd = wnd; //NetWork 클래스에서 Form1의 멤버 사용을 허용
        }

        public void ServerStart()
        {
            try
            { //서버 포트 번호를 7000번으로 지정
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 7000);
                server = new TcpListener(ipep);
                server.Start(); //채팅 서버 실행
               this.wnd.Add_MSG("채팅 서버 시작...");
                // 채팅 클라이언트가 접속하면 통신 소켓 반환
                client = server.AcceptTcpClient();

                if (client.Connected)
                {
                    if (wnd.turn == true)	//서버차례가 됐을때
                    {
                        index = wnd.random.Next(0,wnd.answer.Length-1);	//answer의 인덱스를 랜덤으로 받은것
                        //wnd.label5.Text = wnd.answer[index];	//문제에다가 answer의 인덱스(값)를 넣음
                        CSafeSetText(wnd.label5, wnd.answer[index]);
                    }
                    wnd.timer_start();	//타이머 시작(delegate), 델리게이트를 부르는 함수
                }
					stream = client.GetStream(); //통신 소켓에 대한 스트림 구하기
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);
                    th = new Thread(new ThreadStart(Receive));
                    th.Start();
                
            }
            catch (Exception ex) { wnd.Add_MSG(ex.Message); }
        }

        // 채팅 서버 중지
        public void ServerStop()
        {
            try
            {
                if (client != null)
                {
                    if (reader != null) reader.Close(); //streamreader 종료
                    if (writer != null) writer.Close(); //streamwriter 종료
                    if (stream != null) stream.Close(); //networkstream 종료
                    client.Close(); // 통신 소켓을 닫습니다.
                    if (th.IsAlive) th.Abort(); // Receive 스레드가 실행중이라면 종료
                    server.Stop(); //서버 소켓을 닫습니다.
                }
            }
            catch (Exception ex)
            { //예외 메시지 출력
                wnd.Add_MSG(ex.Message);
            }
        }


        public bool Connect(string ip)//접속(클라)
        {
            try
            { // 접속할 채팅 서버 ip 주소와 포트 번호를 지정
                client = new TcpClient(ip, 7000);
                this.wnd.Add_MSG(ip + "서버에 접속 성공...");
                // 채팅 서버 연결에 성공하면 송수신 네트워크 스트림 생성
                stream = client.GetStream();//서버 정보를 받음
                reader = new StreamReader(stream);//서버가 보낸 내용을 수신
                writer = new StreamWriter(stream);//클라가 쓴 내용을 서버에 송신
                th = new Thread(new ThreadStart(Receive));//받기 시작
                th.Start();
                return true; // 접속 성공하면 true 값을 반환
            }
            catch (Exception ex)
            { //채팅서버 접속에 실패하면 예외 메시지를 출력
                wnd.Add_MSG(ex.Message);
                return false; // 접속 실패했으면 false 값을 반환
            }
        }

        // 채팅 서버와 연결 종료
        public void DisConnect()
        {
            try
            {
                if (client != null)
                { // 채팅 서버와 연결 되어있다면
                    if (reader != null) reader.Close(); //streamreader 종료
                    if (writer != null) writer.Close(); //streamwriter 종료
                    if (stream != null) stream.Close(); //networkstream 종료
                    client.Close(); // 채팅서버와의 연결을 단절
                    if (th.IsAlive) th.Abort(); //Receive 메서드 스레드를 중지
                }
                wnd.Add_MSG("채팅 서버 연결 종료!");
            }
            //채팅 서버 연결 해제와 스레드 종료시 예외가 발생하면
            catch (Exception ex)
            {
                wnd.Add_MSG(ex.Message); // 예외 메시지 출력
            }
        }

        public void Receive()//서버 클라 공용
        {
            string msg = null; // 내가 받을 메세지 or 쓸 메세지    
            int w, x, y, z; //받을 그림의 좌표
            int draw;	//drawmode의 mode번호
            int rgb;	//색깔 받아오는 변수
            Color color;
            Pen newpen;

            try
            { //상대방과 연결되었다면

                string m = wnd.label1.Text;	//label1의 텍스트를 가져옴
                Send("nickname:" + m);	//nickname=temp[0]. m=temp[1]
                do
                {
                    msg = reader.ReadLine(); //라인 단위로 문자열 읽어오기

                    string[] temp = msg.Split(':');
                    
                    if (temp[0] == "nickname")
                    {
                        //wnd.label2.Text = temp[1];	//닉네임을 받음
                        CSafeSetText(wnd.label2, temp[1]);
                        //wnd.label2Change(temp[1]);
                    }
                    else if (temp[0] == "chat")
                    {
                       wnd.Add_MSG("["+wnd.label2.Text+"]"+temp[1]);	//이름+대화내용을 보냄
                        if (temp[1] == wnd.label5.Text)	//대화내용 == 답
                        {
                            msg = "정답입니다.";
                            wnd.Add_MSG(msg);
                            Send("answer:" + msg);

                            wnd.saveData.Clear();		//그림판 청소
                            CSafeSetText(wnd.label5, "");
                            CSafeSetText(wnd.panel1, "panel");
                            ControlToPanel(wnd.panel1);
                            wnd.mycount = 60;		//타이머 다시 60초로 바꿈(초기화)
                            wnd.turn = !wnd.turn;	//차례를 바꿈
                        }

                    }
					//아예 판을 바꿈. 상대팀에서 "정답입니다"를 받았을때
                    else if (temp[0] == "answer")//"정답입니다"를 받으면
                    {
                        wnd.Add_MSG(temp[1]);	//내 메세지창에도 정답입니다를 씀
                        wnd.saveData.Clear();	//그림판 청소
                        CSafeSetText(wnd.panel1, "panel");
                        wnd.mycount = 60;	//카운트 초기화
                        wnd.turn = !wnd.turn;//차례를 바꿈
                        if (wnd.turn == true)	//다음 차례로 바꿈
                        {
                            index = wnd.random.Next(0, wnd.answer.Length - 1);
                            //인덱스를 랜덤으로 돌리고
                            //wnd.label5.Text = wnd.answer[index];
                            CSafeSetText(wnd.label5, wnd.answer[index]);
                            //그 인덱스에 있는 answer을 문제로 냄
                        }

                    }
                    else if (temp[0] == "paint")
                    {
                       // wnd.Add_MSG(temp[1] + ", " + temp[2]);    //좌표받기

                        w = Int32.Parse(temp[1]);   //받은 그림 x좌표(start)
                        x = Int32.Parse(temp[2]);   //받은 그림 y좌표(start)
                        y = Int32.Parse(temp[3]);   //받은 그림 x좌표(end)
                        z = Int32.Parse(temp[4]);   //받은 그림 y좌표(end)
                        draw = Int32.Parse(temp[5]);	//drawMode값, DrawData에서 불러오려고

                        rgb = Int32.Parse(temp[6]);	//RGB를 받아줌
                        color = Color.FromArgb(rgb);	//RGB를 받은 색
                        newpen = new Pen(color,2);	//위에(RGB로 받은 색) 색으로 만든 펜
                        Point start = new Point(w, x); //마우스 클릭시 시작 포인트
                        Point end = new Point(y, z); // 마우스 무브시 엔드포인트
                        DrawData drawdata = new DrawData(start, end, newpen,draw);	//DrawData의 함수 drawData로 받아줌
                        drawdata.drawData(wnd.panel1.CreateGraphics());

                    }

                } while (msg != null);
            }
            catch (Exception ex) { wnd.Add_MSG(ex.Message); }
        }

        public void Send(string msg)
        {
            try
            {
                writer.WriteLine(msg); //문자열 메시지 전송
                writer.Flush();
            }
            catch (Exception ex) { wnd.Add_MSG(ex.Message); }
        }
    }
}