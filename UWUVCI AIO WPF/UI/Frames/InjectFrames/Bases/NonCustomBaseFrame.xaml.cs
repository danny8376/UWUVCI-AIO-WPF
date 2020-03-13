﻿using GameBaseClassLibrary;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UWUVCI_AIO_WPF.Classes;


namespace UWUVCI_AIO_WPF.UI.Frames.InjectFrames.Bases
{
    /// <summary>
    /// Interaktionslogik für NonCustomBaseFrame.xaml
    /// </summary>
    public partial class NonCustomBaseFrame : Page
    {
        MainViewModel mvm;
        public NonCustomBaseFrame(GameBases Base, GameConsoles console, bool existing)
        {
            InitializeComponent();
            mvm = (MainViewModel)FindResource("mvm");
            if (!existing)
            {
                createConfig(Base, console);
            }
            
        }
       
        public NonCustomBaseFrame()
        {
            InitializeComponent();
            mvm = (MainViewModel)FindResource("mvm");
            
        }
        private void createConfig(GameBases Base, GameConsoles console)
        {
            mvm.GameConfiguration = new GameConfig();
            mvm.GameConfiguration.BaseRom = Base;
            mvm.GameConfiguration.Console = console;
        }
    }
}
