using System;
using UnityEngine;
using KSP.IO;

namespace DockingPortAlignment
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DockingPortAlignment : MonoBehaviour
    {
        private static PluginConfiguration config;
        private static bool hasInitializedStyles = false;
        private static GUIStyle windowStyle, labelStyle, gaugeStyle, settingsButtonStyel, rightAlignedStyle, settingsAreaStyle;
        private static Rect windowPosition = new Rect();
        private static Rect lastPosition = new Rect();
        private static Rect debugWindowPosition = new Rect(50,200,350,0);

        private static float scale = .65f;
        private static int gaugeWidth = 400;
        private static int gaugeHeight = 407;
        private static Rect gaugeRect = new Rect(0, 0f, gaugeWidth * scale, gaugeHeight * scale);
        private static float visiblePortion = .76f;
        private static int settingsWindowHeight = 53;

        private static LineRenderer ownshipLine = null;
        private static LineRenderer targetLine = null;

        private static Vector3 orientationDeviation = new Vector3();
        private static Vector2 translationDeviation = new Vector3();
        private static Vector2 transverseVelocity = new Vector2();
        private static float negativeOnBackHemisphere;
        private static float closureV;
        private static float distanceToTarget;
        
        private static Color colorCDINormal = new Color(.064f, .642f, 0f);
        private static Color colorCDIReverse = new Color(.747f, 0f, .05f);
        private static Color color_settingsButtonActivated = new Color(.11f, .66f, .11f, 1f);
        private static Color color_settingsButtonDeactivated = new Color(.22f, .26f, .29f, 1f);
        private static Color color_settingsWindow = new Color(.19f, .21f, .24f);

        public static Texture2D frontGlass = new Texture2D(gaugeWidth, gaugeHeight);
        public static Texture2D background = new Texture2D(gaugeWidth, gaugeHeight);
        public static Texture2D markerTexture = new Texture2D(207, 207);
        public static Texture2D arrowTexture = new Texture2D(21, 87);
        public static Texture2D prograde = new Texture2D(96, 96);
        public static Texture2D retrograde = new Texture2D(96, 96);
        public static Texture2D roll = new Texture2D(51, 33);
        public static Texture2D roll_label = new Texture2D(17, 17);

        // TODO resize the source textures instead of down-scaling
        private static float velocityVectorIconSize = 42f;
        private static float markerSize = 140f;

        private static bool showSettings = false;
        private static bool useCDI = true;
        private static bool drawRollDigits = false;

        private static float transverseVelocityRange = 3.5f;
        private static float velocityVectorExponent = .75f;

        private static float alignmentGaugeRange = 60f;
        private static float alignmentExponent = .8f;

        private static float CDIExponent = .75f;
        private static float CDIexponentDecreaseBeginRange = 15f;
        private static float CDIexponentDecreaseDoneRange = 5f;
        
        private static bool shouldDebug = true;

        public void Awake()
        {
            if (!hasInitializedStyles) initStyles();

            RenderingManager.AddToPostDrawQueue(0, OnDraw);

            if (shouldDebug) RenderingManager.AddToPostDrawQueue(1, OnDrawDebug);

            loadTextures();

            print("Loaded Docking Port Alignment Gauge!");
        }

        private static void loadTextures()
        {
            Byte[] arrBytes;
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("background.png", null);
            background.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("frontglass.png", null);
            frontGlass.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("marker.png", null);
            markerTexture.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("arrow.png", null);
            arrowTexture.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("prograde.png", null);
            prograde.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("retrograde.png", null);
            retrograde.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("roll.png", null);
            roll.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<DockingPortAlignment>("roll_label.png", null);
            roll_label.LoadImage(arrBytes);
        }

        public void Start()
        {
            LoadPrefs();
            if (shouldDebug) setupDebugLines();
        }

        private void setupDebugLines()
        {
            GameObject onwshipObj = new GameObject("ownshipLine");
            GameObject targetObj = new GameObject("targetLine");

            ownshipLine = onwshipObj.AddComponent<LineRenderer>();
            ownshipLine.useWorldSpace = false;

            targetLine = targetObj.AddComponent<LineRenderer>();
            targetLine.useWorldSpace = false;
            
            ownshipLine.transform.localPosition = Vector3.zero;
            ownshipLine.transform.localEulerAngles = Vector3.zero;

            targetLine.transform.localPosition = Vector3.zero;
            targetLine.transform.localEulerAngles = Vector3.zero;

            ownshipLine.material = new Material(Shader.Find("Particles/Additive"));
            ownshipLine.SetColors(Color.green, Color.blue);
            ownshipLine.SetWidth(.1f, .1f);
            ownshipLine.SetVertexCount(2);
            ownshipLine.SetPosition(0, Vector3.zero);
            ownshipLine.SetPosition(1, Vector3.up * 8);

            targetLine.material = new Material(Shader.Find("Particles/Additive"));
            targetLine.SetColors(Color.red, Color.yellow);
            targetLine.SetWidth(.1f, .1f);
            targetLine.SetVertexCount(2);
            targetLine.SetPosition(0, Vector3.zero);
            targetLine.SetPosition(1, Vector3.up * 120); 
        }

        private void OnDraw()
        {
            if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)
            {
                calculateGaugeData();

                if (showSettings)
                {
                    windowPosition.width = gaugeWidth * scale;
                    windowPosition.height = (gaugeHeight * scale) + settingsWindowHeight;
                }
                else
                {
                    windowPosition.width = gaugeWidth * scale;
                    windowPosition.height = gaugeHeight * scale;
                }
                windowPosition = GUI.Window(1337, windowPosition, OnWindow, "Enchanced Nav Ball", labelStyle);
            }
        }

        private void calculateGaugeData()
        {
            Transform selfTransform = FlightGlobals.ActiveVessel.ReferenceTransform;
            ModuleDockingNode targetPort = FlightGlobals.fetch.VesselTarget as ModuleDockingNode;
            Transform targetTransform = targetPort.transform;
            Vector3 targetPortOutVector = targetTransform.up.normalized;

            orientationDeviation.x = AngleAroundNormal(-targetPortOutVector, selfTransform.up, selfTransform.forward);
            orientationDeviation.y = AngleAroundNormal(-targetPortOutVector, selfTransform.up, -selfTransform.right);
            orientationDeviation.z = AngleAroundNormal(targetTransform.forward, selfTransform.forward, selfTransform.up);
             
            Vector3 targetToOwnship = selfTransform.position - targetTransform.position;

            translationDeviation.x = AngleAroundNormal(targetToOwnship, targetPortOutVector, selfTransform.forward);
            translationDeviation.y = AngleAroundNormal(targetToOwnship, targetPortOutVector, -selfTransform.right);

            if (Math.Abs(translationDeviation.x) >= 90)
            {
                negativeOnBackHemisphere = -1;
            }
            else
            {
                negativeOnBackHemisphere = 1;
            }

            float normalVelocity = Vector3.Dot(FlightGlobals.ship_tgtVelocity, targetPortOutVector);
            closureV = -normalVelocity*negativeOnBackHemisphere;

            //Old behavior where velocity vector incorporated forward velocity
            //relativeVelocity.x = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, targetPortOutVector, selfTransform.forward);
            //relativeVelocity.y = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, targetPortOutVector, -selfTransform.right);

            Vector3 globalTransverseVelocity = FlightGlobals.ship_tgtVelocity - normalVelocity * targetPortOutVector;
            transverseVelocity.x = Vector3.Dot(globalTransverseVelocity, selfTransform.right);
            transverseVelocity.y = Vector3.Dot(globalTransverseVelocity, selfTransform.forward);            

            //Prograde/Retrograde Vector
            //Vector3 localVelocity = selfTransform.InverseTransformDirection(FlightGlobals.ship_tgtVelocity);
            //relativeVelocity.x = (float)Math.Atan2(localVelocity.x, localVelocity.y);
            //relativeVelocity.y = (float)Math.Atan2(localVelocity.z, localVelocity.y);
            //relativeVelocity *= (float)(2f / Math.PI);

            distanceToTarget = Vector3.Distance(targetTransform.position, selfTransform.position);
        }

        private void OnWindow(int windowID)
        {
            //For variable scale window (TODO)
            gaugeRect.width = gaugeWidth * scale;
            gaugeRect.height = gaugeHeight * scale;
            Vector2 gaugeCenter = new Vector2(gaugeRect.width / 2f, gaugeRect.height / 2f);

            GUI.DrawTexture(gaugeRect, background);

            if (useCDI)
            {
                drawCDI(gaugeRect);
            }

            Matrix4x4 matrixBackup = GUI.matrix;
            if (Math.Abs(orientationDeviation.x) > alignmentGaugeRange || Math.Abs(orientationDeviation.y) > alignmentGaugeRange)
            {   
                Vector2 normDir = new Vector2(orientationDeviation.x, orientationDeviation.y).normalized;
                float angle = (float)Math.Atan2(normDir.x, -normDir.y) * UnityEngine.Mathf.Rad2Deg;

                float arrowLength = visiblePortion * gaugeCenter.y;
                float arrowWidth = arrowLength * arrowTexture.width / arrowTexture.height;
               
                Rect arrowRect = new Rect(0.5f * (gaugeRect.width - arrowWidth), gaugeCenter.y - arrowLength, arrowWidth, arrowLength);

                GUIUtility.RotateAroundPivot(angle, gaugeCenter);

                GUI.DrawTexture(arrowRect, arrowTexture);
                GUI.matrix = matrixBackup;
            }
            else
            {
                float displayX = scaleExponentially(orientationDeviation.x / alignmentGaugeRange, alignmentExponent);
                float displayY = scaleExponentially(orientationDeviation.y / alignmentGaugeRange, alignmentExponent);

                float scaledMarkerSize = markerSize * scale;

                Rect markerRect = new Rect(gaugeCenter.x * (1 + displayX * visiblePortion),
                                        gaugeCenter.y * (1 + displayY * visiblePortion),
                                        scaledMarkerSize,
                                        scaledMarkerSize);

                GUI.DrawTexture(new Rect(markerRect.x - .5f*markerRect.width, markerRect.y - .5f*markerRect.height, markerRect.width, markerRect.height), markerTexture);
                    
                GUIUtility.RotateAroundPivot(orientationDeviation.z, gaugeCenter);

                float scaledRollWidth = roll.width * scale;
                float scaledRollHeight = roll.height * scale;


                GUI.DrawTexture(new Rect(gaugeCenter.x - .5f * scaledRollWidth, (roll.height + 20) * scale, scaledRollWidth, scaledRollHeight), roll);
            }

            GUI.matrix = matrixBackup;

            if (useCDI)
            {
                drawVelocityVector(gaugeRect);
            }

            drawGaugeText(gaugeRect);

            GUI.DrawTexture(gaugeRect, frontGlass);

            Color lastBackColor = GUI.backgroundColor;
            if (showSettings)
            {
                GUI.backgroundColor = color_settingsButtonActivated;
            }
            else
            {
                GUI.backgroundColor = color_settingsButtonDeactivated;
            }

            bool settingsButtonClicked = GUI.Button(new Rect(gaugeCenter.x - 52 * scale, gaugeRect.height - 18 * scale, 104 * scale, 15 * scale), "Settings", settingsButtonStyel);

            if (settingsButtonClicked) showSettings = !showSettings;
            
            if (showSettings)
            {
                GUI.backgroundColor = color_settingsWindow;
                drawSettingsWindow(gaugeRect);
            }

            GUI.backgroundColor = lastBackColor;

            GUI.DragWindow();

            if (windowPosition.x != lastPosition.x || windowPosition.y != lastPosition.y)
            {
                lastPosition.x = windowPosition.x;
                lastPosition.y = windowPosition.y;
                saveWindowPosition();
            }
        }

        private float scaleExponentially(float value, float exponent)
        {
            return (float)Math.Pow(Math.Abs(value), exponent) * Math.Sign(value);
        }

        private void drawSettingsWindow(Rect gaugeRect)
        {
            GUILayout.BeginArea(new Rect(0, gaugeRect.height+1, gaugeRect.width, settingsWindowHeight), settingsAreaStyle);

            bool last = useCDI;
            useCDI = GUILayout.Toggle(useCDI, "Display CDI Lines");
            if (useCDI != last) saveConfigSettings();
            GUILayout.BeginHorizontal();
            last = drawRollDigits;
            drawRollDigits = GUILayout.Toggle(drawRollDigits, "Display Roll Degrees");
            if (drawRollDigits != last) saveConfigSettings();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void drawGaugeText(Rect gaugeRect)
        {
            float dstXpos = 96 * scale;
            float cvelXpos = 307 * scale;
            float yPos = 334 * scale;

            GUI.Label(new Rect(dstXpos, yPos, 100f, 50f), distanceToTarget.ToString("F1"), labelStyle);
            GUI.Label(new Rect(cvelXpos, yPos, 100f, 30f), closureV.ToString("F"), labelStyle);

            if (drawRollDigits)
            {
                GUI.DrawTexture(new Rect(271*scale , 47 * scale, roll_label.width * scale, roll_label.height * scale), roll_label);
                
                // TODO position of roll digits doesn't scale correctly
                GUI.Label(new Rect(gaugeRect.width - (200 * scale), 40 * scale, 100f, 30f), orientationDeviation.z.ToString("F1"), rightAlignedStyle);
            }
        }

        private void drawVelocityVector(Rect gaugeRect)
        {
            float gaugeX, gaugeY;
            
            //Range-wrapping from old indicator behavior (which incorporated closure velocity component)
            //float mirror = 1f;
            //if (Math.Abs(relativeVelocity.x) <= 90) mirror = -1f;
            //gaugeX = mirror * wrapRange(relativeVelocity.x / 90f);
            //gaugeY = mirror * wrapRange(relativeVelocity.y / 90f);

            gaugeX = UnityEngine.Mathf.Clamp(transverseVelocity.x, -transverseVelocityRange, transverseVelocityRange) / transverseVelocityRange;
            gaugeY = UnityEngine.Mathf.Clamp(transverseVelocity.y, -transverseVelocityRange, transverseVelocityRange) / transverseVelocityRange;

            Texture2D velocityVectorTexture = prograde;
            if (Math.Abs(orientationDeviation.x) > 90f){
                gaugeX *= -1;
                gaugeY *= -1;
                velocityVectorTexture = retrograde;
            }
            
            gaugeX = scaleExponentially(gaugeX, velocityVectorExponent);
            gaugeY = scaleExponentially(gaugeY, velocityVectorExponent);

            float scaledVelocityVectorSize = velocityVectorIconSize * scale;
            float scaledVelocityVectorHalfSize = scaledVelocityVectorSize * .5f;

            GUI.DrawTexture(new Rect(.5f * gaugeRect.width * (1 + gaugeX * visiblePortion) - scaledVelocityVectorHalfSize,
                                        .5f * gaugeRect.height * (1 + gaugeY * visiblePortion) - scaledVelocityVectorHalfSize,
                                        scaledVelocityVectorSize,
                                        scaledVelocityVectorSize),
                                        velocityVectorTexture);
        }

        private float wrapRange(float a)
        {
            return ((((a + 1f) % 2) + 2) % 2) - 1f;
        }

        private void drawCDI(Rect gaugeRect)
        {

            Color colorCDI = colorCDINormal;
            if (negativeOnBackHemisphere < 0) colorCDI = colorCDIReverse;

            float gaugeX = negativeOnBackHemisphere * wrapRange(translationDeviation.x / 90f);
            float gaugeY = negativeOnBackHemisphere * wrapRange(translationDeviation.y / 90f);
            
            float exponent = CDIExponent;

            if (distanceToTarget <= CDIexponentDecreaseDoneRange) exponent = 1f;
            else if (distanceToTarget < CDIexponentDecreaseBeginRange)
            {
                float toGo = distanceToTarget - CDIexponentDecreaseDoneRange;
                float range = CDIexponentDecreaseBeginRange - CDIexponentDecreaseDoneRange;
                float lerp = toGo / range;

                float exponentReduction = 1f - CDIExponent;

                //this gradually eliminates the exponential scaling, avoiding over-sensitive CDI lines
                exponent = 1 - (exponentReduction) * lerp;
            }

            gaugeX = scaleExponentially(gaugeX, exponent);
            gaugeY = scaleExponentially(gaugeY, exponent);

            float xVal, yVal;

            xVal = .5f * gaugeRect.width * (gaugeX * visiblePortion + 1);
            yVal = .5f * gaugeRect.height * (gaugeY * visiblePortion + 1);

            Drawing.DrawVerticalLine(xVal, 0, gaugeRect.height, 2f, colorCDI);
            Drawing.DrawHorizontalLine(0, yVal, gaugeRect.width, 2f, colorCDI);
        }

        private void OnDrawDebug()
        {
            ownshipLine.transform.SetParent(FlightGlobals.ActiveVessel.ReferenceTransform);
            ownshipLine.transform.position = FlightGlobals.ActiveVessel.ReferenceTransform.position;
            ownshipLine.enabled = true;

            if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)
            {
                debugWindowPosition = GUILayout.Window(1338, debugWindowPosition, OnDebugWindow, "Debug", windowStyle);

                targetLine.enabled = true;
                ModuleDockingNode targetDockingPort = FlightGlobals.fetch.VesselTarget as ModuleDockingNode;
                targetLine.transform.SetParent(targetDockingPort.transform);
                targetLine.transform.position = FlightGlobals.ActiveVessel.ReferenceTransform.position;
            }
            else
            {
                targetLine.enabled = false;
            }
        }

        private void OnDebugWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("AngleX: ", labelStyle);
            GUILayout.Label(orientationDeviation.x.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("AngleY: ", labelStyle);
            GUILayout.Label(orientationDeviation.y.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("AngleZ: ", labelStyle);
            GUILayout.Label(orientationDeviation.z.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("DeviationX: ", labelStyle);
            GUILayout.Label(translationDeviation.x.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("DeviationY: ", labelStyle);
            GUILayout.Label(translationDeviation.y.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TransverseVelX: ", labelStyle);
            GUILayout.Label(transverseVelocity.x.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("TransverseVelY: ", labelStyle);
            GUILayout.Label(transverseVelocity.y.ToString(), labelStyle);
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Gauge Scale Exponent ", labelStyle);
            //gaugeScaleExponent = float.Parse(GUILayout.TextField(gaugeScaleExponent.ToString()));
            //GUILayout.Label("(0,1]", labelStyle);
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Gauge Alignment Range ", labelStyle);
            //gaugeRange = float.Parse(GUILayout.TextField(gaugeRange.ToString()));
            //GUILayout.Label("(1-90 deg)",labelStyle);
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("", labelStyle);
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Scale", labelStyle);
            //scale = float.Parse(GUILayout.TextField(scale.ToString()));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("VelVExpo", labelStyle);
            //velocityVectorExponent = float.Parse(GUILayout.TextField(velocityVectorExponent.ToString()));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("AlignExpo", labelStyle);
            //alignmentExponent = float.Parse(GUILayout.TextField(alignmentExponent.ToString()));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("CDIExpo", labelStyle);
            //CDIExponent = float.Parse(GUILayout.TextField(CDIExponent.ToString()));
            //GUILayout.EndHorizontal();
            //GUI.DragWindow();
        }


        //return signed angle in relation to normal's 2d plane
        private float AngleAroundNormal(Vector3 a, Vector3 b, Vector3 up)
        {
            return AngleSigned(Vector3.Cross(up, a), Vector3.Cross(up, b), up);
        }

        //-180 to 180 angle
        private float AngleSigned(Vector3 v1, Vector3 v2, Vector3 up)
        {
            if (Vector3.Dot(Vector3.Cross(v1, v2), up) < 0) //greater than 90 i.e v1 left of v2
                return -Vector3.Angle(v1, v2);
            return Vector3.Angle(v1, v2);
        }


        private static void saveWindowPosition()
        {
            config.SetValue("window_position", windowPosition);
            config.save();
        }

        private static void saveConfigSettings()
        {
            config.SetValue("show_cdi", useCDI);
            config.SetValue("show_rolldigits", drawRollDigits);
            config.save();
        }

        public static void LoadPrefs()
        {
            config = PluginConfiguration.CreateForType<DockingPortAlignment>(null);
            config.load();

            Rect defaultWindow = new Rect(Screen.width * .75f - (gaugeWidth * scale / 2f), Screen.height * .5f - (gaugeHeight * scale / 2f), gaugeWidth * scale, gaugeHeight * scale);
            windowPosition = config.GetValue<Rect>("window_position Position", defaultWindow);

            useCDI = config.GetValue<bool>("show_cdi", true);
            drawRollDigits = config.GetValue("show_rolldigits", false);

            saveWindowPosition();
            saveConfigSettings();
        }

        private void initStyles()
        {
            Color lightGrey = new Color(.8f, .8f, .85f); 

            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.stretchWidth = true;
            windowStyle.stretchHeight = true;

            labelStyle = new GUIStyle(HighLogic.Skin.label);
            labelStyle.stretchWidth = true;
            labelStyle.stretchHeight = true;
            labelStyle.normal.textColor = lightGrey;

            gaugeStyle = new GUIStyle(HighLogic.Skin.label);
            gaugeStyle.stretchWidth = true;
            gaugeStyle.normal.textColor = lightGrey;

            rightAlignedStyle = new GUIStyle(HighLogic.Skin.label);
            rightAlignedStyle.stretchWidth = true;
            rightAlignedStyle.normal.textColor = lightGrey;
            rightAlignedStyle.alignment = TextAnchor.UpperRight;

            settingsButtonStyel = new GUIStyle(HighLogic.Skin.button);
            settingsButtonStyel.padding = new RectOffset(1, 1, 1, 1);
            settingsButtonStyel.stretchHeight = true;
            settingsButtonStyel.stretchWidth = true;
            settingsButtonStyel.fontSize = 12;
            settingsButtonStyel.normal.textColor = lightGrey;

            settingsAreaStyle = new GUIStyle(HighLogic.Skin.window);
            settingsAreaStyle.padding = new RectOffset(5,5,5,5);

            hasInitializedStyles = true;
        }
    }
}