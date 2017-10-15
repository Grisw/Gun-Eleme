using JumpKick.HttpLib;
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
    /// OauthWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OauthWindow : Window {

        public OauthToken oauthToken { get; private set; }
        
        public OauthWindow(Window owner) {
            InitializeComponent();

            Owner = owner;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Clipboard.SetDataObject(url_TextBox.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            OauthToken.GetOauthTokens((result, token) => {
                Dispatcher.Invoke(() => {
                    if (result == 1) {
                        oauthToken = token;
                        DialogResult = true;
                        Close();
                    }
                });
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (DialogResult != true)
                Http.Get("http://close.local").Go();
        }
    }
}
