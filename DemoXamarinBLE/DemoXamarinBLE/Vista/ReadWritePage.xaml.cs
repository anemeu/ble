using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DemoXamarinBLE.Vista
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReadWritePage : ContentPage
    {
        public ReadWritePage()
        {
            InitializeComponent();
        }

        public ReadWritePage(VistaModelo.VistaModeloBLE vmBle)
        {
            InitializeComponent();
            BindingContext = vmBle;
        }
    }
}