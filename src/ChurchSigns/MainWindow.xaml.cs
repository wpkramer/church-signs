using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ChurchSignsLib;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChurchSigns
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Class1 _class1;
        public MainWindow()
        {
            _class1 = new Class1(1, 2);
            InitializeComponent();
        }

        public string HookedUpMessage 
        {
            get
            {
                try
                {
                    if (_class1.Sum != 3)
                    {
                        return "We have a calculation problem??";
                    }
                   
                }
                catch (Exception ex)
                {
                    return $"{ex.GetType().Name} {ex.Message}";
                }
                return "We are hooked up!";
            }
        }
    }
}
