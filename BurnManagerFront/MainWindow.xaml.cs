using BurnManager;
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

using System.Collections.ObjectModel;


namespace BurnManagerFront
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BurnManagerAPI api;

        public string teststring { get; set; } = "Speegly noible";
        public ObservableCollection<string> test { get; set; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();
            api.TestState();
            DataContext = api.data.AllFiles;
            test.Add("adsf");
            test.Add("hngg");
            test.Add("blastoise");
        }
    }
}
