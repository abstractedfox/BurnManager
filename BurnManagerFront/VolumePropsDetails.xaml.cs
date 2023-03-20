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

namespace BurnManagerFront
{
    /// <summary>
    /// Interaction logic for VolumePropsDetails.xaml
    /// </summary>
    public partial class VolumePropsDetails : Window
    {
        private MainWindow mainWindow;
        private VolumeProps thisVolumeProps;

        public VolumePropsDetails(MainWindow mainWindowReference)
        {
            InitializeComponent();
            mainWindow = mainWindowReference;
        }

        public void SetVolumeProps(BurnManager.VolumeProps props)
        {
            thisVolumeProps = props;
            VolumePropsDetails_VolumePropsNameLabel.Content = thisVolumeProps.Name;
            RefreshListBox();
        }

        private async void VolumePropsDetails_RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            lock (mainWindow.LockObj)
            {
                List<BurnManager.FileProps> files = new List<BurnManager.FileProps>();
                var items = listBox.SelectedItems;
                foreach (BurnManager.FileProps item in items) files.Add(item);

                foreach (var file in files)
                {
                    if (!thisVolumeProps.CascadeRemove(file))
                    {
                        System.Windows.MessageBox.Show("Error removing " + file.FileName + " from " + thisVolumeProps.Name);
                    }
                }
                RefreshListBox();
            }
        }

        public void RefreshListBox()
        {
            lock (mainWindow.LockObj)
            {
                listBox.ItemsSource = thisVolumeProps.Files.Select(a => a);
            }
        }
    }
}
