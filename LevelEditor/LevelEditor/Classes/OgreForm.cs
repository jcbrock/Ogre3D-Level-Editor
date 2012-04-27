using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
namespace LevelEditor.Classes
{
    class OgreForm : Mogre.SceneManager.Listener
    {
        Root mRoot;
        RenderWindow mWindow;
        RenderSystem mRSys;
        AnimationState mAnimationState = null;
        int _currentRotation = 0;

        Camera mCamera;
        Boolean IsInitialized { get; set; }
        SceneManager mMgr { get; set; }

        List<string> _ConfigurationPaths;


        SceneNode cameraNode;
        SceneNode cameraYawNode;
        SceneNode cameraPitchNode;
        SceneNode cameraRollNode;

        Vector3 scale = new Vector3((float).2, (float).2, (float).2);

        public OgreForm(List<string> ConfigurationPaths)
        {
            _ConfigurationPaths = ConfigurationPaths;
            IsInitialized = false;
        }

        public void Go(string currentEntity, List<KeyValuePair<int, string>> currentMaterial, float currentScale, bool? autoScale, double renderWindowHeight, double renderWindowWidth)
        {
            RedrawScene(currentEntity, currentEntity, currentMaterial, currentEntity + "node", currentScale, autoScale, renderWindowHeight, renderWindowWidth);
        }

        public string GetLinkedSkeletonName(string currentEntity)
        {
            if (!mMgr.HasEntity(currentEntity))
                return string.Empty;

            Mesh currentMesh = mMgr.GetEntity(currentEntity).GetMesh();
            return currentMesh.SkeletonName;
        }

        public List<KeyValuePair<string, string>> GetAnimations(string currentEntity)
        {
            List<KeyValuePair<string, string>> animations = new List<KeyValuePair<string, string>>();
            if (!mMgr.HasEntity(currentEntity))
                return animations;

            try
            {
                Skeleton currentSkeleton = mMgr.GetEntity(currentEntity).GetMesh().GetSkeleton();

                if (currentSkeleton != null)
                {
                    for (ushort i = 0; i < currentSkeleton.NumAnimations; i++)
                        animations.Add(new KeyValuePair<string, string>(currentSkeleton.GetAnimation(i).Name, currentSkeleton.GetAnimation(i).Length.ToString()));
                }
            }
            catch (Exception e)
            {
                //error while trying to get submesh details, print out error for now - todo, change later to be more user friendly
                animations.Add(new KeyValuePair<string, string>("Current Entity = " + currentEntity, e.Message));
            }

            return animations;
        }

        public List<string> GetSubMeshDefaultMaterials(string currentEntity)
        {
            List<string> defaultMaterials = new List<string>();
            if (!mMgr.HasEntity(currentEntity))
                return defaultMaterials;

            try
            {
                Mesh currentMesh = mMgr.GetEntity(currentEntity).GetMesh();
                Mesh.SubMeshIterator meshIter = currentMesh.GetSubMeshIterator();

                while (meshIter.MoveNext())
                    defaultMaterials.Add(meshIter.Current.MaterialName);
            }
            catch (Exception e)
            {
                //error while trying to get submesh details, print out error for now - todo, change later to be more user friendly
                defaultMaterials.Add(e.Message);
            }

            return defaultMaterials;
        }

        public void SetAnimation(string currentEntity, string animation, bool loopAnimation)
        {
            if (!mMgr.HasEntity(currentEntity))
                return;

            Entity ent = mMgr.GetEntity(currentEntity);

            try
            {
                if (mAnimationState != null)
                    mAnimationState.Enabled = false; //set the old animation to not be enabled

                mAnimationState = ent.GetAnimationState(animation); //if the animation file isn't found, then it throws an exception

                mAnimationState.TimePosition = 0;
                mAnimationState.Loop = loopAnimation;
                mAnimationState.Enabled = true;
            }
            catch (Exception ex)
            {
                mAnimationState = null;
            }
        }

        //Keeping this code in case I need to use it for the Level Editor
        //Instead of changing the Scale on each model to look roughly the same, I know change where the camera is located
        //Whereas I might want it the other way in the level editor because I'll care about relative sizes of the models
        //public void AutoScale(string currentEntity, double renderWindowHeight, double renderWindowWidth)
        //{
        //    if (!mMgr.HasEntity(currentEntity))
        //        return;

