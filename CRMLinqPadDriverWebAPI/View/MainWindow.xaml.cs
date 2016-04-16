/*================================================================================================================================

  This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.  

  THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, 
  INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.  

  We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object 
  code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to market Your software 
  product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your software product in which the 
  Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims 
  or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.

 =================================================================================================================================*/

using LINQPad.Extensibility.DataContext;
using Microsoft.Pfe.Xrm.ViewModel;
using System.Diagnostics;
using System.Windows;

namespace Microsoft.Pfe.Xrm.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cxInfo">IConnectionInfo</param>
        /// <param name="isNewConnection">Indicate if this is new connection request or update existing connection</param>
        public MainWindow(IConnectionInfo cxInfo, bool isNewConnection)
        {
            InitializeComponent();
            // Instantiate ViewModel and pass parameters.
            vm = new MainWindowViewModel(cxInfo, isNewConnection);
            myGrid.DataContext = vm;
        }

        /// <summary>
        /// Close this window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Return true after data loaded, otherwise false (cancel)
            DialogResult = vm.IsLoaded;
        }

        /// <summary>
        /// Open browser to download CSDL file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }       
    }
}
