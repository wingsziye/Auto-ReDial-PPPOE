using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotRas;
using DotRas.Design;
using System.Threading;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace MetroAutoDialNet4
{
    public class DialManager
    {
        public event Action<string> DialgLostConnection;
        public event Action<string> StopListenen;
        public void Diag(string entryName)
        {
            //cmb_ActiveConnectionList = cmb;
            try
            {
                RasDialer dialer = new RasDialer();
                dialer.EntryName = entryName;
                //dialer.PhoneNumber = " ";
                dialer.AllowUseStoredCredentials = true;
                dialer.PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers);
                dialer.Timeout = 1000;
                dialer.Dial();

                Thread.Sleep(100);
                //this.LoadConnections();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Disconnect(string entryName)
        {
            ReadOnlyCollection<RasConnection> conList = RasConnection.GetActiveConnections();
            foreach (RasConnection con in conList)
            {
                Console.WriteLine(con.EntryName);
                if (con.EntryName == entryName)
                {
                    con.HangUp();
                }
            }
        }

        public string[] ListActiveConnName()
        {
            ReadOnlyCollection<RasConnection> conList = RasConnection.GetActiveConnections();
            List<string> s = new List<string>();
            DateTime date = DateTime.Now;
            foreach (RasConnection con in conList)
            {
                //Console.WriteLine(date + ":" + con.EntryName);
                s.Add(con.EntryName);
            }
            return s.ToArray();
        }

        #region 定时器方法
        /*
        DialThreadParam timeDtp = new DialThreadParam();
        public void TimeStartListen( int intervalMillSecond, string listenEntryName)
        {
            timeDtp.IntervalMilliSecTime = intervalMillSecond;
            timeDtp.ListenEntryName = listenEntryName;
            Timer t = new Timer((object a)=> {  });
        }
        */
        #endregion

        #region 线程方法
        bool cnaListen;

        public bool CnaListen
        {
            get
            {
                return cnaListen;
            }

            set
            {
                cnaListen = value;
            }
        }

        public string CanListenCheck(string listenEntryName)
        {
            bool canListen = false;
            var conList = RasConnection.GetActiveConnections();
            foreach (var item in conList)
            {
                if (item.EntryName == listenEntryName)
                {
                    canListen = true;
                }
            }
            if (canListen)
            {
                Console.WriteLine("+1");
                CnaListen = true;
                return "已找到: \"" + listenEntryName + "\" 任务开始...";
            }
            else
            {
                CnaListen = false;
                Console.WriteLine("-1");
                return "未找到活动的拨号连接: " + listenEntryName;
            }
        }

        ThreadWorkClass workclass;
        public void StartListen(int intervalMilliSecTime, string listenName)
        {            
            workclass = new ThreadWorkClass();
            workclass.intervalMilliSecTime = intervalMilliSecTime;
            workclass.listenEntryName = listenName;
            workclass.DisConnectCalback = new ThreadWorkClass.ThreadCallBackDelegate(ThreadCallback);
            workclass.StopConnectCalback = new ThreadWorkClass.ThreadCallBackDelegate(StopThreadCallback);
            Thread th = new Thread(ThreadWork);
            th.Start(workclass);//开始线程，代入参数
        }

        private void ThreadWork(object obj)
        {
            var workcalss = (ThreadWorkClass)obj;
            
            while (true)
            {
                if (workcalss.isStopThread)
                {
                    break;//break退出while循环
                }
                Thread.Sleep(workcalss.intervalMilliSecTime);
                bool state = false;//fasle = Disconn; true = Alive;
                var conList = RasConnection.GetActiveConnections();
                //Console.WriteLine(DateTime.Now);
                for (int i = 0; i < conList.Count; i++)
                {
                    if (conList[i].EntryName == workcalss.listenEntryName)
                    {
                        state = true;
                        //break;
                    }
                }
                if (state == false)//如果没检测到连接，则表示已断开
                {
                    //break;
                    workcalss.DisConnectCalback(DateTime.Now+" : 检测到已断开，执行重连中...");
                }
            }
            workcalss.isStopThread = false;
            workcalss.StopConnectCalback("任务结束，已停止 \"" + workcalss.listenEntryName + "\" 的检测");
        }

        class ThreadWorkClass
        {
            public delegate void ThreadCallBackDelegate(string message);
            public ThreadCallBackDelegate DisConnectCalback;
            public ThreadCallBackDelegate StopConnectCalback;
            public int intervalMilliSecTime;
            public string listenEntryName;
            public bool isStopThread = false;
        }

        public void StopListen()
        {
            if (workclass!=null)
            {
                workclass.isStopThread = true;
            }
        }

        private void ThreadCallback(string message)
        {
            Console.WriteLine(message);
            if (this.DialgLostConnection!=null)
            {
                this.DialgLostConnection(message);
            }
        }

        private void StopThreadCallback(string message)
        {
            Console.WriteLine(message);            
            if (this.StopListenen!=null)
            {
                this.StopListenen(message);
            }
            
        }
        
        #endregion

        #region TaskTest
        /*
        DialThreadParam dtp = new DialThreadParam();


        public async Task<string> WaitAsynchronouslyAsync(int interval,string entryName)
        {
            //await Task.Delay(10000);
            dtp.ListenEntryName = entryName;
            dtp.IntervalMilliSecTime = interval;
            string result = await Task.Run(() => dtp.WaitDialGoOffline());
            return result;
        }
        */
        #endregion

    }

    public class DialThreadParam
    {
        int intervalMilliSecTime;
        public int IntervalMilliSecTime
        {
            get
            {
                return intervalMilliSecTime;
            }

            set
            {
                intervalMilliSecTime = value;
            }
        }
        private string listenEntryName;
        public string ListenEntryName
        {
            get
            {
                return listenEntryName;
            }

            set
            {
                listenEntryName = value;
            }
        }
        public string WaitDialGoOffline()
        {
            var tw = this;
            string retMessage = string.Empty;
            while (true)
            {
                Thread.Sleep(tw.IntervalMilliSecTime);
                bool state = false;//fasle = Disconn; true = Alive;
                var conList = RasConnection.GetActiveConnections();
                Console.WriteLine(DateTime.Now);
                for (int i = 0; i < conList.Count; i++)
                {
                    if (conList[i].EntryName == tw.ListenEntryName)
                    {
                        state = true;
                        break;
                    }
                }
                if (state == false)//如果没检测到连接，则表示已断开
                {
                    break;
                }
            }
            retMessage = tw.ListenEntryName + "断开咯";
            return retMessage;
        }



        public string tasktest()
        {
            Thread.Sleep(10000);
            return "a";
        }
    }
}