        //    Entity ent = mMgr.GetEntity(currentEntity);
        //    Mesh currentMesh = ent.GetMesh(); //helpful: foo.numlod, numSubMeshes, numAnimations

        //    float posYScale = (float)((renderWindowHeight / 2) / (currentMesh.Bounds.Center.y + currentMesh.Bounds.HalfSize.y));
        //    float posXScale = (float)((renderWindowWidth / 2) / (currentMesh.Bounds.Center.x + currentMesh.Bounds.HalfSize.x));
        //    float negYScale = (float)((renderWindowHeight / 2) / (currentMesh.Bounds.Center.y - currentMesh.Bounds.HalfSize.y)) * -1;
        //    float negXScale = (float)((renderWindowWidth / 2) / (currentMesh.Bounds.Center.x - currentMesh.Bounds.HalfSize.x)) * -1;

        //    //Figure out the limiting scale, use that one
        //    if (posYScale <= posXScale && posYScale <= negYScale && posYScale <= negXScale)
        //        Scale(posYScale, posYScale, posYScale, currentMesh.Name);
        //    else if (posXScale <= posYScale && posXScale <= negYScale && posXScale <= negXScale)
        //        Scale(posXScale, posXScale, posXScale, currentMesh.Name);
        //    else if (negYScale <= posXScale && negYScale <= posYScale && negYScale <= negXScale)
        //        Scale(negYScale, negYScale, negYScale, currentMesh.Name);
        //    else
        //        Scale(negXScale, negXScale, negXScale, currentMesh.Name); 
        //}

        //todo - do I need to add camera, viewport, etc... stuff to this?
        private void RedrawScene(string attachedEntityName, string meshName, List<KeyValuePair<int, string>> materialNames, string sceneNodeName, float currentScale, bool? autoScale, double renderWindowHeight, double renderWindowWidth)
        {
            mMgr.ClearScene();
            SceneNode node = null;
            mAnimationState = null;

            //I think by default it'll have the materials set as long as they are found - this is needed if we want to change the materials used...
            Entity ent = mMgr.CreateEntity(attachedEntityName, meshName);
            Mesh currentMesh = ent.GetMesh(); //helpful: foo.numlod, numSubMeshes, numAnimations

            try
            {
                uint cnt = ent.NumSubEntities;
                uint meshCounter = 0;
                Mesh.SubMeshIterator testIter = ent.GetMesh().GetSubMeshIterator();
                while (testIter.MoveNext())
                {
                    string assignedMaterialName = materialNames.FirstOrDefault(n => n.Key == (int)meshCounter).Value;
                    if (meshCounter < cnt && assignedMaterialName != null)
                        ent.GetSubEntity(meshCounter).SetMaterialName(assignedMaterialName);

                    meshCounter++;
                    // SubMesh foobar = testIter.Current;
                    // foobar.SetMaterialName(foobar.MaterialName);
                }
            }
            catch (Exception ex)
            {
                ent.SetMaterialName(materialNames[0].Value); //todo, fix  logic
            }

            //ent.SetMaterialName("Examples/DarkMaterial");
            node = mMgr.RootSceneNode.CreateChildSceneNode(sceneNodeName);
            node.AttachObject(ent);

            //todo - make this a debug toggle (or perhaps a checkbox on the UI)
            node.ShowBoundingBox = true;

            //todo - is this a good way of doing this?
            float z = 0;
            if (ent.BoundingBox.Size.z > ent.BoundingBox.Size.x && ent.BoundingBox.Size.z > ent.BoundingBox.Size.y)
                z = ent.BoundingBox.Size.z;
            else if (ent.BoundingBox.Size.y > ent.BoundingBox.Size.x && ent.BoundingBox.Size.y > ent.BoundingBox.Size.z)
                z = ent.BoundingBox.Size.y;
            else
                z = ent.BoundingBox.Size.x;

            //Since mCameraAutoAspectRatio = true, it means: mCamera.AspectRatio = (float)(renderWindowWidth / renderWindowHeight);
            //By doing this I remove the need for scaling the object - unless you're zooming in/out
            mCamera.Position = new Vector3(ent.BoundingBox.Center.x, ent.BoundingBox.Center.y, z * 2); //*2 for buffer
            mCamera.LookAt(ent.BoundingBox.Center);
            mCamera.NearClipDistance = ent.BoundingBox.Size.z / 3;

            //Create a single point light source
            Light light2 = mMgr.CreateLight("MainLight");
            light2.Position = new Vector3(0, 10, -25);
            light2.Type = Light.LightTypes.LT_POINT;
            light2.SetDiffuseColour(1.0f, 1.0f, 1.0f);
            light2.SetSpecularColour(0.1f, 0.1f, 0.1f);
        }
        public void Resize(int width, int height)
        {
            if (mRoot == null || mWindow == null) return;
            // Need to let Ogre know about the resize...
            mWindow.Resize((uint)width, (uint)height);

            // Alter the camera aspect ratio to match the viewport
            mCamera.AspectRatio = (float)((double)width / (double)height);
        }

