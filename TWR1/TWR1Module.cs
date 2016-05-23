using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VerticalVelocity
{
    public class TWR1Data : PartModule
    {

        [KSPField(isPersistant = true, guiActive = false)]
        public bool masterModule; //is this the master module on the vessel 
        [KSPField(isPersistant = true, guiActive = false)]
        public int controlDirection = 0; //control direction 
        [KSPField(isPersistant = true, guiActive = false)]
        public int controlMode = 0; //control mode, 0 off, 1 Velocity, 2 height 
        [KSPField(isPersistant = true, guiActive = false)]
        public float TWR1VelocitySetpoint = 0; //if in velocity mode, save our setpoint
        [KSPField(isPersistant = true, guiActive = false)]
        public float TWR1HCTarget = 0; //if in velocity mode, save our setpoint
        [KSPField(isPersistant = true, guiActive = false)]
        public bool TWR1Engaged = false; //Is TWR1 (Thrust Weight Ratio 1) mod engaged and auto-controlling?
        public double TWR1ThrustUp = 0f; //thrust straight up needed for desired accel
        public double TWR1ThrustUpAngle = 0f; //thrust needed for desired accel, compensation for vessel angel
        public double TWR1DesiredAccel = 0f; //desired acceleration, includes planet grav so this can not be negative
        public double TWR1DesiredAccelThrust = 0f; //thrust needed for desired accel
        //public Vessel TWR1Vessel; //Our active vessel
        public double TWR1Mass = 0f; //Vessel's mass
        public double TWR1MassLast = 0;
        public double TWR1MaxThrust = 0f; //Max thrust contolled by throttle, modified by current vessel angle
        public double TWR1MinThrust = 0f; //Min thrust controlled by throttle, not necessarily zero if solid rocket boosters are firing, modified by current vessel angle
        public double TWR1MaxThrustVertical = 0f; //Max thrust if we are pefectly vertical
        public double TWR1MinThrustVertical = 0f; //Min thrust controlled by throttle, not necessarily zero if solid rocket boosters are firing, if vessel is perfectly vertical
        public ModuleEngines TWR1EngineModule; //part of check for solid rocket booster
        public ModuleEnginesFX TWR1EngineModuleFX;
        public CelestialBody TWR1SOI; //what are we orbiting?
        public double TWR1GravHeight = 0f; //distance from center of body for gravity force
        public double TWR1GravForce = 0f; //acceleration down due to gravity at this moment
        public double TWR1ThrottleRead = 0f; //current throttle at start of update in %
        public double TWR1VesselPitch; //pitch of vessel from horizon
        public Vector3 TWR1Up; //vessel up angle
        public Vector3 TWR1CoM; //vessel Center of mass
        public double TWR1OffsetVert = 0f; //vessel's offset from vertical in degrees
        public double TWR1OffsetVertRadian = 0f; //vessel's offset from vertical in radians
        public double TWR1OffsetVertRatio = 0f; //cosine of vessel's offset from vertical (return as unitless nubmer, so not degress or radians)
       // public ConfigNode TWR1Node; //config node used to load keybind
        //public string TWR1KeyCodeString = "Z"; //set the TWR1, default to Z if it can't load from TWR1.cfg
        //public KeyCode TWR1KeyCode;
        //public double TWR1VelocitySetpoint = 0f; //vessel vertical velocity setpoint
        public float TWR1VelocityCurrent = 0f; //vessel's current vertical velocity
        public float TWR1VelocityDiff = 0f; //velocity difference between setpoint and current
        public bool TWR1HeightCtrl = false; //control to height engaged?
        public double TWR1HC80Thrust; //80% thrust accel
        //public double TWR1HCTarget = 0f; //target height
        public double TWR1HCToGround; //height about ground/building/sea
        public double TWR1HCDistance; //distance from current heigh to target, this is absolute value so always postive
        public double TWR1HC5Thrust; //accel at 5% thrust
        public double TWR1HC1Thrust; //accel at 5% thrust
        public double TWR1HCDistToTarget; //Distance to HC target, always positive
        //public Color TWR1ContentColor; //Default content color
        public string TWR1HCTargetString; //Height Control target height in string format for GUI text entry
        public bool TWR1HCOrbitDrop = false; //Are we orbit dropping?
       // public IButton TWR1Btn; //blizzy's toolbar button
        //public bool TWR1Show = false; //show GUI?
        public double TWR1HCThrustWarningTime = 0; //gametime saved for thrust warning check
        public bool TWR1OrbitDropAllow = false; //are we high enough to offer Orbit Drop as an option?
        public double TWR1OrbitDropHeightNeeded = 0f; //how much height needed for Orbit Drop
        public double TWR1OrbitDropTimeNeeded = 0f; //how much time needed to orbit drop
       // public TextAnchor TWR1DefautTextAlign; //store default text alignment to reset it after GUI frame draws
       // public TextAnchor TWR1DefaultTextFieldAlign; //same^
       // public double TWR1SpeedStep = 1f; //Size of speed change per tap, default to 1m/s
       // public string TWR1SpeedStepString; //speed step as string for GUI text entry
       // public Texture2D TWR1SettingsIcon = new Texture2D(20, 22, TextureFormat.ARGB32, false); //toolbar icon texture
       // public bool TWR1KASDetect = false; //is KAS installed?
       // public double TWR1SpeedStep = 1f; //Size of speed change per tap, default to 1m/s
        //public string TWR1SpeedStepString; //speed step as string for GUI text entry
        //public Texture2D TWR1SettingsIcon = new Texture2D(20, 22, TextureFormat.ARGB32, false); //toolbar icon texture
       // public Rect TWR1SettingsWin = new Rect(500, 500, 200, 145);  //settings window position
        //public bool TWR1SettingsShow = false; //show settings window?
        //public bool TWR1SelectingKey = false; //are we selecting a new key?
        public double TWR1LastVel; //vessel vertical velocity last physics frame
        public double TWR1DesiredAccelThrustLast = 0; //desired thrust last physics frame
        public double TWR1ThrustDiscrepancy; //difference in kN between thrusts last frame
        public double TWR1LastFrameActualThrust; //actual "thrust" last frame, includes both engine and aerodynamic lift
        public Queue<float> accelDiffQueue; //last 5 frames of thrust to average out, it's too bouncy to use just last frame
        public float actualThrustLastFrame = 0; //difference between requested and actual thrust, hello jet engines
        public Vector3 TWR1ControlUp;

        
        public void VertVelOn()
        {
            //Debug.Log("on");
            TWR1Engaged = true;
            TWR1VelocitySetpoint = (float)this.vessel.verticalSpeed;

        }
        
        public void VertVelOff()
        {
            //Debug.Log("off");
            TWR1Engaged = false;
            
        }
        
        public void VertVelZero()
        {
            //Debug.Log("zero");
            TWR1Engaged = true;
            TWR1VelocitySetpoint = 0;
        }
        
        public void VertVelUp()
        {
            //Debug.Log("up");
            TWR1Engaged = true;
            TWR1VelocitySetpoint = TWR1VelocitySetpoint + TWR1.TWR1SpeedStep;
        }
       
        public void VertVelDown()
        {
            //Debug.Log("dn");
            TWR1Engaged = true;
            TWR1VelocitySetpoint = TWR1VelocitySetpoint - TWR1.TWR1SpeedStep;
        }


        //public override void OnSave(ConfigNode node)
        //{
        //    node.SetValue("controlDirection", controlDirection.ToString());
        //}
        //public override void OnLoad(ConfigNode node)
        //{
        //    controlDirection = Convert.ToInt32(node.GetValue("controlDirection"));
        //}
        public override void OnStart(StartState state)
        {
            //Debug.Log("TWR!START");
            if (HighLogic.LoadedSceneIsFlight)
            {
                //TWR1Vessel = this.part.vessel;
                accelDiffQueue = new Queue<float>();
            }
            //this.vessel.OnPostAutopilotUpdate += ctrlState;
        }

        //public void OnDisable()
        //{
        //    //this.vessel.OnPostAutopilotUpdate -= ctrlState;
        //}

        //public void ctrlState(FlightCtrlState fc)
        //{
        //    if(TWR1Engaged && !TWR1HCOrbitDrop && TWR1Vessel != FlightGlobals.ActiveVessel)
        //    {
        //        print("throttl " + TWR1ThrustUp);
        //        fc.mainThrottle = (float)TWR1ThrustUp;
        //        //fc.mainThrottle = .99f;
        //    }
        //}


        public void FixedUpdate() //forum says "all physics calculations should be on FixedUpdate, not Update". not sure a throttle adjustment qualifies as a physics calc, but put it here anyway
        {
            string errLine = "1";
            try
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    errLine = "2";
                    TWR1VelocityCurrent = (float)this.vessel.verticalSpeed; //set our current vertical velocity
                    errLine = "3";
                    if (this.vessel.mainBody.ocean)
                    {
                        errLine = "4";
                        TWR1HCToGround = Math.Min(this.vessel.altitude - this.vessel.pqsAltitude, this.vessel.altitude);
                    }
                    else
                    {
                        errLine = "5";
                        TWR1HCToGround = this.vessel.altitude - this.vessel.pqsAltitude;
                    }
                    errLine = "6";
                    TWR1Mass = this.vessel.GetTotalMass();
                    errLine = "7";
                    TWR1SOI = this.vessel.mainBody;
                    errLine = "8";
                    TWR1CoM = this.vessel.findWorldCenterOfMass(); //add exception here for destoryed parts
                    errLine = "9";
                    TWR1Up = (TWR1CoM - this.vessel.mainBody.position).normalized;
                    errLine = "10";
                    TWR1GravHeight = (float)this.vessel.altitude + (float)TWR1SOI.Radius; //gravity force at this altitude (not in m/s^2)
                    errLine = "11";
                    TWR1GravForce = (float)TWR1SOI.gMagnitudeAtCenter / (float)Math.Pow(TWR1GravHeight, 2); //accel down due to gravity in m/s^2
                    errLine = "12";
                    TWR1MaxThrust = 0f; //maxthrust reset
                    TWR1MinThrust = 0f; //minthrust reset
                    TWR1MaxThrustVertical = 0f;
                    TWR1MinThrustVertical = 0f;
                    actualThrustLastFrame = 0f; //thrust dif reset
                    errLine = "13";
                    foreach (Part part in this.vessel.Parts) //go through each part on vessel
                    {
                        errLine = "14";
                        if (part.Modules.Contains("ModuleEngines") | part.Modules.Contains("ModuleEnginesFX")) //is part an engine?
                        {
                            float DavonThrottleID = 0;
                            if (part.Modules.Contains("DifferentialThrustEngineModule")) //Devon Throttle Control Installed?
                            {
                                foreach (PartModule pm in part.Modules)
                                {

                                    if (pm.moduleName == "DifferentialThrustEngineModule")
                                    {
                                        DavonThrottleID = (float)pm.Fields.GetValue("throttleFloatSelect"); //which throttle is engine assigned to?
                                    }
                                }

                            }
                            errLine = "15";
                            if (DavonThrottleID == 0f)
                            {
                                foreach (PartModule TWR1PartModule in part.Modules) //change from part to partmodules
                                {
                                    errLine = "16";
                                    if (TWR1PartModule.moduleName == "ModuleEngines") //find partmodule engine on th epart
                                    {
                                        errLine = "16a";
                                        TWR1EngineModule = (ModuleEngines)TWR1PartModule; //change from partmodules to moduleengines
                                        // print("xform "+ TWR1EngineModule.thrustTransforms.Count + "||" + Vector3.Angle(TWR1EngineModule.thrustTransforms[0].forward,-TWR1Up));
                                        //print("TWR1 angle off" + Vector3.Angle(TWR1EngineModule.thrustTransforms[0].forward, TWR1Up));

                                        double offsetMultiplier; 
                                            try{
                                                offsetMultiplier = Math.Max(0, Math.Cos(Mathf.Deg2Rad * Vector3.Angle(TWR1EngineModule.thrustTransforms[0].forward, -TWR1Up)));
                                                //Debug.Log("TWR1off:" + offsetMultiplier + ":" + TWR1EngineModule.thrustTransforms[0].forward + ":" + -TWR1Up);
                                            }
                                        catch
                                            {
                                            offsetMultiplier = 1;
                                        }
                                        errLine = "16b";
                                        //how far off vertical is this engine?
                                        //print("of " + offsetMultiplier);
                                        if ((bool)TWR1PartModule.Fields.GetValue("throttleLocked") && TWR1EngineModule.isOperational)//if throttlelocked is true, this is solid rocket booster. then check engine is operational. if the engine is flamedout, disabled via-right click or not yet activated via stage control, isOperational returns false
                                        {
                                            errLine = "16c";
                                            //Debug.Log("locked " + TWR1EngineModule.finalThrust);
                                            TWR1MaxThrust += (double)((TWR1EngineModule.finalThrust) * offsetMultiplier); //add engine thrust to MaxThrust
                                            TWR1MaxThrustVertical += (double)(TWR1EngineModule.finalThrust);
                                            TWR1MinThrust += (double)((TWR1EngineModule.finalThrust) * offsetMultiplier); //add engine thrust to MinThrust since this is an SRB
                                            TWR1MinThrustVertical += (double)(TWR1EngineModule.finalThrust);
                                        }
                                        else if (TWR1EngineModule.isOperational)//we know it is an engine and not a solid rocket booster so:
                                        {
                                            errLine = "16d";
                                            //ModuleEngines engTest = (ModuleEngines)TWR1PartModule;  
                                            //Debug.Log("twr1test " + TWR1EngineModule.thrustPercentage + ":" + TWR1EngineModule.maxFuelFlow * TWR1EngineModule.g * TWR1EngineModule.atmosphereCurve.Evaluate((float)(TWR1EngineModule.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)));
                                            //Debug.Log("twr1test " + TWR1EngineModule.finalThrust / TWR1EngineModule.currentThrottle + ":" + TWR1EngineModule.maxFuelFlow + ":" + TWR1EngineModule.g + ":" + TWR1EngineModule.atmosphereCurve.Evaluate(1f) + ":" + TWR1EngineModule.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres );
                                            TWR1MaxThrust += (double)((TWR1EngineModule.maxFuelFlow * TWR1EngineModule.g * TWR1EngineModule.atmosphereCurve.Evaluate((float)(TWR1EngineModule.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * TWR1EngineModule.thrustPercentage / 100F) * offsetMultiplier); //add engine thrust to MaxThrust
                                            errLine = "16d1";
                                            TWR1MaxThrustVertical += (double)((TWR1EngineModule.maxFuelFlow * TWR1EngineModule.g * TWR1EngineModule.atmosphereCurve.Evaluate((float)(TWR1EngineModule.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * TWR1EngineModule.thrustPercentage / 100F));
                                            errLine = "16d2";
                                            //TWR1MinThrust += (double)((TWR1EngineModule.minThrust * TWR1EngineModule.thrustPercentage / 100F) * offsetMultiplier); //add engine thrust to MinThrust, stock engines all have min thrust of zero, but mods may not be 0
                                            errLine = "16d3";
                                            //TWR1MinThrustVertical += (double)((TWR1EngineModule.minThrust * TWR1EngineModule.thrustPercentage / 100F));
                                            errLine = "16d4";
                                        }
                                        errLine = "16e";
                                        actualThrustLastFrame += (float)TWR1EngineModule.finalThrust * (float)offsetMultiplier;
                                    }
                                    else if (TWR1PartModule.moduleName == "ModuleEnginesFX") //find partmodule engine on th epart
                                    {
                                         errLine = "17";
                                        TWR1EngineModuleFX = (ModuleEnginesFX)TWR1PartModule; //change from partmodules to moduleengines
                                        errLine = "17a";
                                        double offsetMultiplier;
                                        try
                                        {
                                            errLine = "17b";
                                            //Debug.Log("thturs " + TWR1EngineModuleFX.thrustTransforms.Count);
                                            
                                            offsetMultiplier = Math.Cos(Mathf.Deg2Rad * Vector3.Angle(TWR1EngineModuleFX.thrustTransforms[0].forward, -TWR1ControlUp)); //how far off vertical is this engine?
                                        }
                                        catch
                                        {
                                            offsetMultiplier = 1;
                                        }
                                        errLine = "17c";
                                            if ((bool)TWR1PartModule.Fields.GetValue("throttleLocked") && TWR1EngineModuleFX.isOperational)//if throttlelocked is true, this is solid rocket booster. then check engine is operational. if the engine is flamedout, disabled via-right click or not yet activated via stage control, isOperational returns false
                                        {
                                            errLine = "17d";
                                            TWR1MaxThrust += (double)((TWR1EngineModuleFX.finalThrust) * offsetMultiplier); //add engine thrust to MaxThrust
                                            TWR1MaxThrustVertical += (double)((TWR1EngineModuleFX.finalThrust));
                                            TWR1MinThrust += (double)((TWR1EngineModuleFX.finalThrust) * offsetMultiplier); //add engine thrust to MinThrust since this is an SRB
                                            TWR1MinThrustVertical += (double)((TWR1EngineModuleFX.finalThrust));
                                        }
                                        else if (TWR1EngineModuleFX.isOperational)//we know it is an engine and not a solid rocket booster so:
                                        {
                                            errLine = "17e";
                                            TWR1MaxThrust += (double)((TWR1EngineModuleFX.maxFuelFlow * TWR1EngineModuleFX.g * TWR1EngineModuleFX.atmosphereCurve.Evaluate((float)(TWR1EngineModuleFX.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * TWR1EngineModuleFX.thrustPercentage / 100F) * offsetMultiplier); //add engine thrust to MaxThrust
                                            errLine = "17e1";
                                            TWR1MaxThrustVertical += (double)((TWR1EngineModuleFX.maxFuelFlow * TWR1EngineModuleFX.g * TWR1EngineModuleFX.atmosphereCurve.Evaluate((float)(TWR1EngineModuleFX.vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * TWR1EngineModuleFX.thrustPercentage / 100F));
                                            errLine = "17e2";
                                            //TWR1MinThrust += (double)((TWR1EngineModuleFX.minThrust * TWR1EngineModuleFX.thrustPercentage / 100F) * offsetMultiplier); //add engine thrust to MinThrust, stock engines all have min thrust of zero, but mods may not be 0
                                            errLine = "17e3";
                                            //TWR1MinThrustVertical += (double)((TWR1EngineModuleFX.minThrust * TWR1EngineModuleFX.thrustPercentage / 100F));
                                            errLine = "17e4";
                                        }
                                            errLine = "17f";
                                        actualThrustLastFrame += (float)TWR1EngineModuleFX.finalThrust * (float)offsetMultiplier;
                                    }

                                }
                            }
                        }

                    }
                    errLine = "18";
                    TWR1HC5Thrust = (Math.Max((TWR1MaxThrustVertical * .05), TWR1MinThrustVertical) / TWR1Mass) - TWR1GravForce; //accel at 5% thrust, makes sure engine is on to allow for ship horizontal speed adjustment. this outside HC method for UI dispaly
                    TWR1HC1Thrust = (Math.Max((TWR1MaxThrustVertical * .01), TWR1MinThrustVertical) / TWR1Mass) - TWR1GravForce;
                    errLine = "19";
                    //Debug.Log("startofit " + TWR1GravForce + "||" + TWR1Mass + "||" + TWR1MaxThrust);
                    TWR1HC80Thrust = ((TWR1MaxThrustVertical * .8f) / TWR1Mass) - TWR1GravForce; //use 80% acceleration to account for being off vertical, planet grav reduces accel in this case this outside HC method for UI disaply
                    //Debug.Log(TWR1MaxThrust + " " + TWR1MinThrust + " " + TWR1HC80Thrust);
                    errLine = "20";
                    if (!TWR1Engaged) { TWR1HeightCtrl = false; } //mod has been disengegaed, disengage height control
                    errLine = "21";
                    if (!TWR1HeightCtrl)
                    {
                        TWR1HCOrbitDrop = false; //Height control not engaged, we can not be doing an OrbitDrop
                    }
                    errLine = "22";
                    TWR1ControlUp = SetDirection(controlDirection, this.vessel);
                    errLine = "23";
                    TWR1OrbitDropTimeNeeded = Math.Abs(Math.Min(0, TWR1VelocityCurrent)) / Math.Abs(TWR1HC80Thrust); //how much time is needed to orbit drop? if we are positive velocity, need zero time
                    errLine = "24";
                    //altitude needed to orbit drop, is time to stop our current velocity of zero or lower (use zero if moving upwards) plus 20 seconds of falling due to gravity
                    //TWR1OrbitDropHeightNeeded = (Math.Abs(TWR1VelocityCurrent) * 40) + (TWR1HC80Thrust * Math.Pow(TWR1OrbitDropTimeNeeded, 2)) / 2; //how much altitude is needed to orbit drop?
                    TWR1OrbitDropHeightNeeded = (Math.Pow(((Math.Abs(Math.Min(TWR1VelocityCurrent, 0))) + TWR1GravForce * 20), 2) / (2 * TWR1HC80Thrust)) + (TWR1GravForce * 200) + (Math.Abs(Math.Min(TWR1VelocityCurrent,0)) * 20) ; //twr1gravforce * 200 is shortcut for D = (accel * time^2) /2 
                    errLine = "25";
                    //Debug.Log("heightneed " + TWR1OrbitDropHeightNeeded + "||" + TWR1VelocityCurrent + "||" + TWR1GravForce + "||" + TWR1HC80Thrust);
                    TWR1HCDistToTarget = Math.Abs(TWR1HCToGround - TWR1HCTarget);
                    errLine = "26";
                    TWR1ThrottleRead = this.vessel.ctrlState.mainThrottle;

                    errLine = "27";

                    TWR1OffsetVert = Vector3.Angle(TWR1Up, TWR1ControlUp);
                    //Debug.Log("tip " + TWR1OffsetVert);
                    
                    TWR1VesselPitch = Math.Max((90 - TWR1OffsetVert), 0);
                    errLine = "28";
                    TWR1OffsetVertRadian = Mathf.Deg2Rad * TWR1OffsetVert; //mathf.cos takes radians, not degrees, ask unity why
                    errLine = "29";
                    TWR1OffsetVertRatio = Math.Cos(TWR1OffsetVertRadian); //our compensation factor for being offset from vertical
                    errLine = "30";
                    TWR1HCDistance = Math.Abs(TWR1HCToGround - TWR1HCTarget); //absolute distance to target altitude
                    errLine = "31";
                    if (TWR1Engaged)
                    {
                        errLine = "32";
                        TWR1Math();
                    }
                    errLine = "33";
                }
            }

            catch (Exception e)
            {
                Debug.Log("TWR1 Fixed Fail: " + errLine + " " + e);
            }
        }

        public void TWR1Math()
        {
            string errLine = "1";
            try
            {
                
                 
               
                //TWR1ThrottleRead = FlightInputHandler.state.mainThrottle; //readback current throttle
                
                errLine = "2";
                
                //print("mass " + TWR1MaxThrust + "||" + TWR1MinThrust + "||" + actualThrustLastFrame);
                errLine = "6";
                if (TWR1MaxThrust < 1) //if MaxThrust is zero, a divide by zero error gets thrown later, so...
                {
                    TWR1MaxThrust = 1; //set MaxThrust to at least 1 to avoid this
                }
                errLine = "7";
                //TWR1VelocityCurrent = (float)this.vessel.verticalSpeed; //set our current vertical velocity
                //if (this.vessel.mainBody.ocean)
                //{
                //    TWR1HCToGround = Math.Min(this.vessel.altitude - this.vessel.pqsAltitude, this.vessel.altitude);
                //}
                //else
                //{
                //    TWR1HCToGround = this.vessel.altitude - this.vessel.pqsAltitude;
                //}
                errLine = "8";
                //TWR1HC5Thrust = (Math.Max((TWR1MaxThrust * .05), TWR1MinThrust) / TWR1Mass) - TWR1GravForce; //accel at 5% thrust, makes sure engine is on to allow for ship horizontal speed adjustment. this outside HC method for UI dispaly
                //TWR1HC1Thrust = (Math.Max((TWR1MaxThrust * .01), TWR1MinThrust) / TWR1Mass) - TWR1GravForce;
                //TWR1HC80Thrust = ((TWR1MaxThrust * .8f) / TWR1Mass) - TWR1GravForce; //use 80% acceleration to account for being off vertical, planet grav reduces accel in this case this outside HC method for UI disaply
               // Debug.Log("aaaa " + TWR1VelocitySetpoint); 
                if (TWR1HeightCtrl)
                {
                    TWR1HeightControl(); //Height control now sets VelocitySetpoint (version 1.5)
                }
                //Debug.Log("bbbb" + TWR1VelocitySetpoint);
                //print("Velocity setpoint " + TWR1VelocitySetpoint + " " +TWR1HC80Thrust + " " +TWR1GravForce);
                errLine = "9";

                if (TWR1HC80Thrust <= 0 && TWR1HeightCtrl == true || TWR1HC1Thrust >= 0 && TWR1HeightCtrl == true) //is height control on and 1% or 80% no longer valid?
                {
                    TWR1HeightCtrl = false; //cancel height control
                    TWR1DesiredAccel = 0f - TWR1VelocityCurrent + TWR1GravForce; //we just canceled height control, try to zero velocity
                    TWR1VelocitySetpoint = 0f; //set Velocity Setpoint to zero for normal logic next pass
                }

                errLine = "10";
                
                TWR1VelocityDiff = TWR1VelocitySetpoint - TWR1VelocityCurrent; //find our velocity difference, order is important so that negative velocity is in the correct direction
                TWR1DesiredAccel = TWR1VelocityDiff +TWR1GravForce; //find desired vertical accel, including planets grav. Because velocity is instant, this works to a close enough accuracy for our purposes without getting into PID control or something similar. Include fudge factor


                TWR1DesiredAccelThrust = TWR1DesiredAccel * TWR1Mass; //desired thrust upwards, in kilonewtons
                //print("hrust " + TWR1DesiredAccelThrust);
                

                //if (FlightGlobals.getStaticPressure() > 0.0001 && !TWR1HeightCtrl) //aerodynamic lift compensation calculation, replaiced with general compensation calculation below
                //{

                //    //average out the last physics frame thrusts for aerodynamic lift calculation
                //    while (TWR1ThrustQueue.Count > 5) //only use last 5 frames so make sure the Queue only has 5 values
                //    {
                //        TWR1ThrustQueue.Dequeue();
                //    }
                //    TWR1DesiredAccelThrustLast = 0; //reset thrust average
                //    foreach (double dbl in TWR1ThrustQueue)
                //    {
                //        TWR1DesiredAccelThrustLast = TWR1DesiredAccelThrustLast + dbl; //add last 5 frames thrust together

                //    }
                //    TWR1DesiredAccelThrustLast = TWR1DesiredAccelThrustLast / TWR1ThrustQueue.Count; //divide by count to get average of desired thrusts over last 5 frames

                //    //TWR1LastFrameAccel = ((this.vessel.verticalSpeed - TWR1LastVel) * TWR1FixedUpdatePerSec);
                //    TWR1LastFrameActualThrust = ((((this.vessel.verticalSpeed - TWR1LastVel) / Time.fixedDeltaTime) + TWR1GravForce) * TWR1Mass); //get actual thrust of last physics frame, note GravForce has to be present as you are always fighting gravity

                //    TWR1ThrustDiscrepancy = TWR1DesiredAccelThrustLast - TWR1LastFrameActualThrust - ThrustUnderRun; //discrepancy between thrusts the last frame,

                //    if (Math.Abs(TWR1ThrustDiscrepancy) < TWR1MaxThrust * .2) //only compensate for aerolift if the value is less the 20% of max thrust. If it's more, almost certain that the thrust discrepancy is caused by other factors
                //    {
                //        TWR1DesiredAccelThrust = TWR1DesiredAccelThrust + TWR1ThrustDiscrepancy;
                //    }
                //}
                errLine = "11";
                float accelLastFrameThrust = (((TWR1VelocityCurrent - (float)TWR1LastVel)/TimeWarp.fixedDeltaTime)+(float)TWR1GravForce) * (float)TWR1MassLast; //what was our observed accel last frame?
                float accelDiff = actualThrustLastFrame - accelLastFrameThrust; //difference between actual accel and observed accel last frame, use as compensation this frame
                errLine = "11a";
               // Debug.Log(" diffa " + accelDiff + "||" + accelLastFrameThrust + "||" + accelLastFrameThrust + "||" + TWR1DesiredAccelThrust);
                while(accelDiffQueue.Count > 4) //except we need to averge this, otherwise tehre is too much bounce
                {
                    accelDiffQueue.Dequeue();
                }
                errLine = "11b";
                if (!double.IsNaN(accelDiff))
                {
                    accelDiffQueue.Enqueue(accelDiff);
                }
                else
                {
                    accelDiffQueue.Enqueue(0);
                }
                float accelDiffAverage = 0;
                errLine = "11c";
                foreach(float aD in accelDiffQueue)
                {
                    accelDiffAverage = accelDiffAverage + aD;
                }
                errLine = "11d";
                accelDiffAverage = accelDiffAverage / accelDiffQueue.Count; //our averaged accel diff
               
               
                TWR1DesiredAccelThrust = TWR1DesiredAccelThrust + accelDiffAverage; //compensate for lift/extra attachements with accel diff
                //Debug.Log(" diff " + accelDiff + "||" + accelLastFrameThrust + "||" + accelDiffAverage + "|" + TWR1DesiredAccelThrust);
                //TWR1ThrustUpAngle = TWR1DesiredAccelThrust / TWR1OffsetVertRatio; //compensate for vessel angle off vertical
                //Debug.Log("t1 " + TWR1MaxThrust +"||" + TWR1MinThrust + "||" + TWR1DesiredAccelThrust);
                TWR1ThrustUp = Math.Max(Math.Min((TWR1DesiredAccelThrust - TWR1MinThrust) / (TWR1MaxThrust - TWR1MinThrust), 1), 0); //find percent of current throttle, minimum 0 for no thrust, 1 for max thrust
               // Debug.Log("t2 " + TWR1ThrustUp);
                errLine = "12";
                if (TWR1ThrustUp > TWR1ThrottleRead) //throttle damper to limit vessel jolts, going from 0 to 100% thrust from one physics frame to the next can shake a ship apart.
                {
                    TWR1ThrustUp = Math.Min(TWR1ThrottleRead + 0.03, TWR1ThrustUp);
                }
                else
                {
                    TWR1ThrustUp = Math.Max(TWR1ThrottleRead - 0.03, TWR1ThrustUp);
                }
                TWR1ThrustUp = Math.Max(Math.Min(TWR1ThrustUp, 1), 0); //error catch throttle value, if an invalid value is passed to KSP it screws up.
                errLine = "13";

                
                //if (TWR1HCToGround > TWR1HCTarget && TWR1HCDistToTarget > TWR1GravForce*100 && TWR1VelocityCurrent > ((Math.Sqrt(((TWR1HCDistToTarget + (TWR1VelocityCurrent * 20) - (TWR1GravForce * 10)) - (TWR1GravForce * 5)) * Math.Abs(TWR1HC80Thrust))) * -1.4) - (TWR1GravForce * 20))
                if (TWR1HCToGround > TWR1HCTarget && TWR1HCDistToTarget > TWR1OrbitDropHeightNeeded)
                {

                    TWR1OrbitDropAllow = true;
                }
                else
                {
                    TWR1OrbitDropAllow = false;
                }
                errLine = "14";
                //print("Drop " + TWR1HCToGround + " " + TWR1HCTarget + " " + TWR1VelocityCurrent + " " + (Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 5)) * Math.Abs(TWR1HC80Thrust))) * -1.4);
                if (TWR1Engaged == true) //control throttle? //replaced with ctrlState callback
                {


                    if (TWR1HCOrbitDrop == false) //if falling from orbit, do not lock out throttle control
                    {
                        
                            if (this.vessel == FlightGlobals.ActiveVessel)
                            {
                                if (TWR1VesselPitch < 100)
                                {
                                FlightInputHandler.state.mainThrottle = (float)TWR1ThrustUp; //set throttle to desired thrust
                                }
                                else
                                {
                                    FlightInputHandler.state.mainThrottle =0f;
                                }
                                //Debug.Log("throttles' been set " + TWR1ThrustUp + "||" + FlightInputHandler.state.mainThrottle);
                            }
                            else
                            {

                                //print("set throttle");
                                if (TWR1VesselPitch < 100)
                                {
                                this.vessel.ctrlState.mainThrottle = (float)TWR1ThrustUp;
                                }
                                else
                                {
                                    this.vessel.ctrlState.mainThrottle = 0f;
                                }
                            }
                        
                        

                    }
                }
                //print("Chcekc " + this.vessel.vesselName + "||" + TWR1Mass + "||" +TWR1ThrustUp);
                errLine = "15";
                //TWR1ThrustQueue.Enqueue(TWR1DesiredAccelThrust - ThrustUnderRun); //add desired thrust value to queue
                //VesselDead: //escape for no active vessel
                TWR1LastVel = this.vessel.verticalSpeed; //save velocity from this frame for calculations next frame
                TWR1MassLast = TWR1Mass;
            }
            catch(Exception e)
            {
                Debug.Log("TWR1Math Fail " + errLine + " " + e);
            }
        }


        public Vector3 SetDirection(int ctrlDir,Vessel TWR1Vessel)
        {
            if (ctrlDir == 0)
            {
                return (this.vessel.rootPart.transform.up);
            }
            if (ctrlDir == 1)
            {
                return (this.vessel.rootPart.transform.forward);
            }
            if (ctrlDir == 2)
            {
                return (-this.vessel.rootPart.transform.up);
            }
            if (ctrlDir == 3)
            {
                return (-this.vessel.rootPart.transform.forward);
            }
            if (ctrlDir == 4)
            {
                return (this.vessel.rootPart.transform.right);
            }
            if (ctrlDir == 5)
            {
                return (-this.vessel.rootPart.transform.right);
            }
            else
            {
                return (this.vessel.rootPart.transform.up);
            }
        }

        public void TWR1HeightControl() //DesiredAccel must account for gravity in this method
        {
            
            if (TWR1HCOrbitDrop) //are we orbit dropping?
            {

                if (TWR1HCThrustWarningTime != 0) //are we in thrust warning?
                {

                    if (this.vessel.missionTime - TWR1HCThrustWarningTime > 15) //check to see if it is time to exit thrust warning mode
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

                        //if (TWR1OrbitDropHeightNeeded > TWR1HCDistToTarget) //is our current altitude below the altitude needed for height control?
                        //if(((Math.Sqrt(((TWR1HCDistToTarget + (TWR1VelocityCurrent * 20) - (TWR1GravForce * 10)) - (TWR1GravForce * 5)) * Math.Abs(TWR1HC80Thrust))) * -1.4) - (TWR1GravForce * 15) > TWR1VelocityCurrent)
                        if (TWR1OrbitDropHeightNeeded > TWR1HCDistToTarget)
                        {
                            TWR1HCThrustWarningTime = this.vessel.missionTime; //enter thrust warning mode

                            //Debug.Log("war " + TWR1HCThrustWarningTime + "|" + TWR1OrbitDropHeightNeeded + "||" + TWR1HCDistToTarget);
                        }
                    }
                }
            }
            else //we are not orbit dropping
            {
                //Debug.Log("1");
                if (TWR1HCToGround <= TWR1HCTarget) //vessel below target height
                {
                    //Debug.Log("2");
                    if (TWR1HCDistToTarget <= TWR1GravForce * 6)
                    {
                        //Debug.Log("3" + TWR1HCDistToTarget+"||"+TWR1HC80Thrust+"||"+TWR1GravForce);
                        //TWR1VelocitySetpoint = (float)Math.Min(TWR1GravForce, (TWR1HCDistToTarget)*.7);
                        //TWR1VelocitySetpoint = (float)(TWR1HCDistToTarget * (TWR1HC80Thrust / TWR1GravForce))*.1f;
                        TWR1VelocitySetpoint = (float)Math.Min(TWR1GravForce * .8, (TWR1HCDistToTarget) * .6);
                    }

                    else
                    {
                        //Debug.Log("4");
                        //Debug.Log("checking " + TWR1HCDistToTarget + "||" + TWR1GravForce);
                        //TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 2)) * Math.Abs(TWR1GravForce))) * 1.2);
                        TWR1VelocitySetpoint = (float)Math.Sqrt(2 * (TWR1GravForce * .8) * (TWR1HCDistToTarget - (TWR1GravForce * 4)));
                        //Debug.Log("chekc " + TWR1VelocitySetpoint);
                    }
                    
                    TWR1HCOrbitDrop = false; //error trap, below target height so we can't be orbit dropping
                }
                else//vessel above target height
                { //vessel above target height, this is second so it is part of the else statement so if the math goes wonky the engine should burn high
                    //Debug.Log("5");
                    if(TWR1HCDistToTarget <= TWR1GravForce * 8)
                    {
                        //Debug.Log("6");
                        //TWR1VelocitySetpoint = (float)(TWR1HCDistToTarget * (TWR1HC80Thrust / TWR1GravForce)) *-.1f;
                        //TWR1VelocitySetpoint = (float)Math.Min(TWR1GravForce, (TWR1HCDistToTarget) * .7);
                        TWR1VelocitySetpoint = (float)(Math.Min(TWR1HCDistToTarget * .3,TWR1GravForce*.5) * -1);
                    }
                    //else if (TWR1HCDistToTarget < TWR1GravForce * 100)
                    //{
                    //    TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 3.9)) * Math.Abs(TWR1HC80Thrust))) * -.6);
                    //}
                    else
                    {
                        //TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 5)) * Math.Abs(TWR1HC80Thrust))) * -1.4);
                        TWR1VelocitySetpoint = (float)(Math.Sqrt(2 * (TWR1HC80Thrust * .8) * (TWR1HCDistToTarget - (TWR1GravForce * 6))) * -1);
                    }
                    
                    
                    //TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget * Math.Abs(TWR1HC80Thrust) * 2)))* -1f);
                    //Debug.Log("vel set " + TWR1VelocitySetpoint);
                    //////if (TWR1HCDistToTarget < TWR1GravForce * 4)
                    //////{
                    //////    TWR1VelocitySetpoint = (float)(TWR1HCDistToTarget * .3 * -1);
                    //////}

                    //////else if (TWR1HCDistToTarget < TWR1GravForce * 100)
                    //////{
                    //////    TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 3.9)) * Math.Abs(TWR1HC80Thrust))) * -.6);
                    //////}

                    //////else
                    //////{
                    //////    TWR1VelocitySetpoint = (float)((Math.Sqrt((TWR1HCDistToTarget - (TWR1GravForce * 5)) * Math.Abs(TWR1HC80Thrust))) * -1.4);
                    //////}

                }
            }


        } 
        [KSPAction("VertVel:U")]
        public void VertVelDirUp(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 0;
            TWR1.thisModule.ShowLine();
        }
        [KSPAction("VertVel:D")]
        public void VertVelDirDown(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 2;
            TWR1.thisModule.ShowLine();
        }
        [KSPAction("VertVel:L")]
        public void VertVelDirLeft(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 5;
            TWR1.thisModule.ShowLine();
        }
        [KSPAction("VertVel:R")]
        public void VertVelDirRight(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 4;
            TWR1.thisModule.ShowLine();
        }
        [KSPAction("VertVel:F")]
        public void VertVelDirForward(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 1;
            TWR1.thisModule.ShowLine();
        }
        [KSPAction("VertVel:B")]
        public void VertVelDirBack(KSPActionParam param)
        {
            TWR1.thisModule.curVsl.controlDirection = 3;
            TWR1.thisModule.ShowLine();
        }
    }

}



