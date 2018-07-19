// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RightClickMenu.xaml.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin.Controls
{
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
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for RightClickMenu user control.
    /// </summary>
    /// 
    public partial class RightClickMenu : UserControl
    {
        public static string contractType = "this_week";

        //private readonly DataSource dataSource;
        private readonly IntPtr mainWnd;

       // public RightClickMenu(DataSource dataSource)
        public RightClickMenu(IntPtr pWnd)
        {
           // this.dataSource = dataSource;
            this.mainWnd = pWnd;
            this.InitializeComponent();
        }

        private void CopyDonateLTC(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("LT8ayo7g7QQxhE1tfkKqPNF2ymyfH5ZeVH");
        }

        private void CopyDonateBTC(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("1GTrU3SDshdnfFdHbVmaJwpd9w1hBpXLHv");
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {           
            NativeMethods.SendMessage(mainWnd, 0x0400 + 13000, IntPtr.Zero, IntPtr.Zero); 
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "CryptoCurrencies Data Plug-in (Demo) for AmiBroker.\n\n©2018 Arakcheev V.A.\n\ne-mail: arakcheev.v.a@gmail.com",
                "AmiBroker data plug-in",
                MessageBoxButton.OK);
        }
       
        private void GoToWebPage(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://amicoins.ru");
        }

        private void  SelectContractType(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton).Name)
            { 
                case "ctOne":
                    contractType = "this_week";
                    break;
            
                case "ctTwo":
                    contractType = "next_week";
                    break;

                case "ctThree":
                    contractType = "quarter";
                    break;
            }
            // Типа кликнули обновить
            Update_Click(null, null);
        }
    }
}
