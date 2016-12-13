using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace MetroAutoDialNet4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        #region 最小化到右下角

        /// <summary>
        /// 托盘小图标的双击事件--最小化的状态下双击还原
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotificationAreaIcon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine(this.WindowState);
            if (this.WindowState == WindowState.Minimized)
            {
                this.Show();
                WindowState = WindowState.Normal;
            }
        }


        private void mainwindow_StateChanged(object sender, EventArgs e)
        {
            //Console.WriteLine(this.WindowState);
            if (this.WindowState == WindowState.Normal || this.WindowState == WindowState.Maximized)
            {
                //this.Show();
            }
            else if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }
        #endregion

        #region 拨号控制
        ObservableCollection<UserMessage> UserMessageList;
        DialManager dialManager;
        ComboBoxItem cmbi_default;
        string selDialName;
        int intervalSecond;

        public string SelDialName
        {
            get
            {
                return selDialName;
            }

            set
            {
                selDialName = value;
                //Console.WriteLine(value);
            }
        }

        public int IntervalSecond
        {
            get
            {
                return intervalSecond;
            }

            set
            {
                intervalSecond = value;
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UserMessageList = new ObservableCollection<UserMessage>();
            lv_msgview.ItemsSource = UserMessageList;
            dialManager = new DialManager();
            dialManager.DialgLostConnection += DialManager_DialgLostConnection;
            dialManager.StopListenen += DialManager_StopListenen;
            cmbi_default = cmb_linklist.Items[0] as ComboBoxItem;

            #region 最小化到右下角
            //notificationIcon.Icon = new BitmapImage(new Uri("Resource\\system_monitor.png", UriKind.RelativeOrAbsolute));
            this.ShowInTaskbar = false;//启动后不现实任务栏图标！
            #endregion

        }

        private void DialManager_StopListenen(string obj)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                InputMessage(obj);
            }));
        }

        private void DialManager_DialgLostConnection(string obj)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                InputMessage(obj);
                invokeDial();
                InputMessage("重连\"" + SelDialName + "\"完成");
            }));
        }

        private void InputMessage(string message)
        {
            if (UserMessageList != null)
            {
                if (UserMessageList.Count > 1000)//发现BUG，有一回网线断了，然后不停重连，导致列表太长，界面会卡死。
                {
                    UserMessageList.Clear();
                }
                else if (UserMessageList.Count > 350)//临时解决方案，最好的方法是检测是否频繁断线
                {
                    //当消息太多时，自动降低扫描速度为1分钟一次
                    btn_stopListen_Click(null, null);
                    IntervalSecond = 60000;
                    tbx_interval.Text = IntervalSecond.ToString();
                    btn_startListen_Click(null, null);
                }
                else if (UserMessageList.Count > 0)
                {
                    if (UserMessageList[UserMessageList.Count - 1].Numbers != UserMessageList.Count)
                    {
                        flashNumbers(UserMessageList);//当列表序号不对时，刷新列表顺序
                    }
                }
                UserMessageList.Add(new UserMessage() { Numbers = UserMessageList.Count + 1, Message = message });
                lv_msgview.ScrollIntoView(lv_msgview.Items[lv_msgview.Items.Count - 1]);
            }
        }

        private void deleteMessage(int startindex)
        {
            if (UserMessageList != null)
            {
                UserMessageList.RemoveAt(startindex);
            }
        }

        private async void flashNumbers(ObservableCollection<UserMessage> uc)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < uc.Count; i++)
                {
                    uc[i].Numbers = i + 1;
                    Console.WriteLine("线程中");
                }
            });
        }

        private void btn_stopListen_Click(object sender, RoutedEventArgs e)
        {
            InputMessage("正在停止..请稍后...");
            dialManager.StopListen();
            #region 启用按钮
            btn_dial.IsEnabled = true;
            btn_flashMsg.IsEnabled = true;
            #endregion
        }

        private void btn_startListen_Click(object sender, RoutedEventArgs e)
        {
            #region Task方法,只能执行一次
            /*
            InputMessage(dialManager.CanListenCheck(SelDialName));
            if (dialManager.CnaListen)
            {
                var result = await dialManager.WaitAsynchronouslyAsync(IntervalSecond, SelDialName);
                InputMessage(result);
            }
            */
            #endregion

            #region 禁用按钮
            btn_dial.IsEnabled = false;
            btn_flashMsg.IsEnabled = false;
            #endregion

            #region 线程while方法
            InputMessage(dialManager.CanListenCheck(SelDialName));
            if (dialManager.CnaListen)
            {
                dialManager.StartListen(IntervalSecond, SelDialName);
            }
            #endregion

        }

        private void invokeDial()
        {
            InputMessage("正在尝试对 \"" + SelDialName + "\" 进行拨号...");

            dialManager.Diag(SelDialName);
        }

        private void btn_dial_Click(object sender, RoutedEventArgs e)
        {
            if (SelDialName != "" && SelDialName != null)
            {
                dialManager.Diag(SelDialName);
            }
        }

        private void btn_flashMsg_Click(object sender, RoutedEventArgs e)
        {
            UserMessageList.Clear();
            clearcmb_linkList();

            var items = dialManager.ListActiveConnName();
            foreach (var item in items)
            {
                cmb_linklist.Items.Add(item);
            }
        }

        private void clearcmb_linkList()
        {
            cmb_linklist.Items.Clear();
            cmb_linklist.Items.Add(cmbi_default);
            cmb_linklist.SelectedIndex = 0;
        }

        private void cmb_linklist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox a = sender as ComboBox;
            if (a.SelectedIndex == 0)
            {
                SelDialName = tbx_manulinput.Text;
                InputMessage("已选择手动输入...");
            }
            else if (a.SelectedIndex > 0)
            {
                SelDialName = a.Items[a.SelectedIndex].ToString();
                InputMessage("已选择 \"" + "\"" + SelDialName);
            }
        }

        private void tbx_manulinput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cmb_linklist.SelectedIndex == 0)
            {
                SelDialName = ((TextBox)sender).Text;
            }
        }

        private void tbx_interval_TextChanged(object sender, TextChangedEventArgs e)
        {
            var t = (TextBox)sender;
            string text = t.Text;
            int a = 5000;
            if (text != string.Empty)
            {
                try
                {
                    a = int.Parse(text);
                }
                catch (Exception)
                {
                    //t.Text = t.Text.Remove(t.Text.Length - 1, 1);
                    t.Text = a.ToString();
                }
            }
            IntervalSecond = a;
        }

        #endregion

        private async void btn_iconBtn_Click(object sender, RoutedEventArgs e)
        {
            var controller = await this.ShowProgressAsync("哈哈，你发现了这只喵~", "稍等几秒会自己消失....\n                      ——by 梓夜");

            await waitProgress(controller);
            
        }

        private async Task waitProgress(ProgressDialogController controller)
        {
            await Task.Run(async () =>
            {
               
                for (int i = 0; i < 120; i++)
                {
                    await Task.Delay(20);
                    if (i==40)
                    {
                        controller.SetMessage("哈哈~\n                      ——by 梓夜");
                    }
                    else if (i==80)
                    {
                        controller.SetMessage("再见!\n                      ——by 梓夜");
                    }
                }
            });
            //controller.Maximum = 100;
            await controller.CloseAsync();
        }
    }

    #region 显示数据
    public class UserMessage : INotifyPropertyChanged
    {
        int numbers;

        public int Numbers
        {
            get
            {
                return numbers;
            }

            set
            {
                numbers = value;
                OnPropertyChanged(nameof(Numbers));
            }
        }

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        string message;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string Name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
            }
        }
    }
    #endregion

}
