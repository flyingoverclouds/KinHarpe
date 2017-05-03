using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinHarpe2.ViewModels
{
    /// <summary>
    /// Base view model implementation
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {

        protected void RaisePropertyChange(string propertyName)
        {
            if (PropertyChanged!=null)
                PropertyChanged(this,new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