        public void Scale(float x, float y, float z)
        {
            scale = new Vector3(x, y, z);
        }

        public void Rotate(float x, float y, float z, string currentEntity)
        {
            if (string.IsNullOrWhiteSpace(currentEntity))
                return;

            SceneNode node = mMgr.GetSceneNode(currentEntity + "node");
            node.Rotate(new Vector3(0, -1, 0), ((float)(x * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            node.Rotate(new Vector3(1, 0, 0), ((float)(y * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            node.Rotate(new Vector3(0, 0, 1), ((float)(z * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            //learning: .Yaw/.Pitch/.Roll is pretty much the same as what I'm doing above
        }

        void OgreForm_Resize(object sender, EventArgs e)
        {
            mWindow.WindowMovedOrResized();
        }

        void OgreForm_Disposed(object sender, EventArgs e)
        {
            mRoot.Dispose();
            mRoot = null;
        }

        public void RenderEnvironment()
        {
            mMgr.SetWorldGeometry("terrain.cfg");
            mMgr.SetSkyDome(true, "Examples/CloudySky", 2, 6);
        }

        public void Tick(Object stateInfo)
        {
            if (mRoot != null && IsInitialized == true)
            {
                renderScene();
                mRoot.RenderOneFrame();
            }
        }

        public bool Tick(int diffX, int diffY, string startX, string startY, int searchCounter)
        {

            if (mRoot != null && IsInitialized == true)
            {
                renderScene();
                foobar();
                if (mAnimationState != null)
                {
                    mAnimationState.AddTime(((float).01)); //todo - might not render last frame
                    if (mAnimationState.HasEnded)
                        return false;
                }

                // Setup the scene query
                Vector3 camPos = mCamera.RealPosition;
                Ray cameraRay = new Ray(new Vector3(camPos.x, 5000.0f, camPos.z),
                    Vector3.NEGATIVE_UNIT_Y);
                mRaySceneQuery.Ray = cameraRay;

                // Perform the scene query;
                RaySceneQueryResult result = mRaySceneQuery.Execute();
                RaySceneQueryResult.Enumerator itr = (RaySceneQueryResult.Enumerator)result.GetEnumerator();

                // Get the results, set the camera height
                if ((itr != null) && itr.MoveNext())
                {
                    if (itr.Current != null && itr.Current.worldFragment != null)
                    {
                        float terrainHeight = itr.Current.worldFragment.singleIntersection.y;

                        if ((terrainHeight + 120.0f) > camPos.y)
                        {// mCamera.SetPosition(camPos.x, terrainHeight + 150.0f, camPos.z);
                            cameraNode.Translate(cameraYawNode.Orientation *
                                                                  cameraPitchNode.Orientation *
                                                                  new Vector3(0, 10, 0),
                                                                  SceneNode.TransformSpace.TS_LOCAL);
                        }
                    }
                }

                mRoot.RenderOneFrame();
            }
            return true;
        }

        void renderScene()
        {
            // set the window's viewport as the active viewport
            mRSys._setViewport(mCamera.Viewport);
            // clear colour & depth
            mRSys.ClearFrameBuffer((uint)Mogre.FrameBufferType.FBT_COLOUR | (uint)Mogre.FrameBufferType.FBT_DEPTH);

            // render scene with overlays
            mMgr._renderScene(mCamera, mCamera.Viewport, true);
            mWindow.SwapBuffers(true);
        }

        public void AddResourceLocation(string resourceLocation)
        {
            ResourceGroupManager.Singleton.AddResourceLocation(resourceLocation, "FileSystem", "General");
            Console.WriteLine(resourceLocation);
            //todo - i seriously need a debug window
        }

        public void LoadResourceLocations(List<string> configurationPaths)
        {

            // Console.WriteLine("NEW LOAD:");
            //var foob = ResourceGroupManager.Singleton.ListResourceLocations("General");
            //var bar = ResourceGroupManager.Singleton.ListResourceNames("General", true);
            //var foop = ResourceGroupManager.Singleton.ListResourceNames("General", false);

            // //ResourceGroupManager.Singleton.ClearResourceGroup("General");

            //foreach (string foobar in foob)
            //{
            //    ResourceGroupManager.Singleton.RemoveResourceLocation(foobar);
            //}

            // var foob2 = ResourceGroupManager.Singleton.ListResourceLocations("General");
            // var bar2 = ResourceGroupManager.Singleton.ListResourceNames("General", true);
            // var foop2 = ResourceGroupManager.Singleton.ListResourceNames("General", false);

            //example of manual add: _FileSystemPaths.Add("../../Media/models");
            foreach (string foo in configurationPaths)
            {
                AddResourceLocation(foo);
            }
            //Console.WriteLine("DONE LOAD:");
            //var foob3 = ResourceGroupManager.Singleton.ListResourceLocations("General");
            //var bar3 = ResourceGroupManager.Singleton.ListResourceNames("General", true);
            //var foop3 = ResourceGroupManager.Singleton.ListResourceNames("General", false);

        }

        //public void MoveCameraX(int x)
        //{
        //    mCamera.Position = new Vector3(mCamera.Position.x + x, mCamera.Position.y, mCamera.Position.z);
        //}
        //public void MoveCameraY(int y)
        //{
        //    mCamera.Position = new Vector3(mCamera.Position.x, mCamera.Position.y + y, mCamera.Position.z);
        //}
        //public void MoveCameraZ(int z)
        //{
        //    mCamera.Position = new Vector3(mCamera.Position.x, mCamera.Position.y, mCamera.Position.z + z);
        //}
        //public void RotateCameraX(int x)
        //{
        //    mCamera.Rotate(new Vector3(0, -1, 0), ((float)(x * System.Math.PI) / 180));
        //}
        //public void RotateCameraY(int y)
        //{
        //    //mCamera.Rotate(new Vector3(1, 0, 0), ((float)(y * System.Math.PI) / 180));
        //    mCamera.Rotate(new Vector3(1, 0, 0), ((float)(y * System.Math.PI) / 180)); //if looking right down z
        //    mCamera.Rotate(new Vector3(1, 0, 0), ((float)(y * System.Math.PI) / 180)); //if looking right down x
        //}
        //public void RotateCameraZ(int z)
        //{
        //    mCamera.Rotate(new Vector3(0, 0, 1), ((float)(z * System.Math.PI) / 180));
        //}
        //public void LookAtX(int x)
        //{
        //    mCamera.LookAt(x, 0, 0);
        //}
        //public void LookAtY(int y)
        //{
        //    mCamera.LookAt(0, y, 0);
        //}
        //public void LookAtZ(int z)
        //{
        //    mCamera.LookAt(0, 0, z);
        //}


        //Needs to assign a member variable so it gets rotated every frame
        //since key buttons don't have a "keyPressed" event
        public void rotateLeft(int a)
        {
            _currentRotation = a;
        }

        public void rotateRight(int a)
        {
            _currentRotation = -a;
        }

        public void rotateUp(int a)
        {
            // Rotate camera left.
            cameraPitchNode.Pitch((float)(a * System.Math.PI * .5) / 180);
        }

        Vector3 transVect = new Vector3(0, 0, 0);
        public void move(int v)
        {
            transVect.z = v;
        }

        protected RaySceneQuery mRaySceneQuery = null;      // The ray scene query pointer
        protected bool mLMouseDown, mRMouseDown = false;    // True if the mouse buttons are down
        protected int mCount = 0;                           // The number of robots on the screen
        protected SceneNode mCurrentObject = null;          // The newly created object

        //  protected OgreCEGUIRenderer mGUIRenderer = null;    //CEGUI Renderer
        //protected GuiSystem mGUISystem = null;              //GUISystem
        protected float mouseX, mouseY;
        public void LeftClicked(float x, float y, string currentEntity)
        {
            // Save mouse position
            mouseX = x; mouseY = y;

            // Setup the ray scene query
            Ray mouseRay = mCamera.GetCameraToViewportRay(mouseX, mouseY);

            Ray newRay = new Ray(new Vector3(mouseRay.Origin.x, System.Math.Abs(mouseRay.Origin.y), mouseRay.Origin.z), mouseRay.Direction);
            mRaySceneQuery.Ray = newRay;

            // Execute query
            RaySceneQueryResult result = mRaySceneQuery.Execute();
            RaySceneQueryResult.Enumerator itr = (RaySceneQueryResult.Enumerator)result.GetEnumerator();

            // Get results, create a node/entity on the position
            if (itr != null && itr.MoveNext())
            {
                if (string.IsNullOrWhiteSpace(currentEntity))
                    return;

                if (itr.Current != null && itr.Current.worldFragment != null)
                {
                    Entity ent = mMgr.CreateEntity(currentEntity + mCount.ToString(), currentEntity);
                    mCurrentObject = mMgr.RootSceneNode.CreateChildSceneNode(currentEntity + "Node" + mCount.ToString(),
                        itr.Current.worldFragment.singleIntersection);
                    mCount++;
                    mCurrentObject.AttachObject(ent);
                    mCurrentObject.SetScale(scale);
                }
            } // 

            mLMouseDown = true;
            return;
        }

        public void foobar()
        {
            float pitchAngle;
            float pitchAngleSign;

            // Yaws the camera according to the mouse relative movement.
            cameraYawNode.Yaw((float)(_currentRotation * System.Math.PI * .1) / 180);

            // Pitches the camera according to the mouse relative movement.
            //cameraPitchNode.Pitch((float)(y * System.Math.PI) / 180);

            // Translates the camera according to the translate vector which is
            // controlled by the keyboard arrows.
            //
            // NOTE: We multiply the mTranslateVector by the cameraPitchNode's
            // orientation quaternion and the cameraYawNode's orientation
            // quaternion to translate the camera accoding to the camera's
            // orientation around the Y-axis and the X-axis.
            cameraNode.Translate(cameraYawNode.Orientation *
                                        cameraPitchNode.Orientation *
                                        transVect,
                                        SceneNode.TransformSpace.TS_LOCAL);

            // Angle of rotation around the X-axis.
            pitchAngle = (2 * (Mogre.Math.ACos(cameraPitchNode.Orientation.w))).ValueDegrees;

            // Just to determine the sign of the angle we pick up above, the
            // value itself does not interest us.
            pitchAngleSign = cameraPitchNode.Orientation.x;

            // Limit the pitch between -90 degress and +90 degrees, Quake3-style.
            if (pitchAngle > 90.0f)
            {
                if (pitchAngleSign > 0)
                    // Set orientation to 90 degrees on X-axis.
                    cameraPitchNode.SetOrientation(Mogre.Math.Sqrt(0.5f), Mogre.Math.Sqrt(0.5f), 0, 0);
                else if (pitchAngleSign < 0)
                    // Sets orientation to -90 degrees on X-axis.
                    cameraPitchNode.SetOrientation(Mogre.Math.Sqrt(0.5f), -Mogre.Math.Sqrt(0.5f), 0, 0);
            }
        }

        public void Init(String handle)
        {
            try
            {
                // Create root object
                mRoot = new Root();

                // Define Resources
                ConfigFile cf = new ConfigFile();
                cf.Load("./resources.cfg", "\t:=", true);
                ConfigFile.SectionIterator seci = cf.GetSectionIterator();
                String secName, typeName, archName;

                while (seci.MoveNext())
                {
                    secName = seci.CurrentKey;
                    ConfigFile.SettingsMultiMap settings = seci.Current;
                    foreach (KeyValuePair<string, string> pair in settings)
                    {
                        typeName = pair.Key;
                        archName = pair.Value;
                        ResourceGroupManager.Singleton.AddResourceLocation(archName, typeName, secName);
                    }
                }

                //Load the resources from resources.cfg and selected tab (_ConfigurationPaths)
                //LoadResourceLocations(_ConfigurationPaths);

                //example of manual add: _FileSystemPaths.Add("../../Media/models");
                foreach (string foo in _ConfigurationPaths)
                {
                    AddResourceLocation(foo);
                }



                // Setup RenderSystem
                mRSys = mRoot.GetRenderSystemByName("Direct3D9 Rendering Subsystem");
                //mRSys = mRoot.GetRenderSystemByName("OpenGL Rendering Subsystem");

                // or use "OpenGL Rendering Subsystem"
                mRoot.RenderSystem = mRSys;

                mRSys.SetConfigOption("Full Screen", "No");
                mRSys.SetConfigOption("Video Mode", "800 x 600 @ 32-bit colour");

                // Create Render Window
                mRoot.Initialise(false, "Main Ogre Window");
                NameValuePairList misc = new NameValuePairList();
                misc["externalWindowHandle"] = handle;
                misc["FSAA"] = "4";
                // misc["VSync"] = "True"; //not sure how to enable vsync to remove those warnings in Ogre.log
                mWindow = mRoot.CreateRenderWindow("Main RenderWindow", 800, 600, false, misc);

                // Init resources
                MaterialManager.Singleton.SetDefaultTextureFiltering(TextureFilterOptions.TFO_ANISOTROPIC);
                TextureManager.Singleton.DefaultNumMipmaps = 5;
                ResourceGroupManager.Singleton.InitialiseAllResourceGroups();

                // Create a Simple Scene
                //SceneNode node = null;
                // mMgr = mRoot.CreateSceneManager(SceneType.ST_GENERIC, "SceneManager");
                mMgr = mRoot.CreateSceneManager(SceneType.ST_EXTERIOR_CLOSE, "SceneManager");

                mMgr.AmbientLight = new ColourValue(0.8f, 0.8f, 0.8f);

                mCamera = mMgr.CreateCamera("Camera");
                mWindow.AddViewport(mCamera);

                mCamera.AutoAspectRatio = true;
                mCamera.Viewport.SetClearEveryFrame(false);

                //Entity ent = mMgr.CreateEntity(displayMesh, displayMesh);

                //ent.SetMaterialName(displayMaterial);
                //node = mMgr.RootSceneNode.CreateChildSceneNode(displayMesh + "node");
                //node.AttachObject(ent);

                mCamera.Position = new Vector3(0, 0, 0);
                //mCamera.Position = new Vector3(0, 0, -400);
                mCamera.LookAt(0, 0, 1);

                //Create a single point light source
                Light light2 = mMgr.CreateLight("MainLight");
                light2.Position = new Vector3(0, 10, -25);
                light2.Type = Light.LightTypes.LT_POINT;
                light2.SetDiffuseColour(1.0f, 1.0f, 1.0f);
                light2.SetSpecularColour(0.1f, 0.1f, 0.1f);

                mWindow.WindowMovedOrResized();

                IsInitialized = true;

                // Create the camera's top node (which will only handle position).
                cameraNode = mMgr.RootSceneNode.CreateChildSceneNode();
                cameraNode.Position = new Vector3(0, 0, 0);

                //cameraNode = mMgr->getRootSceneNode()->createChildSceneNode();
                //cameraNode->setPosition(0, 0, 500);

                // Create the camera's yaw node as a child of camera's top node.
                cameraYawNode = cameraNode.CreateChildSceneNode();

                // Create the camera's pitch node as a child of camera's yaw node.
                cameraPitchNode = cameraYawNode.CreateChildSceneNode();

                // Create the camera's roll node as a child of camera's pitch node
                // and attach the camera to it.
                cameraRollNode = cameraPitchNode.CreateChildSceneNode();
                cameraRollNode.AttachObject(mCamera);

                mRaySceneQuery = mMgr.CreateRayQuery(new Ray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error,OgreForm.cs]: " + ex.Message + "," + ex.StackTrace);
            }
        }      
    }
}
