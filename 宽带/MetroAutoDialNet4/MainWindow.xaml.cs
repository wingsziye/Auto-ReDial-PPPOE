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
                InputMessage("重连\""+SelDialName+"\"完成");
            }));
        }

        private void InputMessage(string message)
        {
            if (UserMessageList != null)
            {
                if (UserMessageList.Count > 0)
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
                InputMessage("已选择 \""+ "\""+ SelDialName);
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
            if (text!=string.Empty)
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
    }

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
}
