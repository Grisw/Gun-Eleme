using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gun_Eleme {
    /// <summary>
    /// ChooseUserWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChooseUserWindow : Window {

        public OauthToken Token { get; private set; }

        public ChooseUserWindow(Window owner) {
            InitializeComponent();

            Owner = owner;
        }

        private void refresh() {
            tokenList.Items.Clear();
            List<string> list = OauthToken.Load();
            foreach(string s in list) {
                ListBoxItem item = new ListBoxItem();
                item.Content = s;
                item.MouseDoubleClick += Item_MouseDoubleClick;
                tokenList.Items.Add(item);
            }
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Token = OauthToken.Load((string)((ListBoxItem)sender).Content);
            DialogResult = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            OauthWindow window = new OauthWindow(this);
            if (window.ShowDialog() == true) {
                window.oauthToken.Save();
                refresh();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            refresh();
        }
    }
}
