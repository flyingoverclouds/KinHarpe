using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinHarpe2.ViewModels
{
    public class KinHarpeMainViewModel : BaseViewModel
    {

        public KinHarpeMainViewModel()
        {
            this.Status = "";

            this.drawingGroup = new DrawingGroup(); // used to draw in butmap
            this.drawingImageSource= new DrawingImage(this.drawingGroup); // the image source for rendering in image controle
        }


        private string status;

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChange("Status");
            }
        }

        private DrawingGroup drawingGroup;
        private DrawingImage drawingImageSource;
        public DrawingImage DrawingImageSource
        {
            get { return drawingImageSource; }
            internal set
            {
                drawingImageSource = value;
                RaisePropertyChange("DrawingImageSource");
            }
        }


        private WriteableBitmap videoImageSource;
        public WriteableBitmap VideoImageSource
        {
            get
            {
                return this.videoImageSource;
            }
            internal set
            {
                this.videoImageSource = value;
                RaisePropertyChange("VideoImageSource");
            }
        }

        private double depthImageOpacity = 0;
        public double DepthImageOpacity
        {
            get { return depthImageOpacity; }
            set { depthImageOpacity = value; RaisePropertyChange("DepthImageOpacity"); }
        }


        private WriteableBitmap depthImageSource;
        public WriteableBitmap DepthImageSource
        {
            get
            {
                return this.depthImageSource;
            }
            internal set
            {
                this.depthImageSource = value;
                RaisePropertyChange("DepthImageSource");
            }
        }


        private SolidColorBrush leftHandColor = new SolidColorBrush(Colors.Gray);
        public SolidColorBrush LeftHandColor
        {
            get { return leftHandColor; }
            set { leftHandColor = value; RaisePropertyChange("LeftHandColor"); }
        }

        private SolidColorBrush rightHandColor = new SolidColorBrush(Colors.Gray);
        public SolidColorBrush RightHandColor
        {
            get { return rightHandColor; }
            set { rightHandColor = value; RaisePropertyChange("RightHandColor"); }
        }

       


        private double rightHandY = 1080;
        public double RightHandY
        {
            get { return rightHandY; }
            set { rightHandY = value; RaisePropertyChange("RightHandY");}
        }

         private double rightHandX = 1160;
        public double RightHandX
        {
            get { return rightHandX; }
            set { rightHandX = value; RaisePropertyChange("RightHandX"); }
        }

        private double leftHandY = 1080;
        public double LeftHandY
        {
            get { return leftHandY; }
            set { leftHandY = value; RaisePropertyChange("LeftHandY"); }
        }

        private double leftHandX = 660;
        public double LeftHandX
        {
            get { return leftHandX; }
            set { leftHandX = value; RaisePropertyChange("LeftHandX"); }
        }

        public string RightHandTorsoDistance
        {
            get { return rightHandTorsoDistance; }
            set { rightHandTorsoDistance = value; RaisePropertyChange("RightHandTorsoDistance");}
        }

        public string LeftHandTorsoDistance
        {
            get { return leftHandTorsoDistance; }
            set { leftHandTorsoDistance = value; RaisePropertyChange("LeftHandTorsoDistance"); }
        }

        public string LeftHandStatus
        {
            get { return leftHandStatus; }
            set { leftHandStatus = value; RaisePropertyChange("LeftHandStatus"); }
        }

        public string RightHandStatus
        {
            get { return rightHandStatus; }
            set { rightHandStatus = value; RaisePropertyChange("RightHandStatus"); }
        }

        private string leftHandStatus = string.Empty;
        private string rightHandStatus = string.Empty;
        private string leftHandTorsoDistance = string.Empty;
        private string rightHandTorsoDistance = string.Empty;
    }
}
