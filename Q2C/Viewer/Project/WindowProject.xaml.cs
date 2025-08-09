using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Viewer.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for WindowSample.xaml
    /// </summary>
    public partial class WindowProject : Window
    {
        private UserControl _projectUC;
        public WindowProject()
        {
            InitializeComponent();
        }

        public void Load(UserControl projectUC, bool isUpdate = false, Q2C.Model.Project project = null)
        {
            Connection.Refresh_time = int.MinValue;
            _projectUC = projectUC;
            if (isUpdate)
                this.Title = "Q2C :: Edit Project";
            else
                this.Title = "Q2C :: Add Project";

            _Project.Load(this, project);
        }

        public void UpdateProjectDataGrid()
        {
            ((UCAllProjects)_projectUC).Filter();
            ((UCAllProjects)_projectUC).LoadComboFilter();
            ((UCAllProjects)_projectUC).UpdateProjectDataGrid();
        }

        internal void UpdateMethods()
        {
            _Project.UpdateMethods();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
