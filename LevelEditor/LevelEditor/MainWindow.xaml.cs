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
        private const string DefaultTabHeaderText = "Default";
        private const string NewTabHeaderText = "*";
        private const string NewTabHeaderValue = "NewTab";
        private const string MeshFileExtension = ".mesh";
        private const string MeshXMLFileExtension = ".mesh.xml";
        private const string MaterialFileExtension = ".material";
        private const string SceneFileExtension = ".scene";
        private const string BlenderFileExtension = ".blend";
        private const string AnimationQueueDelimiter = " -- ";
        private const string AnimationQueueLoopSuffix = " -- Loop";


        //Member Variables
        OgreForm _ogre;
        //ObservableCollection<RenderedObject> _fields { get; set; }
        List<string> _fileSystemPaths = new List<string>();
        List<KeyValuePair<int, string>> _currentMaterials = new List<KeyValuePair<int, string>>(); //Mapping of SubMesh index to Material Name // = "Examples/DarkMaterial";
        string _currentEntity = string.Empty; // = "knot.mesh";
        string _currentAnimation = string.Empty;
        float _currentZoom = (float)1.0;
        System.Drawing.Point _startPoint = new System.Drawing.Point();
        int diffX, diffY, startX, startY;

        //Creating an Mesh XML list for quick access - for linking Meshes to Submeshes... wait a second...

        //These two search counters keep track of what item is selected. One is incremented each time an item is found and selected and
        //the other is incremented each time an item is found. Once they equal each other then a new item is selected.
        int searchCounter = 0;
        int localSearchCounter = 0;

        public MainWindow()
        {

            //This is the fix for the Design time error where WPF has trouble finding MOgre.dll.
            //Much research on this issue has no yielded a way to solve the issue, so I resort to this.
            //Doesn't work:
            //if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            //{ return; }

            InitializeComponent();
            //LoadTabConfig();
            InitializeOgre();
            //AssetManager.Classes.GameAssetServicePoC foop = new AssetManager.Classes.GameAssetServicePoC();
            //if (AnimationQueue.HasItems)
           //     AnimationQueue.Items.RemoveAt(0); //todo? kinda hack - removes "System.Windows.Style", which gets placed in there when I set the style...
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
            if (!_ogre.Tick(diffX.ToString(), diffY.ToString(), startX.ToString(), startY.ToString(), searchCounter))
            {
                //Make sure there are still animations in the queue and that the one at the top is still the one that just finished
                //if (AnimationQueue.Items != null && AnimationQueue.Items.Count > 0 &&
                //    AnimationQueue.Items[0].ToString().Remove(AnimationQueue.Items[0].ToString().IndexOf(AnimationQueueDelimiter)) == _currentAnimation)
                //    RemoveAnimation(AnimationQueue.Items[0].ToString(), 0);

                //StartNextAnimation();
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
            //FileTreeview.Items.Add(OpenFolder("C:\\Users\\G521214\\Desktop\\ResourceTestFolder", null));
        }

        //Generates the right click menu
       // private System.Windows.Controls.ContextMenu GenerateContextMenu(FileDetailInformation fileInfo)
        private System.Windows.Controls.ContextMenu GenerateContextMenu()
        {
            System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
            System.Windows.Controls.MenuItem getLatest = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem checkOut = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem checkIn = new System.Windows.Controls.MenuItem();

          //  getLatest.Tag = fileInfo;
           // checkIn.Tag = fileInfo;
           // checkOut.Tag = fileInfo;
            getLatest.Header = "Get Latest";
            checkOut.Header = "Check Out";
            checkIn.Header = "Check In";
            getLatest.Click += new RoutedEventHandler(getLatest_Click);
            checkIn.Click += new RoutedEventHandler(checkIn_Click);
            checkOut.Click += new RoutedEventHandler(checkOut_Click);

           // SearchTabs currentTabProperties = (SearchTabs)((System.Windows.Controls.TabItem)TabContainer.SelectedItem).Tag;
           // if (currentTabProperties == null || !currentTabProperties.isMappedToSourceControl)
           // {
                getLatest.IsEnabled = false;
                checkOut.IsEnabled = false;
                checkIn.IsEnabled = false;
           // }
            contextMenu.Items.Add(getLatest);
            contextMenu.Items.Add(checkOut);
            contextMenu.Items.Add(checkIn);


            return contextMenu;
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
            //treeViewItem.ContextMenu = GenerateContextMenu(new FileDetailInformation(treeViewItem.Header.ToString(), currentDirectory, null, null, OgreFileType.Unknown));
            treeViewItem.MouseRightButtonDown += new MouseButtonEventHandler(myItem_Click);

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

            string parentFileName = "";
            string parentFilePath = "";
          //  OgreFileType fileType = OgreFileType.Unknown;
            //todo
            if (fileItem.Header.ToString().EndsWith(MeshFileExtension))
            {
              //  fileType = OgreFileType.Mesh;
                //int meshCount = 1;
                List<string> subMeshes = new List<string>();
                List<string> defaultMaterials = new List<string>();
                List<string> meshDefinitions = new List<string>();
                string skeletonLink = null;





              


               // MeshFileDetailInformation meshFileDetails = new MeshFileDetailInformation(file.Substring(file.LastIndexOf("\\") + 1), file, parentFileName, parentFilePath, fileType, meshDefinitions, subMeshes, defaultMaterials, skeletonLink);
                //fileItem.Tag = meshFileDetails;
                //DAMN, I have to run the converter to go from .mesh to .mesh.xml
                //XDocument xmlDoc = XDocument.Load(file);

                //foreach (var savedTab in xmlDoc.Descendants("tabs").Elements("tab"))
                //{
                //    try
                //    {
                //        List<string> searchPaths = new List<string>();
                //        List<string> extensionsToSearch = new List<string>();
                //        foreach (XElement searchPath in savedTab.Descendants("searchPaths").Elements("searchPath"))
                //            searchPaths.Add(searchPath.Value);
                //        foreach (XElement extensionToSearch in savedTab.Descendants("extensions").Elements("extension"))
                //            extensionsToSearch.Add(extensionToSearch.Value);

                //        System.Windows.Controls.TabItem newTab = new System.Windows.Controls.TabItem();
                //        newTab.Header = savedTab.Element("name").Value.ToString();
                //        newTab.Name = savedTab.Element("name").Value.ToString().ToUpper();

                //        TabSearchInformation container = new TabSearchInformation(searchPaths, extensionsToSearch);
                //        newTab.Tag = container;
                //        tabControl.Items.Add(newTab);
                //    }
                //    //todo - do I want to make this more specific?
                //    catch (Exception ex)
                //    {
                //        //There was an error while parsing this tab - log and continue
                //    }
                //}
            }
            else if (fileItem.Header.ToString().EndsWith(MaterialFileExtension))
            {
               // fileType = OgreFileType.Material;

                //Try to read the Parent Path / Filename out of the .material file. It should be the first line.
                try
                {
                    // create reader & open file
                    TextReader tr = new StreamReader(file);

                    string line = tr.ReadLine();
                    if (line.IndexOf('=') > -1)
                    {
                        parentFilePath = line.Substring(line.IndexOf('=') + 2);
                        parentFileName = parentFilePath.Substring(parentFilePath.LastIndexOf("\\") + 1);
                    }
                    // close the stream
                    tr.Close();
                }
                //todo - do I want to make this more specific?
                catch (Exception ex)
                {
                    //There was an error while parsing this tab - log and continue
                }

            }
            //else if (fileItem.Header.ToString().EndsWith(SceneFileExtension))
            //{
            //    fileType = OgreFileType.Scene;
            //}
            //else if (fileItem.Header.ToString().EndsWith(MeshXMLFileExtension))
            //{
            //    fileType = OgreFileType.MeshXML;
            //}

            //if (fileType != OgreFileType.Mesh)
            //{
            //    FileDetailInformation fileDetails = new FileDetailInformation(file.Substring(file.LastIndexOf("\\") + 1), file, parentFileName, parentFilePath, fileType);
            //    fileItem.Tag = fileDetails;
            //}

          //  fileItem.ParseMaterialFile();
           // fileItem.ContextMenu = GenerateContextMenu((FileDetailInformation)fileItem.Tag);
            fileItem.MouseRightButtonDown += new MouseButtonEventHandler(myItem_Click); //todo, I don't know if I really want to select items, is there some sort of highlight action instead?

            return fileItem;
            //.mesh     <parentFile filePath="C:\Users\Jeff\Desktop\ResourceTestFolder\sctest.blend"></parentFile>
            //.scene:    <scene exported_by="Jeff" formatVersion="1.0.1" previous_export_time="1317353079.173" export_time="1317353348.956" filePath="C:\Users\Jeff\Desktop\ResourceTestFolder\sctest.blend" >
            //.material   parentFile = C:\Users\Jeff\Desktop\ResourceTestFolder\sctest.blend
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

            if (e.Button == MouseButtons.Left)
            {
                _ogre.Rotate((float)(diffX * .5), (float)(diffY * .5), 0, _currentEntity);
            }
        }
        private void Panel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
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
            else if (parentItem != null && parentItem.Header.ToString().EndsWith(MaterialFileExtension))
                ChangeMaterial(selectedTreeViewItem, parentItem);
            else if (parentItem != null && parentItem.Header.ToString().EndsWith(BlenderFileExtension))
                DisplayBlendFileInfo(selectedTreeViewItem);
            else if (selectedTreeViewItem.Header.ToString().EndsWith(MaterialFileExtension))
                DisplayMaterialFileInfo(selectedTreeViewItem, null);
            else
                return;

            return;
        }

        private void ChangeMesh(TreeViewItem selectedItem)
        {
            _currentEntity = selectedItem.Header.ToString();
            if (_ogre != null && !string.IsNullOrWhiteSpace(_currentEntity))
                _ogre.Go(_currentEntity, _currentMaterials, _currentZoom, AutoScaling.IsChecked, OgreWinFormsHost.ActualHeight, OgreWinFormsHost.ActualWidth);

            //Need to display MeshFileInfo after ogre has rendered because it gets some of the properties from Ogre
            DisplayMeshFileInfo(selectedItem);
        }
        private void ChangeMaterial(TreeViewItem selectedItem, TreeViewItem parentItem)
        {


            //Remove previous mappings to this submesh index, add the newly selected material mapping
            //_currentMaterials.RemoveAll(n => n.Key == SubMeshListBox.SelectedIndex);
            //_currentMaterials.Add(new KeyValuePair<int, string>(SubMeshListBox.SelectedIndex, selectedItem.Header.ToString()));

            if (_ogre != null && !string.IsNullOrWhiteSpace(_currentEntity))
                _ogre.Go(_currentEntity, _currentMaterials, _currentZoom, AutoScaling.IsChecked, OgreWinFormsHost.ActualHeight, OgreWinFormsHost.ActualWidth);

            DisplayMaterialFileInfo(selectedItem, parentItem);
        }
        private void DisplayMeshFileInfo(TreeViewItem selectedMesh)
        {
            /*
            MeshFileDetailInformation currentFileInfo = (MeshFileDetailInformation)selectedMesh.Tag;

            MeshFileName.Content = _currentEntity;
            MeshBlendFileName.Content = currentFileInfo.parentFileName;

            //FileDetailInformation currentFileInfo = (FileDetailInformation)selectedMesh.Tag;
            ObservableCollection<RenderedSubMesh> renderedMeshList = new ObservableCollection<RenderedSubMesh>();
            ObservableCollection<RenderedAnimation> renderedAnimationList = new ObservableCollection<RenderedAnimation>();

            List<string> subMeshDefaultMaterials = _ogre.GetSubMeshDefaultMaterials(_currentEntity);
            for (int i = 0; i < subMeshDefaultMaterials.Count; i++)
            {
                renderedMeshList.Add(new RenderedSubMesh() { SubMesh = "submesh" + i, Material = subMeshDefaultMaterials[i], MaterialFileName = "", MaterialBlendFileName = "" });
            }

            SubMeshListBox.ItemsSource = renderedMeshList;

            //Default selection to first item
            if (renderedMeshList.Count > 0)
                SubMeshListBox.SelectedIndex = 0;

            SkeletonFileName.Content = _ogre.GetLinkedSkeletonName(_currentEntity);
            List<KeyValuePair<string, string>> animationDetails = _ogre.GetAnimations(_currentEntity);
            for (int i = 0; i < animationDetails.Count; i++)
                renderedAnimationList.Add(new RenderedAnimation() { Animation = animationDetails[i].Key, Length = animationDetails[i].Value, Loop = false });

            AnimationListBox.ItemsSource = renderedAnimationList;
             */
        }
        private void DisplayMaterialFileInfo(TreeViewItem selectedMaterial, TreeViewItem materialParent)
        {
            /*
            selectedMaterial.IsSelected = false;

            if (SubMeshListBox.SelectedIndex < 0)
                return; //I don't think this is possible, but just in case...

            //FileDetailInformation currentFileInfo = (FileDetailInformation)selectedMesh.Tag;
            ObservableCollection<RenderedSubMesh> renderedList;
            if (SubMeshListBox.ItemsSource == null)
                return; //renderedList5 = new ObservableCollection<RenderedObject3>();
            else
                renderedList = ((ObservableCollection<RenderedSubMesh>)SubMeshListBox.ItemsSource);

            //Have to remove in order for changes to take place.. :/
            RenderedSubMesh oldSubMesh = renderedList.Where(n => n.SubMesh == ((RenderedSubMesh)SubMeshListBox.SelectedItem).SubMesh).FirstOrDefault();

            if (materialParent != null)
            {
                FileDetailInformation currentFileInfo = (FileDetailInformation)materialParent.Tag;
                oldSubMesh.MaterialBlendFileName = currentFileInfo.parentFileName;
                oldSubMesh.MaterialFileName = currentFileInfo.fileName;
                oldSubMesh.ParentBlenderPath = currentFileInfo.parentFilePath;
            }

            oldSubMesh.Material = selectedMaterial.Header.ToString();

            //first need to read what Mesh file is applied - get skeleton file out of that
            //how to read animations from .skeleton file - todo
            //renderedList5.Add(new RenderedObject3() { SubMesh = "submesh1", Material = "foo mat", MaterialFileName = "foo mat.material", MaterialBlendFileName = "foo mat.blend" });
            //SubMeshListBox.ItemsSource = null;
            SubMeshListBox.ItemsSource = renderedList;

            //Since I'm working with ItemsSource I think I have to rebind the entire list - can't add/remove one item at a time.
        */
             }


        //private void DisplaySkeletonFileInfo()
        //{
        //    //FileDetailInformation currentFileInfo = (FileDetailInformation)selectedMaterial.Tag;

        //    //Since I'm working with ItemsSource I think I have to rebind the entire list - can't add/remove one item at a time.
        //    SkeletonFileName.Content = "Foo.skeleton";
        //    BlendFileName.Content = "(Foo.blend)";

        //    //FileDetailInformation currentFileInfo = (FileDetailInformation)selectedMesh.Tag;
        //    ObservableCollection<RenderedAnimation> renderedList;
        //    if (FieldsListBox.ItemsSource == null)
        //        renderedList = new ObservableCollection<RenderedAnimation>();
        //    else
        //        renderedList = ((ObservableCollection<RenderedAnimation>)FieldsListBox.ItemsSource);

        //    //first need to read what Mesh file is applied - get skeleton file out of that
        //    //how to read animations from .skeleton file - todo
        //    renderedList.Add(new RenderedAnimation() { Animation = "foo animation", Loop = false });
        //    renderedList.Add(new RenderedAnimation() { Animation = "foo animation2", Loop = true });
        //    FieldsListBox.ItemsSource = renderedList;


        //    //TEMP:
        //    MeshFileName.Content = "Foo.mesh";
        //    MeshBlendFileName.Content = "(Foo.blend)";

        //    //FileDetailInformation currentFileInfo = (FileDetailInformation)selectedMesh.Tag;
        //    ObservableCollection<RenderedSubMesh> renderedList5;
        //    if (SubMeshListBox.ItemsSource == null)
        //        renderedList5 = new ObservableCollection<RenderedSubMesh>();
        //    else
        //        renderedList5 = ((ObservableCollection<RenderedSubMesh>)SubMeshListBox.ItemsSource);

        //    //first need to read what Mesh file is applied - get skeleton file out of that
        //    //how to read animations from .skeleton file - todo
        //    renderedList5.Add(new RenderedSubMesh() { SubMesh = "submesh1", Material = "foo mat", MaterialFileName = "foo mat.material", MaterialBlendFileName = "foo mat.blend" });
        //    renderedList5.Add(new RenderedSubMesh() { SubMesh = "submesh2", Material = "foo mat2", MaterialFileName = "foo mat2.material", MaterialBlendFileName = "foo mat2.blend" });
        //    SubMeshListBox.ItemsSource = renderedList5;

        //}

        private void DisplayBlendFileInfo(TreeViewItem selectedItem)
        {
            //_currentMaterials = selectedItem.Header.ToString();
            //SelectedMatFileName.Text = selectedItem.Header.ToString();
            //SelectedMatFilePath.Text = selectedItem.Tag.ToString();
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
        private void ApplyZoom(int deltaZoom)
        {
            int zoomPercentage;
            int.TryParse(ZoomText.Text.Replace('%', ' '), out zoomPercentage);
            zoomPercentage += deltaZoom;
            _currentZoom = (float)zoomPercentage / 100;
            _ogre.Scale(_currentZoom, _currentZoom, _currentZoom, _currentEntity);
            ZoomText.Text = zoomPercentage.ToString() + " %";
        }
        private void ZoomText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyZoom(0);
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!SearchText.IsKeyboardFocusWithin)
            {
                if (e.Key == Key.O)
                    ApplyZoom(-10);
                else if (e.Key == Key.P)
                    ApplyZoom(10);
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

        #region SOURCE CONTROL


        void getLatest_Click(object sender, RoutedEventArgs e)
        {
         
        }

        void checkOut_Click(object sender, RoutedEventArgs e)
        {
         
        }

        void checkIn_Click(object sender, RoutedEventArgs e)
        {
         
        }

        List<string> GetFilesWithinFolder(string folderName, bool searchSubFolders)
        {
            List<string> files = new List<string>();

            if (searchSubFolders)
            {
                IEnumerable<string> subFolders = System.IO.Directory.EnumerateDirectories(folderName); //todo - need to put in a try catch so that invalid directories don't cause errors
                foreach (string folder in subFolders)
                {
                    files.AddRange(GetFilesWithinFolder(folder, searchSubFolders));
                }
            }

            //Iterate child files
            IEnumerable<string> testFiles = System.IO.Directory.EnumerateFiles(folderName);
            foreach (string file in testFiles)
                files.Add(file);

            return files;
        }

        void myItem_Click(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.Controls.TreeViewItem)sender).IsSelected = true;

            //set e.Handled to true so that this click event doesn't cascade up to parent folders
            e.Handled = true;
        }

        #endregion
        private void DownloadFiles_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Machine Name := " + Environment.MachineName);

            //this works:
            // DownloadProgress popup = new DownloadProgress("");
            // popup.ShowDialog();



            //credit for this code goes to:http://www.c-sharpcorner.com/uploadfile/kirtan007/how-to-download-file-and-showing-its-progress-in-progress-bar/
            //System.Net.WebClient wc = new System.Net.WebClient();
            //wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged);
            //wc.DownloadFileAsync(new Uri("http://www.jeffandlainawedding.com/GameAssets/ogrehead.mesh"), System.AppDomain.CurrentDomain.BaseDirectory + "\\ogrehead.mesh", null);

            //var request = System.Net.WebRequest.Create("http://www.jeffandlainawedding.com/GameAssets/ogrehead.mesh");
            //using (var stream = request.GetResponse().GetResponseStream())
            //{
            //    using (var reader = new StreamReader(stream))
            //    {
            //        var fileContent = reader.ReadToEnd();
            //    }
            //}
        }


      
    }


}
