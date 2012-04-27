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

//JCB - Added
using System.Xml.Linq;
using System.Drawing;
using System.IO;
using LevelEditor.Classes;
using LevelEditor.Helpers;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Net;

namespace LevelEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //constants
        //naming convention: http://stackoverflow.com/questions/242534/c-sharp-naming-convention-for-constants
         private const string MeshFileExtension = ".mesh";
     
        //Member Variables
        OgreForm _ogre;
        List<string> _fileSystemPaths = new List<string>();
        string _currentEntity = string.Empty; // = "knot.mesh";
        float _currentZoom = (float)1.0;
        System.Drawing.Point _startPoint = new System.Drawing.Point();
        int diffX, diffY, startX, startY;

        //These two search counters keep track of what item is selected. One is incremented each time an item is found and selected and
        //the other is incremented each time an item is found. Once they equal each other then a new item is selected.
        int searchCounter = 0;
        int localSearchCounter = 0;

        public MainWindow()
        {
            InitializeComponent();
            GenerateTreeViewAndResourceConfigurationPaths("..\\..\\EditorData", true);
            InitializeOgre();
               _ogre.RenderEnvironment();
            SetupEventTimer();
        }

        #region SETUP CALLS

        /// <summary>
        /// Sends an update to Ogre
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RenderTick(object sender, EventArgs e)
        {
            if (!_ogre.Tick(diffX, diffY, startX.ToString(), startY.ToString(), searchCounter))
            {
                //todo - some animation stuff would go here if I wanted to do object's idle animation
            }
        }

        /// <summary>
        /// Call Ogre constructer / init. Tell Ogre what WinForms panel to render to.
        /// </summary>
        /// <param name="startingMesh"></param>
        void InitializeOgre()
        {
            _ogre = new OgreForm(_fileSystemPaths);
            System.Windows.Forms.Panel pan = OgreWinFormsHost.Child as System.Windows.Forms.Panel;
            _ogre.Init(pan.Handle.ToString());
        }

        void SetupEventTimer()
        {
            //Create Rendering Timer (?) - Research/Comment this better
            System.Windows.Threading.DispatcherTimer RenderTimer = new System.Windows.Threading.DispatcherTimer();
            RenderTimer.Tick += new EventHandler(RenderTick);
            RenderTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            RenderTimer.Start();
        }

        /// <summary>
        /// Loads Tab data from config file
        /// </summary>


        /// <summary>
        /// This generates the default tab's TreeView
        /// </summary>
        void GenerateTreeViewAndResourceConfigurationPaths(string rootFolderPath, bool searchSubfolders)
        {
            FileTreeview.Items.Add(OpenFolder(rootFolderPath, null, searchSubfolders));
        }

        /// <summary>
        /// This recusively traverses folders/files and creates a TreeView. The header text is the file name. The
        /// tag contains the FileDetailInformation for that file (basically the information that Ogre needs).
        /// </summary>
        /// <param name="currentDirectory"></param>
        /// <param name="parentItem"></param>
        /// <returns></returns>
        System.Windows.Controls.TreeViewItem OpenFolder(string currentDirectory, System.Windows.Controls.TreeViewItem parentItem, bool searchSubfolders)
        {
            if (string.IsNullOrWhiteSpace(currentDirectory))
                return null;

            System.Windows.Controls.TreeViewItem treeViewItem = new System.Windows.Controls.TreeViewItem();
            treeViewItem.Header = currentDirectory.Substring(currentDirectory.LastIndexOf("\\") + 1);
            treeViewItem.Tag = currentDirectory;

            //Iterate child folders
            if (searchSubfolders)
            {
                IEnumerable<string> subFolders = System.IO.Directory.EnumerateDirectories(currentDirectory); //todo - need to put in a try catch so that invalid directories don't cause errors
                foreach (string folder in subFolders)
                {
                    OpenFolder(folder, treeViewItem, searchSubfolders);
                }
            }

            if (parentItem != null)
                parentItem.Items.Add(treeViewItem);

            _fileSystemPaths.Add(currentDirectory);

            //Iterate child files
            IEnumerable<string> testFiles = System.IO.Directory.EnumerateFiles(currentDirectory);
            foreach (string file in testFiles)
                treeViewItem.Items.Add(CreateTreeViewFile(file));

            return treeViewItem;
        }

        /// <summary>
        /// This function builds up the FileDetailInformation required by Ogre.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private TreeViewItem CreateTreeViewFile(string file)
        {
            TreeViewItem fileItem = new TreeViewItem();
            fileItem.Header = file.Substring(file.LastIndexOf("\\") + 1);

            if (fileItem.Header.ToString().EndsWith(MeshFileExtension))
            {
                List<string> subMeshes = new List<string>();
                List<string> defaultMaterials = new List<string>();
                List<string> meshDefinitions = new List<string>();
            }
            return fileItem;
           }
        #endregion

        #region PREVIEW PANEL CONTROLS
        private void Panel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Get the current mouse position
            System.Drawing.Point mousePos = e.Location;
            diffX = _startPoint.X - mousePos.X;
            diffY = _startPoint.Y - mousePos.Y;
            _startPoint.X = mousePos.X;
            _startPoint.Y = mousePos.Y;

            if (e.Button == MouseButtons.Right)
            {
                //Make sure there isn't a big jump, like when the cursor exits/enters panel
                if (Math.Abs(diffY) < 100)
                    _ogre.rotateUp(-diffY);
            }
        }
        private void Panel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {            
            if (e.Button == MouseButtons.Left)
                _ogre.LeftClicked((float)(e.X / OgreWinFormsHost.ActualWidth),(float)( e.Y / OgreWinFormsHost.ActualHeight), _currentEntity);

            _startPoint = e.Location;
            startX = _startPoint.X;
            startY = _startPoint.Y;
        }
        #endregion

        #region CHANGE MODEL/MATERIAL SCENE DATA

        private void FileTreeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            System.Windows.Controls.TreeViewItem selectedTreeViewItem = ((System.Windows.Controls.TreeViewItem)FileTreeview.SelectedItem);

            //todo - imrpove logic
            if (selectedTreeViewItem == null)
                return;

            TreeViewItem parentItem = null;
            if (selectedTreeViewItem.Parent.GetType().Name == "TreeViewItem")
                parentItem = (TreeViewItem)selectedTreeViewItem.Parent;

            if (selectedTreeViewItem.Header.ToString().EndsWith(MeshFileExtension))
                ChangeMesh(selectedTreeViewItem);
            else
                return;

            return;
        }

        private void ChangeMesh(TreeViewItem selectedItem)
        {
            _currentEntity = selectedItem.Header.ToString();
         }
        #endregion

        /// <summary>
        /// When the app closes it writes the tabs and their information to a config file to be
        /// loaded up in the future.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*
            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement rootNode = new XElement("configuration");
            XElement tabsNode = new XElement("tabs");

            foreach (System.Windows.Controls.TabItem tab in TabContainer.Items)
            {
                try
                {
                    if (tab.Header.ToString() == NewTabHeaderText || tab.Header.ToString() == DefaultTabHeaderText)
                        continue;

                    XElement tabNode = new XElement("tab");
                    XElement searchPaths = new XElement("searchPaths");
                    XElement extensionsToSearch = new XElement("extensions");

                    tabNode.Add(new XElement("name", tab.Header.ToString()));
                    tabNode.Add(new XElement("mapped", ((SearchTabs)tab.Tag).isMappedToSourceControl.ToString()));
                    //tabNode.Add(new XElement("mappedLocalFolder", ((SearchTabs)tab.Tag).mappedLocalFolder));
                    tabNode.Add(new XElement("mappedServerFolder", ((SearchTabs)tab.Tag).mappedServerFolder));
                    tabNode.Add(new XElement("preserveFolderHeirarchy", ((SearchTabs)tab.Tag).preserveFolderHeirarchy));
                    tabNode.Add(new XElement("localRootFolder", ((SearchTabs)tab.Tag).localRootFolder));
                    tabNode.Add(new XElement("searchSubfolders", ((SearchTabs)tab.Tag).searchSubfolders));

                    if (tab.Tag != null)
                    {
                        foreach (string searchPath in ((SearchTabs)tab.Tag).searchPaths)
                            searchPaths.Add(new XElement("searchPath", searchPath));

                        foreach (string extension in ((SearchTabs)tab.Tag).searchFileExtensions)
                            extensionsToSearch.Add(new XElement("extension", extension));
                    }
                    tabNode.Add(searchPaths);
                    tabNode.Add(extensionsToSearch);
                    tabsNode.Add(tabNode);
                }
                catch (Exception ex)
                {
                    //Error in writing to file, log, continue on to other tabs
                }
            }
            rootNode.Add(tabsNode);
            doc.Add(rootNode);

            doc.Save(System.AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
            //doc.Save("E:\\Documents\\Visual Studio 2010\\Projects\\MogreWinForm\\WpfApplication1\\config1.xml");
             */
        }

        #region ZOOM
        /// <summary>
        /// Applies the scale of the object in the scene based on the delta and what is in the Zoom textbox.
        /// </summary>
        /// <param name="deltaZoom"></param>
        private void ApplyScale(int deltaZoom)
        {
            int scalePercentage;
            int.TryParse(ScaleText.Text.Replace('%', ' '), out scalePercentage);
            scalePercentage += deltaZoom;
            _currentZoom = (float)scalePercentage / 100;
            _ogre.Scale(_currentZoom, _currentZoom, _currentZoom);
            ScaleText.Text = scalePercentage.ToString() + " %";
        }
        private void ScaleText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)            
                ApplyScale(0);
        }


        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!SearchText.IsKeyboardFocusWithin)
            {
                if (e.Key == Key.O)
                    ApplyScale(-10);
                else if (e.Key == Key.P)
                    ApplyScale(10);
                else if (e.Key == Key.D)
                    _ogre.rotateRight(10);
                else if (e.Key == Key.W)
                    _ogre.move(10);
                else if (e.Key == Key.S)
                    _ogre.move(-10);
                else if (e.Key == Key.A)
                    _ogre.rotateLeft(10);
            }
        }
        #endregion

        #region SEARCHING
        /// <summary>
        /// Increments the global search counter and looks to see if there is enough matching items to match that counter
        /// </summary>
        private void SearchForward()
        {
            localSearchCounter = 1;
            searchCounter++;

            if (!FindAndSelectItemInTree(FileTreeview.Items, SearchText.Text))
            {
                if (searchCounter > 1)
                {
                    //If an item had been found previously, then reset counter and search forward again
                    searchCounter = 0;
                    SearchForward();
                }
                else
                    searchCounter = 0;
            }
        }
        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchCounter = 0;
            if (string.IsNullOrEmpty(SearchText.Text))
                SearchForwardButton.Opacity = .4;
            else
                SearchForwardButton.Opacity = 1;
        }
        private void SearchText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchForward();
        }
        private void SearchForwardButton_MouseDown(object sender, RoutedEventArgs e)
        {
            SearchForward();
        }

        /// <summary>
        /// Recursively find an item that matches filterText and select it. 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="filterText"></param>
        /// <returns></returns>
        bool FindAndSelectItemInTree(ItemCollection collection, string filterText)
        {
            foreach (TreeViewItem item in collection)
            {
                if (item.Header.ToString().Contains(filterText))
                {
                    if (searchCounter == localSearchCounter)
                    {
                        FileTreeview.ExpandAndSelectItem(item);
                        return true;
                    }
                    else
                    {
                        localSearchCounter++;
                        continue;
                    }
                }
                else
                {
                    if (item.Items != null)
                    {
                        if (FindAndSelectItemInTree(item.Items, filterText))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Recursively find an item that matches filterText and select it. 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="filterText"></param>
        /// <returns></returns>
        bool FindAndSelectItemInTree2(ItemCollection collection, string filterText)
        {
            foreach (TreeViewItem item in collection)
            {
                if (item.Header.ToString().Contains(filterText))
                {
                    if (searchCounter == localSearchCounter)
                    {
                        FileTreeview.SelectItem(item);
                        return true;
                    }
                    else
                    {
                        localSearchCounter++;
                        continue;
                    }
                }
                else
                {
                    if (item.Items != null)
                    {
                        if (FindAndSelectItemInTree2(item.Items, filterText))
                            return true;
                    }
                }
            }
            return false;
        }
        #endregion

        private void DownloadFiles_Click(object sender, RoutedEventArgs e)
        {
            //debugging button click
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!SearchText.IsKeyboardFocusWithin)
            {
                if (e.Key == Key.D)
                    _ogre.rotateRight(0);
                if (e.Key == Key.W)
                    _ogre.move(0);
                else if (e.Key == Key.S)
                    _ogre.move(0);
                else if (e.Key == Key.A)
                    _ogre.rotateLeft(0);
            }
        }
    }
}
