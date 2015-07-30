﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EVAEnhancements
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EVAEnhancementsBehaviour : MonoBehaviour
    {
        internal static GameObject navBall = null;
        internal static NavBall ball = null;

        public void Update()
        {
            if (navBall == null) {
                // Get a pointer to the navball
                navBall = GameObject.Find("NavBall");
                ball = navBall.GetComponent<NavBall>();
            }
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                // Change rotation offset so it points in the direction the Kerbal is facing
                ball.rotationOffset = new Vector3(0, 0, 0);
            }
            else
            {
                ball.rotationOffset = new Vector3(90f, 0, 0);
            }

        }
    }


    public class EVAEnhancements : PartModule
    {
        [KSPField(guiActive = true, guiName = "Profession", isPersistant = true)]
        public string kerbalProfession = null;

        [KSPField(guiName = "Jetpack", guiFormat = "P0", guiActive = true, isPersistant = true), UI_FloatRange(minValue = 0.01f, maxValue = 1f, stepIncrement = 0.05f)]
        public float jetPackPower = 1f;
        internal float currentPower = 1f;

        [KSPField(guiName = "AutoRotate", guiActive = true, isPersistant = true), UI_Toggle(disabledText = "No", enabledText = "Yes")]
        public bool rotateOnMove = false;

        public bool precisionControls = false;

        // Define settings file
        internal Settings settings = new Settings("EVAEnhancements.cfg");

        // Variables to keep track of original values
        internal float origLinPower = 0f;
        internal float origRotPower = 0f;
        internal float origPropConsumption = 0f;

        // Pointers to various objects
        internal KerbalEVA eva = null;
        internal ScreenSafeUISlideTab navBallTab;

        // Flags
        internal bool first = true;
        internal bool expandUI = true;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            // Load settings
            settings.Load();
            settings.Save();

            // Display profession and level
            ProtoCrewMember myKerbal = this.part.protoModuleCrew.SingleOrDefault();
            this.Fields["kerbalProfession"].guiName = myKerbal.experienceTrait.Title;
            kerbalProfession = "Level " + myKerbal.experienceLevel.ToString();

            

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Only run if the EVA'd Kerbal is the active vessel
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // Toggle precision node
                if (GameSettings.PRECISION_CTRL.GetKeyDown())
                {
                    precisionControls = !precisionControls;
                    if (precisionControls)
                    {
                        ScreenMessages.PostScreenMessage("Precision Controls: Enabled", 2f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Precision Controls: Disabled", 2f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }

                // Toggle Rotate on Move
                if (GameSettings.SAS_TOGGLE.GetKeyDown())
                {
                    rotateOnMove = !rotateOnMove;
                }

                // Set pointer to KerbalEVA
                if (eva == null)
                {
                    eva = FlightGlobals.ActiveVessel.GetComponent<KerbalEVA>();
                }


                if (eva.JetpackDeployed)
                {
                    if (first)
                    {
                        // Grab original values
                        origLinPower = eva.linPower;
                        origRotPower = eva.rotPower;
                        origPropConsumption = eva.PropellantConsumption;
                        first = false;
                    }

                    // Make sure this is set properly
                    GameSettings.EVA_ROTATE_ON_MOVE = rotateOnMove;

                    // Determine current jetpack power
                    
                    if (precisionControls)
                    {
                        currentPower = settings.precisionModePower;
                    } else {
                        currentPower = jetPackPower;
                    }

                    // Set the jetpack power and fuel consumption
                    eva.linPower = origLinPower * currentPower;
                    eva.rotPower = origRotPower * currentPower;
                    eva.PropellantConsumption = origPropConsumption * currentPower;

                    // Detect key presses
                    if (Input.GetKey(settings.pitchDown))
                    {
                        EVAController.Instance.UpdateEVAFlightProperties(-1, 0, currentPower);

                    }
                    if (Input.GetKey(settings.pitchUp))
                    {
                        EVAController.Instance.UpdateEVAFlightProperties(1, 0, currentPower);
                    }
                    if (Input.GetKey(settings.rollLeft))
                    {
                        EVAController.Instance.UpdateEVAFlightProperties(0, -1, currentPower);
                    }
                    if (Input.GetKey(settings.rollRight))
                    {
                        EVAController.Instance.UpdateEVAFlightProperties(0, 1, currentPower);
                    }

                }

            }
            
        }

        internal void LateUpdate()
        {
            // Only process is this is current vessel and the eva pointer was set previously
            if (this.vessel == FlightGlobals.ActiveVessel && eva != null)
            {
                

                // Display the navball
                if (expandUI)
                {
                    foreach (ScreenSafeUISlideTab tab in FlightEVA.fetch.EVACollapseGroups)
                    {
                        if (tab.name == "EVACollapse_navball")
                            navBallTab = tab;
                    }
                    expandUI = false;
                }
                navBallTab.Expand();


                if (eva.JetpackDeployed)
                {
                    // Set throttle to current jetpack power
                    FlightUIController.fetch.thr.setValue(currentPower);

                    // Turn on RCS light for jetpack
                    FlightUIController.fetch.rcs.renderer.material.mainTexture = FlightUIController.fetch.rcs.ledColors[1];

                    // Turn on SAS light for "EVA Rotate on Move"
                    if (GameSettings.EVA_ROTATE_ON_MOVE)
                    {
                        FlightUIController.fetch.SAS.renderer.material.mainTexture = FlightUIController.fetch.SAS.ledColors[1];
                    }
                    else
                    {
                        FlightUIController.fetch.SAS.renderer.material.mainTexture = FlightUIController.fetch.SAS.ledColors[0];
                    }

                }
                else
                {
                    // Set throttle to 0 and turn off lights
                    FlightUIController.fetch.thr.setValue(0f);
                    FlightUIController.fetch.rcs.renderer.material.mainTexture = FlightUIController.fetch.rcs.ledColors[0];
                    FlightUIController.fetch.SAS.renderer.material.mainTexture = FlightUIController.fetch.SAS.ledColors[0];
                }


            }

        }

    }
}