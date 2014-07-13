using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Timers;
using KSP.IO;




namespace VerticalVelocity
{
  
    


    //Begin Vertical Velocity Control Mod by Diazo. (Originally Thrust to Weight Ratio 1 mod, hence the TWR1 references everywhere.)
    //Released under the GPL 3 license (http://www.gnu.org/licenses/gpl-3.0.html)
    //This means you may modify and distribute this code as long as you include the source code showing the changes you made and also release your changes under the GPL 3 license.

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TWR1 : MonoBehaviour
    {
        private bool TWR1KeyDown = false; //Is the TWR1 being held down at the moment?
        private bool TWR1Engaged = false; //Is TWR1 (Thrust Weight Ratio 1) mod engaged and auto-controlling?
        private double TWR1ThrustUp = 0f; //thrust straight up needed for desired accel
        private double TWR1ThrustUpAngle = 0f; //thrust needed for desired accel, compensation for vessel angel
        private double TWR1DesiredAccel = 0f; //desired acceleration, includes planet grav so this can not be negative
        private double TWR1DesiredAccelThrust = 0f; //thrust needed for desired accel
        private Vessel TWR1Vessel; //Our active vessel
        private double TWR1Mass = 0f; //Vessel's mass
        private double TWR1MassLast = 0;
        private double TWR1MaxThrust = 0f; //Max thrust contolled by throttle
        private double TWR1MinThrust = 0f; //Min thrust controlled by throttle, not necessarily zero if solid rocket boosters are firing
        private ModuleEngines TWR1EngineModule; //part of check for solid rocket booster
        private ModuleEnginesFX TWR1EngineModuleFX;
        private CelestialBody TWR1SOI; //what are we orbiting?
        private double TWR1GravHeight = 0f; //distance from center of body for gravity force
        private double TWR1GravForce = 0f; //acceleration down due to gravity at this moment
        private double TWR1ThrottleRead = 0f; //current throttle at start of update in %
        private Quaternion TWR1RotSurf; //surface rotation of SOI body (?)
        private Quaternion TWR1RotVesselSurf; //rotation of vessel relative to SOI body (?)
        private double TWR1VesselPitch; //pitch of vessel from horizon
        private Vector3 TWR1North; //vessel north angle
        private Vector3 TWR1Up; //vessel up angle
        private Vector3 TWR1CoM; //vessel Center of mass
        private double TWR1OffsetVert = 0f; //vessel's offset from vertical in degrees
        private double TWR1OffsetVertRadian = 0f; //vessel's offset from vertical in radians
        private double TWR1OffsetVertRatio = 0f; //cosine of vessel's offset from vertical (return as unitless nubmer, so not degress or radians)
        private ConfigNode TWR1Node; //config node used to load keybind
        private string TWR1KeyCodeString = "Z"; //set the TWR1, default to Z if it can't load from TWR1.cfg
        private KeyCode TWR1KeyCode;
        private double TWR1VelocitySetpoint = 0f; //vessel vertical velocity setpoint
        private double TWR1VelocityCurrent = 0f; //vessel's current vertical velocity
        private double TWR1VelocityDiff = 0f; //velocity difference between setpoint and current
        private static Rect TWR1WinPos = new Rect(100, 100, 195, 80); //window size
        private int TWR1WinPosHeight = 100;
        private int TWR1WinPosWidth = 100;
        private static GUIStyle TWR1WinStyle = null; //window style
        private bool TWR1HeightCtrl = false; //control to height engaged?
        private double TWR1HCtime = 1f; //Time for Height Control Mode
        private double TWR1HC80Thrust; //80% thrust accel
        private double TWR1HCneeded; //height needed for altitude control
        private double TWR1HCTarget = 0f; //target height
        private double TWR1HCToGround; //height about ground/building/sea
        private double TWR1HCDistance; //distance from current heigh to target, this is absolute value so always postive
        private double TWR1HC5Thrust; //accel at 5% thrust
        private double TWR1HC1Thrust; //accel at 5% thrust
        private double TWR1HCDistToTarget; //Distance to HC target, always positive
        private Color TWR1ContentColor; //Default content color
        private string TWR1HCTargetString; //Height Control target height in string format for GUI text entry
        private bool TWR1HCOrbitDrop = false; //Are we orbit dropping?
        private IButton TWR1Btn; //blizzy's toolbar button
        private bool TWR1Show = false; //show GUI?
        private double TWR1HCThrustWarningTime = 0; //gametime saved for thrust warning check
        private bool TWR1HCFullThrustUp = false; //during height control we are low enough to lock at full thrust?
        private bool TWR1OrbitDropAllow = false; //are we high enough to offer Orbit Drop as an option?
        private double TWR1OrbitDropHeightNeeded = 0f; //how much height needed for Orbit Drop
        private double TWR1OrbitDropTimeNeeded = 0f; //how much time needed to orbit drop
        private TextAnchor TWR1DefautTextAlign; //store default text alignment to reset it after GUI frame draws
        private TextAnchor TWR1DefaultTextFieldAlign; //same^
        private bool TWR1KASDetect = false; //is KAS installed?
        private string TWR1ControlOffText; //set text string that displays when "Control Off"
        private double TWR1SpeedStep = 1f; //Size of speed change per tap, default to 1m/s
        private string TWR1SpeedStepString; //speed step as string for GUI text entry
        private Texture2D TWR1SettingsIcon = new Texture2D(20, 22, TextureFormat.ARGB32, false); //toolbar icon texture
        private Rect TWR1SettingsWin = new Rect(500, 500, 200, 145);  //settings window position
        private bool TWR1SettingsShow = false; //show settings window?
        private bool TWR1SelectingKey = false; //are we selecting a new key?
        private double TWR1LastVel; //vessel vertical velocity last physics frame
        private double TWR1DesiredAccelThrustLast = 0; //desired thrust last physics frame
        private double TWR1ThrustDiscrepancy; //difference in kN between thrusts last frame
        private double TWR1LastFrameActualThrust; //actual "thrust" last frame, includes both engine and aerodynamic lift
        private Queue<double> TWR1ThrustQueue; //last 5 frames of thrust to average out, it's too bouncy to use just last frame
        private float ThrustUnderRun = 0; //difference between requested and actual thrust, hello jet engines
        GameObject lineObj = new GameObject("Line");
        LineRenderer theLine = new LineRenderer();
        private Timer showLineTime;
        private static bool timerElapsed = false;
        private bool timerRunning = false;
        Vector3 TWR1ControlUp;
        public static int ControlDirection = 0; //control direction for up, 0 is for rockets, 1 for cockpits, 2 through 5 the other directions.
        private Part LastVesselRoot; //saved vessel last update pass, use rootpart for check
        private KeyCode throttleUp;
        private KeyCode throttleDown;
        private KeyCode throttleCut;
        
        public class VslTime
        {
            public Vessel vsl;
            public double time;
            public bool landed;
            public bool stable;
        } //class for 15second landed delay
        public static List<VslTime> SCVslList; //vessel list
        private bool TWR1VesselActive = true; //do we have a vessel to control?
        TWR1Data rootTWR1Data;
        

        //public void Awake() //Awake runs on mod load
        //{

        //}

        public Vector3 SetDirection()
        {
            if (ControlDirection == 0)
            {
                return (TWR1Vessel.rootPart.transform.up);
            }
            if (ControlDirection == 1)
            {
                return (TWR1Vessel.rootPart.transform.forward);
            }
            if (ControlDirection == 2)
            {
                return (-TWR1Vessel.rootPart.transform.up);
            }
            if (ControlDirection == 3)
            {
                return (-TWR1Vessel.rootPart.transform.forward);
            }
            if (ControlDirection == 4)
            {
                return (TWR1Vessel.rootPart.transform.right);
            }
            if (ControlDirection == 5)
            {
                return (-TWR1Vessel.rootPart.transform.right);
            }
            else
            {
                return (TWR1Vessel.rootPart.transform.up);
            }
        }
        
        public void Start() //Start runs on mod start, after all other mods loaded
        {
            
            
            TWR1SettingsIcon = GameDatabase.Instance.GetTexture("Diazo/TWR1/TWR1Settings", false); //load toolbar icon
            SCVslList = new List<VslTime>(); //initialize SkyCrane vesse list
            TWR1ThrustQueue = new Queue<double>();  // initilize ThrustQueue for lift compensation
            //if (!CompatibilityChecker.IsCompatible()) //run compatiblity check
            //{
            //    TWR1ControlOffText = "Control Off (Mod Outdated)"; //if mod outdated, display it
            //}
            //else
            //{
            //    TWR1ControlOffText = "Control Off";
            //}

            RenderingManager.AddToPostDrawQueue(0, OnDraw); //add call to GUI routing
            TWR1Node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg"); //load .cfg file
            TWR1KeyCodeString = TWR1Node.GetValue("TWR1Key"); //read keybind from .cfg, no functionality to set keybind from ingame exists yet
            TWR1KeyCode = (KeyCode)Enum.Parse(typeof(KeyCode), TWR1KeyCodeString); //convert from text string to KeyCode item
            
            TWR1SpeedStep = (float)Convert.ToDecimal(TWR1Node.GetValue("TWR1Step")); //load speed step size from file
            

            if (TWR1Node.GetValue("TWR1KASDisable") == "true") //force SkyCrane mode off
            {
                TWR1KASDetect = false;
            }
            else if (TWR1Node.GetValue("TWR1KASForce") == "true") //force SkyCrane mode on
            {
                TWR1KASDetect = true;
            }
            else
            {
                foreach (AssemblyLoader.LoadedAssembly Asm in AssemblyLoader.loadedAssemblies) //auto detect KAS for Skycrane
                {
                    if (Asm.dllName == "KAS")
                    {
                        TWR1KASDetect = true;
                    }

                }
            }

            TWR1WinStyle = new GUIStyle(HighLogic.Skin.window); //GUI skin style
            try
            {
                TWR1WinPosHeight = Convert.ToInt32(TWR1Node.GetValue("TWR1WinY")); //get saved window position
            }
            catch
            {
                TWR1WinPosHeight = (int)(Screen.height * .1); 
            }
            try
            {
                TWR1WinPosWidth = Convert.ToInt32(TWR1Node.GetValue("TWR1WinX")); //get saved window position
            }
            catch
            {
                TWR1WinPosWidth = (int)(Screen.height * .1); //set window to 10% from top if fail
            }


            TWR1WinPos = new Rect(TWR1WinPosWidth, TWR1WinPosHeight, 215, 180); //set window position
            TWR1SettingsWin = new Rect(TWR1WinPosWidth + 218, TWR1WinPosHeight, 200, 180); //set settings window position to just next to main window
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {

                
                TWR1Btn = ToolbarManager.Instance.add("TWR1", "TWR1Btn");
                TWR1Btn.TexturePath = "Diazo/TWR1/icon_button";
                TWR1Btn.ToolTip = "Vertical Velocity Control";
                TWR1Btn.OnClick += (e) =>
                {
                    TWR1Show = !TWR1Show;
                };
            }
            showLineTime = new System.Timers.Timer(3000);
            //showLineTime.Interval = 3;
            showLineTime.Elapsed += new ElapsedEventHandler(LineTimeOut);
            showLineTime.AutoReset = false;
            

            theLine = lineObj.AddComponent<LineRenderer>();
            theLine.material = new Material(Shader.Find("Particles/Additive"));
            theLine.SetColors(Color.red, Color.red);
            theLine.SetWidth(0, 0);
            theLine.SetVertexCount(2);
            theLine.useWorldSpace = false;
            LastVesselRoot = new Part();
            
        }

        public void LineTimeOut(System.Object source, ElapsedEventArgs e)
        {
            timerElapsed = true; //timer runs in seperate thread, set static bool to true to get back to our main thread
        }
        
        public void ShowLine()
        {
            showLineTime.Start();
            timerRunning = true;
            theLine.SetWidth(1, 0);
            

   
        }

        public void HideLine()
        {
            theLine.SetWidth(0, 0);
            
        }

        public void OnDisable()
        {

            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving flight scene
            {
                TWR1Btn.Destroy();
            }
            TWR1Node.SetValue("TWR1WinX", TWR1WinPos.x.ToString()); //save window position
            TWR1Node.SetValue("TWR1WinY", TWR1WinPos.y.ToString());//same^
            TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");//same^
            
        }


        public void OnDraw()
        {
            if (TWR1Show) //show window?
            {
                TWR1WinPos = GUI.Window(673467798, TWR1WinPos, OnWindow, "Vertical Velocity (Key:" + TWR1KeyCode.ToString() + ")", TWR1WinStyle);
                if (TWR1SettingsShow) //show settings window?
                {
                    TWR1SettingsWin = GUI.Window(673467799, TWR1SettingsWin, OnSettingsWindow, "Settings", TWR1WinStyle);
                }
            }
            
        }

        public void OnSettingsWindow(int WindowID)
        {
            TWR1ContentColor = GUI.contentColor; //set defaults to reset them at end
            TWR1DefautTextAlign = GUI.skin.label.alignment; //same^
            TWR1DefaultTextFieldAlign = GUI.skin.textField.alignment;//same^
            if (TWR1SelectingKey) //are we selecting a new key binding?
            {
                if (Event.current.keyCode != KeyCode.None) //wait for keypress
                {
                    TWR1KeyCode = Event.current.keyCode; //assign new key
                    
                    TWR1SelectingKey = false; //no longer selecting a new key binding
                    TWR1KeyCodeString = TWR1KeyCode.ToString(); //save new keybinding
                    TWR1Node.SetValue("TWR1Key", TWR1KeyCodeString);//same^
                    TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");//same^
                }
                GUI.Label(new Rect(10, 30, 150, 25), "Press New Key"); //change GUI to indicate we are waiting for key press
                if (GUI.Button(new Rect(110, 30, 100, 25), "Cancel")) //cancel key change
                {
                    TWR1SelectingKey = false; 
                }
            }
            else  //not selecting a new key so display normal settings window
            {
                GUI.Label(new Rect(10, 30, 150, 25), "Key: " + TWR1KeyCode.ToString()); //current key and option to change
                if (GUI.Button(new Rect(80, 30, 90, 25), "Change Key"))//select new key?
                {
                    TWR1SelectingKey = true;
                }
            }
            GUI.Label(new Rect(10, 60, 150, 25), "Scycrane Mode:"); //skycrane mode settings
            
            if (TWR1Node.GetValue("TWR1KASDisable") == "false" && TWR1Node.GetValue("TWR1KASForce") == "false") //in auto, so green
            {
                
                GUI.contentColor = Color.green;
            }
            if (GUI.Button(new Rect(10, 80, 57, 25), "Auto")) //change to auto mode
            {
                foreach (AssemblyLoader.LoadedAssembly Asm in AssemblyLoader.loadedAssemblies) //run auto mode check
                {
                    if (Asm.dllName == "KAS")
                    {
                        TWR1KASDetect = true;
                    }

                }
                TWR1Node.SetValue("TWR1KASDisable", "false"); //save change
                TWR1Node.SetValue("TWR1KASForce", "false");//same^
                TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");//same^
            }
            
            GUI.contentColor = TWR1ContentColor; //reset color
            if (TWR1Node.GetValue("TWR1KASDisable") == "false" && TWR1Node.GetValue("TWR1KASForce") == "true") //skycrane forced on? green text
            {
                GUI.contentColor = Color.green;
            }
            if (GUI.Button(new Rect(67, 80, 57, 25), "On")) //force skycrane mode on
            {
                TWR1Node.SetValue("TWR1KASDisable", "false"); //save change
                TWR1Node.SetValue("TWR1KASForce", "true");//same^
                TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");//same^
            }
            GUI.contentColor = TWR1ContentColor;//reset color
            if (TWR1Node.GetValue("TWR1KASDisable") == "true" && TWR1Node.GetValue("TWR1KASForce") == "false") //skycrane forced off? green text
            {
                GUI.contentColor = Color.green;
            }
            if (GUI.Button(new Rect(124, 80, 57, 25), "Off")) //force skycrane mode off
            {
                TWR1Node.SetValue("TWR1KASDisable", "true"); //save change
                TWR1Node.SetValue("TWR1KASForce", "false");//same^
                TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");//same^
            }
            GUI.contentColor = TWR1ContentColor; //reset color

            GUI.Label(new Rect(10, 110, 150, 25), "Velocity Step Size:");
            TWR1SpeedStepString = TWR1SpeedStep.ToString(); //text box requires a string, not a number
            GUI.skin.label.alignment = TextAnchor.MiddleRight; //these lines are for that conversion back and forth
            GUI.skin.textField.alignment = TextAnchor.MiddleRight;//same^
            TWR1SpeedStepString = GUI.TextField(new Rect(130, 110, 50, 25), TWR1SpeedStepString, 5);//same^
            try //try converting characters in text box to string
            {
                TWR1SpeedStep = (float)Convert.ToDecimal(TWR1SpeedStepString); //conversion ok, apply and save change
                TWR1Node.SetValue("TWR1Step", TWR1SpeedStep.ToString());
                TWR1Node.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/TWR1/TWR1.cfg");
            }
            catch //fail converting characters
            {
                TWR1SpeedStepString = TWR1SpeedStep.ToString(); //undo any changes
                GUI.FocusControl(""); //non-number key was pressed, give focus back to ship control
            }

            if (GUI.Button(new Rect(10, 140, 70, 25), "Direction")) //force skycrane mode off
            {
                ShowLine();
            }
            if (GUI.Button(new Rect(80, 140, 23, 20), "U")) //force skycrane mode off
            {
                ControlDirection = 0;
                rootTWR1Data.controlDirection = 0;
                ShowLine();
            }
            if (GUI.Button(new Rect(80, 160, 23, 20), "D")) //force skycrane mode off
            {
                ControlDirection = 2;
                rootTWR1Data.controlDirection = 2;
                ShowLine();
            }
            if (GUI.Button(new Rect(103, 140, 23, 25), "F")) //force skycrane mode off
            {
                ControlDirection = 3;
                rootTWR1Data.controlDirection = 3;
                ShowLine();
            }
            if (GUI.Button(new Rect(126, 140, 23, 25), "B")) //force skycrane mode off
            {
                ControlDirection = 1;
                rootTWR1Data.controlDirection = 1;
                ShowLine();
            }
            if (GUI.Button(new Rect(103, 160, 23, 25), "L")) //force skycrane mode off
            {
                ControlDirection = 5;
                rootTWR1Data.controlDirection = 5;
                ShowLine();
            }
            if (GUI.Button(new Rect(126, 160, 23, 25), "R")) //force skycrane mode off
            {
                ControlDirection = 4;
                rootTWR1Data.controlDirection = 4;
                ShowLine();
            }

            GUI.skin.textField.alignment = TWR1DefaultTextFieldAlign; //reset GUI skin stuff
            GUI.skin.label.alignment = TWR1DefautTextAlign;//same^
            GUI.contentColor = TWR1ContentColor;//same^
            GUI.DragWindow(); //window is draggable
        }

        public void OnWindow(int WindowID) //main VertVel window
        {


            TWR1ContentColor = GUI.contentColor; //grab defaults
            TWR1DefautTextAlign = GUI.skin.label.alignment;//same^
            TWR1DefaultTextFieldAlign = GUI.skin.textField.alignment;//same^
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            GUI.Label(new Rect(10, 40, 150, 20), "Velocity Setpoint(m/s): ");
            GUI.Label(new Rect(10, 25, 150, 20), "Current Velocity(m/s): ");
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(145, 25, 60, 20), TWR1VelocityCurrent.ToString("##0.00"));
            //velocity setpoint value changes format depending on mode
            if (TWR1HeightCtrl) //in height control mode, display "Auto"
            {
                GUI.Label(new Rect(145, 40, 60, 20), "Auto");
            }
            else if (TWR1Engaged) //in velocity control, display velocity setpoint
            {
                GUI.Label(new Rect(145, 40, 60, 20), TWR1VelocitySetpoint.ToString("##0.00"));
            }
            else //mod off, display "---.--"
            {
                GUI.Label(new Rect(145, 40, 60, 20), "---.--");
            }
            if (GUI.Button(new Rect(7, 65, 50, 40), "Off")) //button to turn mod off
            {
                TWR1Engaged = false;
                TWR1HeightCtrl = false;

            }
            if (GUI.Button(new Rect(57, 65, 50, 40), "Zero\nVel.")) //button to zero velocity
            {
                TWR1Engaged = true;
                TWR1HeightCtrl = false;
                TWR1VelocitySetpoint = 0f;
            }
            if (GUI.Button(new Rect(107, 65, 50, 40), "+"+ TWR1SpeedStep)) //button to increase velocity, display value of change (SpeedStep)
            {
                if (TWR1Engaged) //if mod is engaged already, add speedstep to velocity setpoint
                {
                    TWR1HeightCtrl = false;
                    TWR1VelocitySetpoint += TWR1SpeedStep;
                }
                else //if mod is not engaged, add speedstep to current velocity and make that the velocity setpoint
                {
                    TWR1Engaged = true;
                    TWR1HeightCtrl = false;
                    TWR1VelocitySetpoint = TWR1VelocityCurrent + TWR1SpeedStep;
                }
            }

            if (GUI.Button(new Rect(157, 65, 50, 40), "-" + TWR1SpeedStep))//button to decrease velocity, display value of change (SpeedStep)
            {
                if (TWR1Engaged)//if mod is engaged already, subtract speedstep from velocity setpoint
                {
                    TWR1HeightCtrl = false;
                    TWR1VelocitySetpoint -= TWR1SpeedStep;
                }
                else//if mod is not engaged, subtract speedstep from current velocity and make that the velocity setpoint
                {
                    TWR1Engaged = true;
                    TWR1HeightCtrl = false;
                    TWR1VelocitySetpoint = TWR1VelocityCurrent - TWR1SpeedStep;
                }
            }

            //height control button
            if (TWR1HC1Thrust >= 0f) //1% thrust is a TWR of greater then 1, probably SRBs. Can not enable Height Control
            {
                GUI.contentColor = Color.red;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(107, 110, 100, 40), "TWR\nHIGH");
                GUI.contentColor = TWR1ContentColor;
            }
            else if (TWR1HC80Thrust <= 0f) //80% thrust is not a TWR of greater then 1. Can not enable Height Control
            {
                GUI.contentColor = Color.red;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(107, 110, 100, 40), "TWR\nLOW");
                GUI.contentColor = TWR1ContentColor;
            }
            else if (TWR1VesselPitch <= 55f && TWR1HCOrbitDrop == false && TWR1Engaged == true) //vessel is angled a long way off vertical, warn player mod may not work
            {
                GUI.contentColor = Color.yellow;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(107, 110, 100, 40), "OVER\nPITCH");
                GUI.contentColor = TWR1ContentColor;
            }
            else if (TWR1HeightCtrl) //are we in height control mode?
            {
                if (TWR1HCOrbitDrop == true) //orbit drop in progress
                {
                    if (TWR1HCThrustWarningTime == 0) //have not hit ThrustWarning altitude yet
                    {
                        GUI.contentColor = Color.green;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(new Rect(107, 110, 100, 40), "Free\nPitch");
                        GUI.contentColor = TWR1ContentColor;
                    }
                    else if (TWR1HCThrustWarningTime != 0) //hit ThrustWarning altitude, warn player they have to upright their ship
                    {
                        GUI.contentColor = Color.yellow;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(new Rect(107, 110, 100, 40), "THRUST\nWARNING");
                        GUI.contentColor = TWR1ContentColor;
                    }
                }
                else //orbit drop is not in progress, height control mode engaged
                {
                    GUI.contentColor = Color.green;
                    if (GUI.Button(new Rect(107, 110, 100, 40), "In\nAuto"))
                    {
                        TWR1HeightCtrl = false;
                        TWR1VelocitySetpoint = 0f;
                    }
                    GUI.contentColor = TWR1ContentColor;
                }


            }

            else if (!TWR1HeightCtrl) //not in height control mode
            {
                if (TWR1OrbitDropAllow == true) //can enter orbitdrop mode
                {
                    if (GUI.Button(new Rect(107, 110, 100, 40), "Auto Height\n(Free)"))
                    {
                        TWR1HeightCtrl = true;
                        TWR1Engaged = true;
                        TWR1HCOrbitDrop = true;
                        TWR1HCThrustWarningTime = 0;

                    }
                }
                else //too low for orbit drop, but can enter normal height control mode
                {
                    if (GUI.Button(new Rect(107, 110, 100, 40), "Auto Height\n(Now)"))
                    {
                        TWR1HeightCtrl = true;
                        TWR1Engaged = true;
                        TWR1HCOrbitDrop = false;
                    }
                }
            }


            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(7, 110, 50, 20), "Altitude:");
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            if (TWR1HCToGround > 50000) //are we really high? disaply orbit due to character limit concerns
            {
                GUI.Label(new Rect(47, 110, 49, 20), "Orbit");
            }
            else
            {
                GUI.Label(new Rect(47, 110, 49, 20), TWR1HCToGround.ToString("#####0"));
            }
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(7, 130, 40, 20), "Fly to:"); //fly to altitude, GUI text box requires string, not number
            TWR1HCTargetString = TWR1HCTarget.ToString();//same^
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUI.skin.textField.alignment = TextAnchor.MiddleRight;
            TWR1HCTargetString = GUI.TextField(new Rect(47, 130, 50, 20), TWR1HCTargetString, 5);//same^
            try//same^
            {
                TWR1HCTarget = Convert.ToInt32(TWR1HCTargetString); //convert string to number
            }
            catch//same^
            {
                TWR1HCTargetString = TWR1HCTarget.ToString(); //conversion failed, reset change
                GUI.FocusControl(""); //non-number key pressed, return control focus to vessel
            }

            //bottom button displaying mode mod is in
            if (!TWR1VesselActive)
            {
                if (GUI.Button(new Rect(10, 153, 175, 25), "No Active Vessel"))
                {
                    TWR1HeightCtrl = false;
                    TWR1Engaged = false;
                    TWR1Show = false;
                }
            }

            else if (TWR1HeightCtrl)
            {
                GUI.contentColor = Color.green;
                if (GUI.Button(new Rect(10, 153, 175, 25), "Height Control Engaged"))
                {
                    TWR1HeightCtrl = false;
                }
                GUI.contentColor = TWR1ContentColor;
            }
            else if (TWR1Engaged)
            {
                GUI.contentColor = Color.green;
                if (GUI.Button(new Rect(10, 153, 175, 25), "Velocity Control Engaged"))
                {
                    TWR1HeightCtrl = false;
                    TWR1Engaged = false;
                }
                GUI.contentColor = TWR1ContentColor;
            }
            else //if (!TWR1HCArmed)
            {

                if (GUI.Button(new Rect(15, 153, 175, 25), "Control Off")) //text displayed changes if mod is out of date
                {
                    TWR1Engaged = false;
                    TWR1Show = false;
                }

            }
            
            if(GUI.Button(new Rect(185,153,25,25), TWR1SettingsIcon)) //settings button
            {
                TWR1SettingsShow = !TWR1SettingsShow;
                TWR1SettingsWin.x = TWR1WinPos.x + 218;
                TWR1SettingsWin.y = TWR1WinPos.y;
            }

            GUI.skin.textField.alignment = TWR1DefaultTextFieldAlign; //reset text defaults
            GUI.skin.label.alignment = TWR1DefautTextAlign; //same^
            GUI.contentColor = TWR1ContentColor;//same^
            GUI.DragWindow(); //window is draggable

        }

        public void Update()
        {

            if (timerElapsed)
            {
                HideLine();
                timerElapsed = false;
                timerRunning = false;
            }
            if (timerRunning)
            {
                theLine.transform.parent = TWR1Vessel.rootPart.transform;
                theLine.transform.localPosition = Vector3.zero;
                theLine.transform.rotation = Quaternion.identity;
                theLine.SetPosition(0, new Vector3(0, 0, 0));
                theLine.SetPosition(1, TWR1ControlUp * 50);
            }
            
            

            if (Input.GetKeyDown(TWR1KeyCode) == true) //Does the Z key get pressed, enabling this mod? Note this is only true on the first Update cycle the key is pressed.
            {
               
                TWR1Show = true;
                if (TWR1Engaged == false) //TWR1 not engaged when Z pressed so input our current velocity into velocity setpoint
                {
                    TWR1VelocitySetpoint = (float)TWR1Vessel.verticalSpeed;
                }
                TWR1KeyDown = true; //TWR1 key is down
                TWR1Engaged = true; //TWR1 Engaged, auto control on
                //TWR1ThrottleWhileKeyDown = false; //Z key was just pressed so throttle key can not have been pressed yet

                TWR1HeightCtrl = false; //turn off height control if engaged
                TWR1HCOrbitDrop = false;
            }

            if (Input.GetKeyUp(TWR1KeyCode) == true) //TWR1 Key just got released.
            {
                TWR1KeyDown = false; //TWR1 key is no longer held down
            }


            if (GameSettings.THROTTLE_UP.GetKeyDown() == true && TWR1Engaged == true) //throtlle up key pressed, TWR1 engaged
            {
                if (TWR1HCOrbitDrop) //orbit drop in progress
                {
                    //do nothing, allow KSP to control throttle
                }
                else if (TWR1KeyDown == false) //Z key not down, disengage TWR1
                {

                    TWR1Engaged = false; //disengage TWR1 

                }

                else if (TWR1KeyDown == true) //Z key down, increse desired accel by 1 m/s^2
                {
                    TWR1VelocitySetpoint += TWR1SpeedStep;


                    //TWR1ThrottleWhileKeyDown = true; //throttle key pressed while TWR1 keydown
                    TWR1HeightCtrl = false;
                }
            }

            if (GameSettings.THROTTLE_DOWN.GetKeyDown() == true && TWR1Engaged == true) //throtlle down key pressed, TWR1 engaged
            {
                if (TWR1HCOrbitDrop) //orbit drop in progress
                {
                    //do nothing, allow KSP to control throttle
                }
                else if (TWR1KeyDown == false) ////Z key not down, disengage TWR1
                {

                    TWR1Engaged = false; //disengage TWR1

                }
                else if (TWR1KeyDown == true)//Z key down, decrease desired accel by 1 m/s^2
                {
                    TWR1VelocitySetpoint -= TWR1SpeedStep;

                    //TWR1ThrottleWhileKeyDown = true; //throttle key pressed while TWR1 keydown
                    TWR1HeightCtrl = false;

                }
            }

            if (GameSettings.THROTTLE_CUTOFF.GetKeyDown() == true && TWR1Engaged == true)
            {
                if (TWR1HCOrbitDrop) //height control engaged and doing orbit drop
                {
                    //do nothing, allow KSP to control throttle
                }

                else if (TWR1KeyDown == false) ////Z key not down, disengage TWR1
                {
                    TWR1Engaged = false; //disengage TWR1
                    FlightInputHandler.state.mainThrottle = 0f; //throttle cut off hit, player will not necessarily hold it down like they will throttle up/down so zero the throttle

                }
                else if (TWR1KeyDown == true)//Z key down, set vert vel to 0
                {
                    TWR1VelocitySetpoint = 0f;
                    //TWR1ThrottleWhileKeyDown = true; //throttle key pressed while TWR1 keydown
                    TWR1HeightCtrl = false;

                }

            }


        }




        private void TWR1Skycrane()
        {
            
            ResetSCV:
            if (SCVslList.Count > 0)
            {
                foreach (VslTime SCV in SCVslList) //Has the 15 seconds elapsed to turn stable true?
                {
                    if (SCV.vsl.state == Vessel.State.DEAD)
                    {
                        SCVslList.Remove(SCV);
                        goto ResetSCV;
                    }
                    if (Planetarium.GetUniversalTime() - SCV.time > 15)
                    {
                        SCV.stable = true;
                    }
                    
                    if (Vector3.Distance(SCV.vsl.findWorldCenterOfMass(), TWR1Vessel.findWorldCenterOfMass()) > 3000) //clean up SCVslList, load distance is 2500 so 3000 buffer here.
                    {
                        SCVslList.Remove(SCV);
                        goto ResetSCV;

                    }
                   
                   
                }
            }
           
           
            foreach (Vessel ves in FlightGlobals.Vessels) //cycle through all active vessels, I don't know of anyway to list only loaded vessels.
            {
                if (ves.loaded == true) //only parse loaded vessels that are not the current vessel
                {
                FindVessel:
                    
                    VslTime FndVsl = SCVslList.Find(v => v.vsl == ves); //match vessels from the two lists
                    
                    if (FndVsl == null) //populate SCVslList
                    {
                        SCVslList.Add(new VslTime { vsl = ves, time = Planetarium.GetUniversalTime(), landed = ves.Landed, stable = true });
                        goto FindVessel;
                    }
                    
                    if (FndVsl.landed == false && !ves.isActiveVessel) //if vessel is flying as per FndVsl, add it's mass if not active vessel
                    {
                        
                        TWR1Mass = TWR1Mass + ves.GetTotalMass();
                    }
                    
                    if(FndVsl.landed != ves.Landed) //is the landed variable different?
                    {
                        
                        if (FndVsl.stable == true) //vessel is stable
                        {
                            
                            FndVsl.landed = ves.Landed;
                            
                            FndVsl.stable = false;
                            
                            FndVsl.time = Planetarium.GetUniversalTime();
                            
                        }
                        //do nothing if vessel unstable is true as that is within 15 seconds buffer
                    }
                
                }

               
            }
            
            
           

            
        }
        public void FixedUpdate() //forum says "all physics calculations should be on FixedUpdate, not Update". not sure a throttle adjustment qualifies as a physics calc, but put it here anyway
        {

            
            

            if (!TWR1Engaged) { TWR1HeightCtrl = false; } //mod has been disengegaed, disengage height control
            if (!TWR1HeightCtrl)
            {
                TWR1HCOrbitDrop = false; //Height control not engaged, we can not be doing an OrbitDrop

                TWR1HCFullThrustUp = false;
            }

            TWR1Vessel = FlightGlobals.ActiveVessel; //Set vessel to active vessel
            if (TWR1Vessel.rootPart != LastVesselRoot)
            {
                print("TWR1 Root Part Changed");

                if (!TWR1Vessel.rootPart.Modules.Contains("TWR1Data"))
                {
                    print("TWR1 Module not found");
                    TWR1Vessel.rootPart.AddModule("TWR1Data");
                }
                rootTWR1Data = (TWR1Data)TWR1Vessel.rootPart.Modules.OfType<TWR1Data>().First();
                ControlDirection = rootTWR1Data.controlDirection;
                LastVesselRoot = TWR1Vessel.rootPart;
                //print("TWR1 Ctrl " + ControlDirection);
            }

           // print("Ctr; " + ControlDirection + " " + rootTWR1Data.controlDirection);

            if (TWR1Vessel.state == Vessel.State.DEAD)
            {
                TWR1VesselActive = false;
                TWR1Engaged = false;
                goto VesselDead;
            }
            else
            {
                TWR1VesselActive = true;
            }
           

            TWR1MaxThrust = 0f; //maxthrust reset
            TWR1MinThrust = 0f; //minthrust reset
            ThrustUnderRun = 0f; //thrust dif reset
            TWR1SOI = TWR1Vessel.mainBody; //set body we are orbiting
            TWR1MassLast = TWR1Mass;
            TWR1Mass = TWR1Vessel.GetTotalMass(); //vessel's total mass, including resources

            if (TWR1KASDetect && !TWR1OrbitDropAllow && !TWR1HCOrbitDrop) //Skycrane enabled, check other vessels
            {
               
                TWR1Skycrane();
            }

            TWR1GravHeight = (float)TWR1Vessel.altitude + (float)TWR1SOI.Radius; //gravity force at this altitude (not in m/s^2)
            TWR1GravForce = (float)TWR1SOI.gMagnitudeAtCenter / (float)Math.Pow(TWR1GravHeight, 2); //accel down due to gravity in m/s^2
            TWR1ThrottleRead = FlightInputHandler.state.mainThrottle; //readback current throttle

            //next 6 lines find vessel pitch, copy-pasted from MechJeb so no clue what these lines are acutally doing.
            //Used under Mechjeb's GPL 3 license.
            //version 1.11: removed mechjeb code, used own method now for up
            TWR1CoM = TWR1Vessel.findWorldCenterOfMass();
            TWR1Up = (TWR1CoM - TWR1Vessel.mainBody.position).normalized;
            //TWR1North = Vector3d.Exclude(TWR1Up, (TWR1Vessel.mainBody.position + TWR1Vessel.mainBody.transform.up * (float)TWR1Vessel.mainBody.Radius) - TWR1CoM).normalized;
           // TWR1RotSurf = Quaternion.LookRotation(TWR1North, TWR1Up);
            //TWR1RotVesselSurf = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(TWR1Vessel.GetTransform().rotation) * TWR1RotSurf);
           // TWR1VesselPitch = (TWR1RotVesselSurf.eulerAngles.x > 180) ? (360.0 - TWR1RotVesselSurf.eulerAngles.x) : -TWR1RotVesselSurf.eulerAngles.x; //vessel pitch found
            //end of code from Mechjeb
            TWR1ControlUp = SetDirection();
            TWR1OffsetVert = Vector3.Angle(TWR1Up, TWR1ControlUp);
            TWR1VesselPitch = Math.Max((90 - TWR1OffsetVert), 0);
            //print("pitch " + TWR1OffsetVert);
            //switch to just using local up? as defined by player?
            //print("loc " + TWR1Vessel.rootPart.transform.position + " " + TWR1Vessel.rootPart.transform.rotation + " " + TWR1OffsetVert);
            //print("pitch " + TWR1VesselPitch);
            //TWR1OffsetVert = Math.Max(Math.Min(Math.Abs(TWR1VesselPitch - 90), 89), 0); //change vessel pitch to angle off vertical for thrust compensation
            //print("pitch " + TWR1OffsetVert+ " " + AngleTest);
            TWR1OffsetVertRadian = Mathf.Deg2Rad * TWR1OffsetVert; //mathf.cos takes radians, not degrees, ask unity why
            TWR1OffsetVertRatio = Math.Cos(TWR1OffsetVertRadian); //our compensation factor for being offset from vertical
            TWR1HCDistance = Math.Abs(TWR1HCToGround - TWR1HCTarget); //absolute distance to target altitude

            foreach (Part part in TWR1Vessel.Parts) //go through each part on vessel
            {
                
               
                
                
                if (part.Modules.Contains("ModuleEngines") | part.Modules.Contains("ModuleEnginesFX")) //is part an engine?
                {
                    float DavonThrottleID = 0;
                    if(part.Modules.Contains("DifferentialThrustEngineModule")) //Devon Throttle Control Installed?
                    {
                        foreach(PartModule pm in part.Modules)
                        {
                            
                            if (pm.moduleName=="DifferentialThrustEngineModule")
                            {
                                DavonThrottleID = (float)pm.Fields.GetValue("throttleFloatSelect"); //which throttle is engine assigned to?
                            }
                        }

                    }
                    if(DavonThrottleID == 0f)
                    {
                    foreach (PartModule TWR1PartModule in part.Modules) //change from part to partmodules
                    {
                        
                        if (TWR1PartModule.moduleName == "ModuleEngines") //find partmodule engine on th epart
                        {
                           
                            TWR1EngineModule = (ModuleEngines)TWR1PartModule; //change from partmodules to moduleengines
                            //print("TWR1 " + TWR1EngineModule.currentThrottle + " " + TWR1EngineModule.requestedThrottle);
                            if ((bool)TWR1PartModule.Fields.GetValue("throttleLocked") && TWR1EngineModule.isOperational)//if throttlelocked is true, this is solid rocket booster. then check engine is operational. if the engine is flamedout, disabled via-right click or not yet activated via stage control, isOperational returns false
                            {
                                TWR1MaxThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModule.thrustPercentage / 100F; //add engine thrust to MaxThrust
                                TWR1MinThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModule.thrustPercentage / 100F; //add engine thrust to MinThrust since this is an SRB
                            }
                            else if (TWR1EngineModule.isOperational)//we know it is an engine and not a solid rocket booster so:
                            {
                                TWR1MaxThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModule.thrustPercentage / 100F; //add engine thrust to MaxThrust
                                TWR1MinThrust += (float)(TWR1PartModule.Fields.GetValue("minThrust")) * TWR1EngineModule.thrustPercentage / 100F; //add engine thrust to MinThrust, stock engines all have min thrust of zero, but mods may not be 0
                                ThrustUnderRun = +(TWR1EngineModule.requestedThrottle - TWR1EngineModule.currentThrottle) * TWR1EngineModule.maxThrust;
                            }
                        }
                        else if (TWR1PartModule.moduleName == "ModuleEnginesFX") //find partmodule engine on th epart
                        {

                            TWR1EngineModuleFX = (ModuleEnginesFX)TWR1PartModule; //change from partmodules to moduleengines
                            if ((bool)TWR1PartModule.Fields.GetValue("throttleLocked") && TWR1EngineModuleFX.isOperational)//if throttlelocked is true, this is solid rocket booster. then check engine is operational. if the engine is flamedout, disabled via-right click or not yet activated via stage control, isOperational returns false
                            {
                                TWR1MaxThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModuleFX.thrustPercentage / 100F; //add engine thrust to MaxThrust
                                TWR1MinThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModuleFX.thrustPercentage / 100F; //add engine thrust to MinThrust since this is an SRB
                            }
                            else if (TWR1EngineModuleFX.isOperational)//we know it is an engine and not a solid rocket booster so:
                            {
                                TWR1MaxThrust += (float)(TWR1PartModule.Fields.GetValue("maxThrust")) * TWR1EngineModuleFX.thrustPercentage / 100F; //add engine thrust to MaxThrust
                                TWR1MinThrust += (float)(TWR1PartModule.Fields.GetValue("minThrust")) * TWR1EngineModuleFX.thrustPercentage / 100F; //add engine thrust to MinThrust, stock engines all have min thrust of zero, but mods may not be 0
                                ThrustUnderRun = +(TWR1EngineModuleFX.requestedThrottle - TWR1EngineModuleFX.currentThrottle) * TWR1EngineModuleFX.maxThrust;
                            }
                        }

                    }
                }
                }
               
            }
           
            if (TWR1MaxThrust < 1) //if MaxThrust is zero, a divide by zero error gets thrown later, so...
            {
                TWR1MaxThrust = 1; //set MaxThrust to at least 1 to avoid this
            }
            TWR1VelocityCurrent = TWR1Vessel.verticalSpeed; //set our current vertical velocity
            //if (TWR1Vessel.mainBody.bodyName == "Kerbin" || TWR1Vessel.mainBody.bodyName == "Laythe")
            //{
            //    if (TWR1Vessel.heightFromTerrain >= 0)
            //    {
            //        TWR1HCToGround = (float)Math.Min(TWR1Vessel.altitude, TWR1Vessel.heightFromTerrain); //use shorter distance, ship to sea level (altitude) or terrain (above ground/building). If you are over water, height to terrain will read to the sea bottom, not sea level, resulting in crashes.
            //    }
            //    else
            //    {
            //        TWR1HCToGround = (float)TWR1Vessel.altitude;
            //    }
            //}
            //else
            //{
            //    if (TWR1Vessel.heightFromTerrain >= 0)
            //    {
            //        TWR1HCToGround = (float)TWR1Vessel.heightFromTerrain;
            //    }
            //    else
            //    {
            //        TWR1HCToGround = (float)TWR1Vessel.altitude;
            //    }
            //}
            TWR1HCToGround = heightToLand();

            TWR1HC5Thrust = (Math.Max((TWR1MaxThrust * .05), TWR1MinThrust) / TWR1Mass) - TWR1GravForce; //accel at 5% thrust, makes sure engine is on to allow for ship horizontal speed adjustment. this outside HC method for UI dispaly
            TWR1HC1Thrust = (Math.Max((TWR1MaxThrust * .01), TWR1MinThrust) / TWR1Mass) - TWR1GravForce;
            TWR1HC80Thrust = ((TWR1MaxThrust * .8f) / TWR1Mass) - TWR1GravForce; //use 80% acceleration to account for being off vertical, planet grav reduces accel in this case this outside HC method for UI disaply
            TWR1HeightControl(); //Height control now sets VelocitySetpoint (version 1.5)



            if (TWR1HC80Thrust <= 0 && TWR1HeightCtrl == true || TWR1HC1Thrust >= 0 && TWR1HeightCtrl == true) //is height control on and 1% or 80% no longer valid?
            {
                TWR1HeightCtrl = false; //cancel height control
                TWR1DesiredAccel = 0f - TWR1VelocityCurrent + TWR1GravForce; //we just canceled height control, try to zero velocity
                TWR1VelocitySetpoint = 0f; //set Velocity Setpoint to zero for normal logic next pass
            }


            TWR1VelocityDiff = TWR1VelocitySetpoint - TWR1VelocityCurrent; //find our velocity difference, order is important so that negative velocity is in the correct direction
            TWR1DesiredAccel = TWR1VelocityDiff + TWR1GravForce; //find desired vertical accel, including planets grav. Because velocity is instant, this works to a close enough accuracy for our purposes without getting into PID control or something similar. Include fudge factor


                TWR1DesiredAccelThrust = TWR1DesiredAccel * TWR1Mass; //desired thrust upwards, in kilonewtons


                
            if (FlightGlobals.getStaticPressure() > 0.0001 && !TWR1HeightCtrl) //aerodynamic lift compensation calculation
                {

                    //average out the last physics frame thrusts for aerodynamic lift calculation
                while (TWR1ThrustQueue.Count > 5) //only use last 5 frames so make sure the Queue only has 5 values
                    {
                        TWR1ThrustQueue.Dequeue();
                    }
                TWR1DesiredAccelThrustLast = 0; //reset thrust average
                foreach (double dbl in TWR1ThrustQueue)
                    {
                        TWR1DesiredAccelThrustLast = TWR1DesiredAccelThrustLast + dbl; //add last 5 frames thrust together

                    }
                TWR1DesiredAccelThrustLast = TWR1DesiredAccelThrustLast / TWR1ThrustQueue.Count; //divide by count to get average of desired thrusts over last 5 frames

                    //TWR1LastFrameAccel = ((TWR1Vessel.verticalSpeed - TWR1LastVel) * TWR1FixedUpdatePerSec);
                    TWR1LastFrameActualThrust = ((((TWR1Vessel.verticalSpeed - TWR1LastVel) / Time.fixedDeltaTime)+TWR1GravForce) * TWR1Mass); //get actual thrust of last physics frame, note GravForce has to be present as you are always fighting gravity

                    TWR1ThrustDiscrepancy = TWR1DesiredAccelThrustLast - TWR1LastFrameActualThrust - ThrustUnderRun; //discrepancy between thrusts the last frame,
                    
                    if (Math.Abs(TWR1ThrustDiscrepancy) < TWR1MaxThrust * .2) //only compensate for aerolift if the value is less the 20% of max thrust. If it's more, almost certain that the thrust discrepancy is caused by other factors
                    {
                        TWR1DesiredAccelThrust = TWR1DesiredAccelThrust + TWR1ThrustDiscrepancy; 
                    }
                }

            
            TWR1ThrustUpAngle = TWR1DesiredAccelThrust / TWR1OffsetVertRatio; //compensate for vessel angle off vertical
            TWR1ThrustUp = Math.Max(Math.Min((TWR1ThrustUpAngle - TWR1MinThrust) / (TWR1MaxThrust - TWR1MinThrust), 1), 0); //find percent of current throttle, minimum 0 for no thrust, 1 for max thrust
            
            
            if (TWR1ThrustUp > TWR1ThrottleRead) //throttle damper to limit vessel jolts, going from 0 to 100% thrust from one physics frame to the next can shake a ship apart.
            {
                TWR1ThrustUp = Math.Min(TWR1ThrottleRead + 0.03, TWR1ThrustUp);
            }
            else
            {
                TWR1ThrustUp = Math.Max(TWR1ThrottleRead - 0.03, TWR1ThrustUp);
            }
            TWR1ThrustUp = Math.Max(Math.Min(TWR1ThrustUp, 1), 0); //error catch throttle value, if an invalid value is passed to KSP it screws up.

            
            TWR1OrbitDropTimeNeeded = Math.Abs(TWR1VelocityCurrent) / Math.Abs(TWR1HC80Thrust); //how much time is needed to orbit drop?
            TWR1OrbitDropHeightNeeded = (Math.Abs(TWR1VelocityCurrent) * 40) + (TWR1HC80Thrust * Math.Pow(TWR1OrbitDropTimeNeeded, 2)) / 2; //how much altitude is needed to orbit drop?

            if (TWR1HCDistToTarget > TWR1OrbitDropHeightNeeded && TWR1VelocityCurrent < 0f && TWR1HCDistToTarget > TWR1GravForce * 1000 && TWR1HCToGround > TWR1HCTarget || TWR1VelocityCurrent >= 0f && TWR1HCDistToTarget > TWR1GravForce * 1000) //are we allowed to orbit drop right now? 
            {

                TWR1OrbitDropAllow = true;
            }
            else
            {
                TWR1OrbitDropAllow = false;
            }
           

            if (TWR1Engaged == true) //control throttle?
            {

                if (TWR1HCOrbitDrop == false) //if falling from orbit, do not lock out throttle control
                {
                    FlightInputHandler.state.mainThrottle = (float)TWR1ThrustUp; //set throttle to desired thrust

                }
            }
           
            
            TWR1ThrustQueue.Enqueue(TWR1DesiredAccelThrust - ThrustUnderRun); //add desired thrust value to queue
        VesselDead: //escape for no active vessel
            TWR1LastVel = TWR1Vessel.verticalSpeed; //save velocity from this frame for calculations next frame
          
        }

        public void TWR1HeightControl() //DesiredAccel must account for gravity in this method
        {
            
            TWR1HCDistToTarget = Math.Abs(TWR1HCToGround - TWR1HCTarget); //how far to target? always positive

            if (TWR1HCOrbitDrop) //are we orbit dropping?
            {

                if (TWR1HCThrustWarningTime != 0) //are we in thrust warning?
                {

                    if (TWR1Vessel.missionTime - TWR1HCThrustWarningTime > 15) //check to see if it is time to exit thrust warning mode
                    {
                        TWR1HCOrbitDrop = false;
                        TWR1HCThrustWarningTime = 0;
                        //TWR1HCFreePitch = 0;
                    }

                }
                else //we are not in thrust warning
                {
                    if (TWR1VelocityCurrent < 0f) //are we descending?
                    {

                        if (TWR1OrbitDropHeightNeeded > TWR1HCDistToTarget) //is our current altitude below the altitude needed for height control?
                        {
                            TWR1HCThrustWarningTime = TWR1Vessel.missionTime; //enter thrust warning mode


                        }
                    }
                }
            }
            else //we are not orbit dropping
            {
                if (TWR1HCToGround < TWR1HCTarget) //vessel below target height
                {

                    if (TWR1HCDistToTarget < (TWR1HC80Thrust * 2f)) //are we within 2 times 80% thrust as velocity as distance?
                    {
                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = Math.Min(TWR1GravForce * .3, (TWR1HCDistToTarget * .3)); //close to target height, desired speed is the smaller of 1/5 gravity or one third the distance to target
                        } 
                    }
                    
                    else if (TWR1HCDistToTarget < (TWR1HC80Thrust * 5f)) //are we within 5 times 80% thrust as velocity as distance?
                    {
                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = Math.Min(TWR1GravForce * .5, (TWR1HCDistToTarget * .3)); //close to target height, desired speed is the smaller of half gravity or one third the distance to target
                        } 


                        TWR1HCFullThrustUp = false; //variable thrust



                    }

                    else if (TWR1VelocityCurrent < -0f) //below target height with negative velocity
                    {
                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = Math.Min(TWR1HC80Thrust - TWR1VelocityCurrent, Math.Abs(TWR1HCDistToTarget) * 3); //set thrust to 80%, 'adding' our current velocity. remember downwards is negative. limit to 1/3 the distance to target to avoid overshoots
                            TWR1HCFullThrustUp = false;
                        } 


                    }
                    else //below target height with positive velocity
                    {
                        if (TWR1HCFullThrustUp) //previous frame we were low enough to full thrust
                        {

                            TWR1HCtime = TWR1VelocityCurrent / Math.Abs(TWR1HC5Thrust); //should we exit full thrust to variable thrust?
                            TWR1HCneeded = TWR1VelocityCurrent / 2 * TWR1HCtime; //same^
                            if (TWR1HCneeded > TWR1HCDistToTarget)//same^
                            {
                                TWR1HCFullThrustUp = false; //closing on target, go to variable thrust
                            }
                        }
                        else if (!TWR1HCFullThrustUp) //we were in variable thrust last frame
                        {

                            TWR1HCtime = TWR1VelocityCurrent / Math.Abs(TWR1HC5Thrust); //goto full thrust?
                            TWR1HCneeded = TWR1VelocityCurrent / 2 * TWR1HCtime;//same^
                            if (TWR1HCneeded < TWR1HCDistToTarget)//same^
                            {
                                TWR1HCFullThrustUp = true;//no longer near target, go full thrust
                            }
                        }


                        if (TWR1HeightCtrl) //set thust
                        {
                            if (TWR1HCFullThrustUp) //in full thrust?
                            {

                                TWR1VelocitySetpoint = TWR1HC80Thrust + TWR1VelocityCurrent; //need to add velocity so vertical acceleration happens, limit to 80%  for wiggle room
                            }
                            else
                            {

                                TWR1VelocitySetpoint = TWR1GravForce * .25; //"variable" thrust, slow as we are approaching target height
                            }
                        }

                    }
                    //TWR1HCFreePitch = 0;
                    TWR1HCOrbitDrop = false; //error trap, below target height so we can't be orbit dropping
                }
                else//vessel above target height
                { //vessel above target height, this is second so it is part of the else statement so if the math goes wonky the engine should burn high
                    if (TWR1HCDistToTarget < (TWR1GravForce * 2)) //are we within gravforce as velocity as distance?
                    {

                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = (Math.Min(TWR1GravForce * .3, (TWR1HCDistToTarget * .3)) * -1); //just above target height, set velocity to lesser of 1.5 gravity or 1/3 distance to target
                        }

                        //TWR1HCFreePitch = 0;

                        TWR1HCOrbitDrop = false;
                    }
                    else if (TWR1HCDistToTarget < (TWR1GravForce * 10)) //are we within gravforce as velocity as distance?
                    {

                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = (Math.Min(TWR1GravForce * .5, (TWR1HCDistToTarget * .3)) * -1); //just above target height, set velocity to lesser of half gravity or 1/3 distance to target
                        }

                        //TWR1HCFreePitch = 0;

                        TWR1HCOrbitDrop = false;
                    }

                    else if (TWR1VelocityCurrent > 0) //above target height with positive velocity
                    {

                        if (TWR1HeightCtrl)
                        {
                            TWR1VelocitySetpoint = TWR1HC1Thrust; //cut engine
                        }


                    }
                    else //above target height with negative velocity
                    {
                        TWR1HCtime = Math.Abs(TWR1VelocityCurrent) / TWR1HC80Thrust; //time to target height?
                        TWR1HCneeded = Math.Abs(TWR1VelocityCurrent) / 2f * TWR1HCtime; //altitude needed

                        if (TWR1HCneeded > TWR1HCDistToTarget) //below height needed?
                        {


                            if (TWR1HeightCtrl)
                            {
                                TWR1VelocitySetpoint = Math.Min(TWR1GravForce * .25, TWR1HC80Thrust) * -1; //set to 1/4 gravity or 80% thrust, whichever is lesser
                            }
                            TWR1HCOrbitDrop = false;
                        }
                        else
                        {

                            if (TWR1HeightCtrl)
                            {

                                if (TWR1VesselPitch >= 80) //less then 10 degress off vertical
                                {
                                    TWR1VelocitySetpoint = -100000f;//cut engines so no fuel used
                                }
                                else
                                {
                                    TWR1VelocitySetpoint = TWR1HC1Thrust + TWR1VelocityCurrent; //greater then 10 degrees off vertical, activate engines
                                }
                            }
                        }


                    }

                }
            }


        }
        public class partDist //part's distace to CelestialBody CoM for distance sort
        {
            public Part prt;
            public float dist;
        }

        public double heightToLand() //leave public so other mods can call
        {
            double landHeight = 0;
            bool firstRay = true;



            if (FlightGlobals.ActiveVessel.LandedOrSplashed) //if landed or splashed, height is 0
            {
                landHeight = 0;
            }
            else if (FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude > 2400) //raycast goes wierd outside physics range
            {
                landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
            }
            else //inside physics range, goto raycast
            {
                List<Part> partToRay = new List<Part>(); //list of parts to ray
                if (FlightGlobals.ActiveVessel.Parts.Count < 50) //if less then 50 parts, just raycast all parts
                {
                    partToRay = FlightGlobals.ActiveVessel.Parts;
                }
                else //if over 50 parts, only raycast the 30 parts closest to ground. difference between 30 and 50 parts to make the sort worth the processing cost, no point in running the sort on 31 parts as the sort costs more processor power then 1 raycast
                {
                    List<partDist> partHeights = new List<partDist>(); //only used above 50 parts, links part to distance to ground
                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        partHeights.Add(new partDist() { prt = p, dist = Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position) }); //create list of parts and their distance to ground
                        //print("a: " + Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position));
                    }
                    partHeights.Sort((i, j) => i.dist.CompareTo(j.dist)); //sort parts so parts closest to ground are at top of list
                    for (int i = 0; i < 30; i = i + 1)
                    {
                        partToRay.Add(partHeights[i].prt); //make list of 30 parts closest to ground
                        //print("b: " + i + " " + partHeights[i].prt.name + " " + partHeights[i].dist + " " + Vector3.Distance(FlightGlobals.ActiveVessel.CoM, FlightGlobals.ActiveVessel.mainBody.position));
                    }

                }

                foreach (Part p in partToRay)
                {
                    try
                    {
                        if (p.collider.enabled) //only check part if it has collider enabled
                        {
                            Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); //find collider edge closest to ground
                            RaycastHit pHit;
                            Ray pRayDown = new Ray(partEdge, FlightGlobals.currentMainBody.position);
                            LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
                            if (Physics.Raycast(pRayDown, out pHit, (float)(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.altitude), pRayMask)) //cast ray
                            {

                                if (firstRay) //first ray this update, always set height to this
                                {

                                    landHeight = pHit.distance;

                                    firstRay = false;
                                }
                                else
                                {

                                    landHeight = Math.Min(landHeight, pHit.distance);


                                }
                                //if (pHit.transform.gameObject.layer != 10 && pHit.transform.gameObject.layer != 15)  //Error trap, ray should only hit layers 10 and 15
                                //{
                                //    print(p.name + " " + pHit.transform.gameObject.layer + " " + pHit.collider.name + " " + pHit.distance);
                                //}

                            }
                            else if (!firstRay) //error trap, ray hit nothing
                            {
                                landHeight = FlightGlobals.ActiveVessel.altitude;
                                firstRay = false;
                            }
                        }
                    }
                    catch
                    {
                        landHeight = FlightGlobals.ActiveVessel.altitude;
                        firstRay = false;
                    }

                }
                if (landHeight < 1) //if we are in the air, always display an altitude of at least 1
                {
                    landHeight = 1;
                }
            }

            if (FlightGlobals.ActiveVessel.mainBody.ocean) //if mainbody has ocean we land on water before the seabed
            {
                if (landHeight > FlightGlobals.ActiveVessel.altitude)
                {
                    landHeight = FlightGlobals.ActiveVessel.altitude;
                }
            }

            return landHeight;
        }   
    }
    public class TWR1Data : PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        public int controlDirection = 0; //Serialzed string of actions and action groups
        public override void OnSave(ConfigNode node)
        {
            node.SetValue("controlDirection", controlDirection.ToString());
            //print("SAVE");
        }
        public override void OnLoad(ConfigNode node)
        {
            controlDirection = Convert.ToInt32(node.GetValue("controlDirection"));
            //print("LOAD!"); 
        }
    }
}