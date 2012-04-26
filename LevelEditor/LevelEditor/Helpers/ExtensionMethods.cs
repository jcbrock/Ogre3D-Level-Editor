using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace LevelEditor.Helpers
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///  todo - btw, I currently removed currentFilePath and I'm just using the file's info
        /// </summary>
        /// <param name="currentFilePath">Pass in file path. If null, then check the TreeViewItem's tag property.</param>
        /// <param name="currentLevel"></param>
        //public static void ParseMaterialFile(this TreeViewItem currentItem)//, string currentFilePath)
        //{
        //    if (currentItem.Tag == null)
        //    {
        //        return; //todo, log this?
        //    }
        //    Classes.FileDetailInformation treeViewFile;
        //    treeViewFile = (Classes.FileDetailInformation)currentItem.Tag;

        //    if (treeViewFile.fileType != OgreFileType.Material)
        //        return;

        //    string materialName = "";
        //    using (System.IO.StreamReader sr = new System.IO.StreamReader(treeViewFile.filePath))
        //    {
        //        string line;
        //        while ((line = sr.ReadLine()) != null)
        //        {
        //            //Trim front of line for whitespace, make sure the first 9 characters are 'material '
        //            //If there is a comment in the line, then ignore the '//' and everything past it
        //            //todo, can i use string builder?
        //            line = line.TrimStart(' ');
        //            if (line.Length > 9 && line.Substring(0, 9).ToLower().Contains("material "))
        //            {
        //                materialName = line.Remove(0, 9);
        //                if (line.Contains("//"))
        //                    materialName = materialName.Substring(0, materialName.IndexOf("//"));

        //                materialName = materialName.Trim();
        //                TreeViewItem fileItem = new TreeViewItem();
        //                fileItem.Header = materialName;
        //                fileItem.Tag = treeViewFile.filePath;
        //                currentItem.Items.Add(fileItem);
        //            }
        //        }
        //    }
        //    //RedrawScene(currentEntity, currentEntity, materialName, currentEntity + "node");
        //    //currentMaterial = materialName;
        //}


        public static void ExpandAndSelectItem(this TreeView treeView, object item)
        {
            ExpandAndSelectItemHelper(treeView, item);
        }
        public static void SelectItem(this TreeView treeView, object item)
        {
            SelectItemHelper(treeView, item);
        }

        //Using the extension method allows us to select an item in the WPF TreeView in the following way:
        //myTreeView.SelectItem(myItem);

        //Solving problem #1 requires iterating through all children at the 
        //top level first to see if any of them correspond to the item to select:

        private static bool ExpandAndSelectItemHelper(ItemsControl parentContainer, object itemToSelect)
        {
            foreach (Object item in parentContainer.Items)
            {
                TreeViewItem currentContainer = parentContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (item == itemToSelect && currentContainer != null)
                {
                    currentContainer.IsSelected = true;
                    currentContainer.BringIntoView();
                    currentContainer.Focus();

                    //the item was found
                    return true;
                }
            }

            //If no TreeViewItem is a match, then we must iterate through the children again to 
            //expand each one and iterate through its children:

            foreach (Object item in parentContainer.Items)
            {
                TreeViewItem currentContainer = parentContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (currentContainer != null && currentContainer.Items.Count > 0)
                {
                    bool wasExpanded = currentContainer.IsExpanded;
                    currentContainer.IsExpanded = true;

                    if (currentContainer.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        EventHandler eh = null;
                        eh = new EventHandler(delegate
                        {
                            if (currentContainer.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                            {
                                if (ExpandAndSelectItemHelper(currentContainer, itemToSelect) == false)
                                {
                                    //The assumption is that code executing in this EventHandler is the result of the parent not
                                    //being expanded since the containers were not generated.
                                    //since the itemToSelect was not found in the children, collapse the parent since it was previously collapsed
                                    currentContainer.IsExpanded = false;
                                }

                                //remove the StatusChanged event handler since we just handled it (we only needed it once)
                                currentContainer.ItemContainerGenerator.StatusChanged -= eh;
                            }
                        });
                        currentContainer.ItemContainerGenerator.StatusChanged += eh;
                    }
                    else //otherwise the containers have been generated, so look for item to select in the children
                    {
                        if (ExpandAndSelectItemHelper(currentContainer, itemToSelect) == false)
                        {
                            //restore the current TreeViewItem’s expanded state
                            currentContainer.IsExpanded = wasExpanded;
                        }
                        else //otherwise the node was found and selected, so return true
                        {
                            return true;
                        }
                    }
                }
            }
            return false; //end of ExpandAndSelectItem
        }




        private static bool SelectItemHelper(ItemsControl parentContainer, object itemToSelect)
        {
            foreach (Object item in parentContainer.Items)
            {
                TreeViewItem currentContainer = parentContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (item == itemToSelect && currentContainer != null)
                {
                    currentContainer.IsSelected = true;
                    currentContainer.BringIntoView();
                    currentContainer.Focus();

                    //the item was found
                    return true;
                }
            }

            //If no TreeViewItem is a match, then we must iterate through the children again to 
            //expand each one and iterate through its children:

            foreach (Object item in parentContainer.Items)
            {
                TreeViewItem currentContainer = parentContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (currentContainer != null && currentContainer.Items.Count > 0)
                {
                    bool wasExpanded = currentContainer.IsExpanded;
                    //currentContainer.IsExpanded = true;

                    if (currentContainer.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        EventHandler eh = null;
                        eh = new EventHandler(delegate
                        {
                            if (currentContainer.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                            {
                                if (SelectItemHelper(currentContainer, itemToSelect) == false)
                                {
                                    //The assumption is that code executing in this EventHandler is the result of the parent not
                                    //being expanded since the containers were not generated.
                                    //since the itemToSelect was not found in the children, collapse the parent since it was previously collapsed
                                    currentContainer.IsExpanded = false;
                                }

                                //remove the StatusChanged event handler since we just handled it (we only needed it once)
                                currentContainer.ItemContainerGenerator.StatusChanged -= eh;
                            }
                        });
                        currentContainer.ItemContainerGenerator.StatusChanged += eh;
                    }
                    else //otherwise the containers have been generated, so look for item to select in the children
                    {
                        if (SelectItemHelper(currentContainer, itemToSelect) == false)
                        {
                            //restore the current TreeViewItem’s expanded state
                            currentContainer.IsExpanded = wasExpanded;
                        }
                        else //otherwise the node was found and selected, so return true
                        {
                            return true;
                        }
                    }
                }
            }
            return false; //end of ExpandAndSelectItem
        }


        public static void SetItemCollection(this ItemCollection itemCollection, List<string> itemsToBeBound)
        {
            itemCollection.Clear();
            if (itemsToBeBound == null)
                return;

            foreach (string item in itemsToBeBound)
                itemCollection.Add(item);
        }
    }
}
