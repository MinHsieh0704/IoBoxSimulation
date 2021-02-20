using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IoBoxSimulation
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeState();
        }

        /// <summary>
        /// Initialize default state
        /// </summary>
        private void InitializeState()
        {
            rb_di0_off.IsChecked = true;
            rb_di1_off.IsChecked = true;
            rb_di2_off.IsChecked = true;
            rb_di3_off.IsChecked = true;
        }

        private void btn_action_Click(object sender, RoutedEventArgs e)
        {
            if (btn_action.Content?.ToString() == "open")
            {
                btn_action.Content = "close";
            } 
            else
            {
                btn_action.Content = "open";
            }
        }

        private void input_port_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // validate number only
                Regex regex = new Regex("^[0-9]+$");
                e.Handled = !regex.IsMatch(e.Text);

                if (e.Handled == true) return;

                // validate number range 1 - 65535
                int min = 1;
                int max = 65535;

                int input = int.Parse(input_port.Text + e.Text);

                e.Handled = !(input >= min && input <= max);
            }
            catch (Exception)
            {
                e.Handled = true;
            }

        }

        private void input_di_count_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // validate number range 2 - 4
                Regex regex = new Regex("^[2-4]+$");
                e.Handled = !regex.IsMatch(e.Text);           
            }
            catch (Exception)
            {
                e.Handled = true;
            }
        }

        private void input_di_count_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
