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
using System.Windows.Media.Animation;

namespace KinHarpe
{
    /// <summary>
    /// Interaction logic for LaserRay.xaml
    /// </summary>
    public partial class LaserRay : UserControl
    {
        public LaserRay()
        {
            InitializeComponent();
        }

        double savedHeight = 0;
        Storyboard sbTouched = null;
        Storyboard sbUntouched = null;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            sbTouched = FindResource("sbLaserTouched") as Storyboard;
            sbUntouched = FindResource("sbLaserUntouched") as Storyboard;
            savedHeight = this.Height;
        }

        public void Touched(double x, double y)
        {
            this.Height= 470;
            sbTouched.Begin();
        }

        public void Untouched()
        {
            sbUntouched.Begin();
            this.Height = savedHeight;
        }

    }
}
