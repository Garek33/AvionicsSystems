﻿/*****************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016 MOARdV
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 ****************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace AvionicsSystems
{
    /// <summary>
    /// The MASVesselComputer encompasses all of the per-vessel data tracking
    /// used in Avionics Systems.  As such, it's entirely concerned with keeping
    /// tabs on data, but not much else.
    /// </summary>
    internal class MASVesselComputer : VesselModule
    {
        /// <summary>
        /// We use this dictionary to quickly fetch the vessel module for a
        /// given vessel, so we don't have to repeatedly call GetComponent<Vessel>().
        /// </summary>
        private static Dictionary<Guid, MASVesselComputer> knownModules = new Dictionary<Guid, MASVesselComputer>();

        /// <summary>
        /// The Vessel that we're attached to.  This is expected not to change.
        /// </summary>
        private Vessel vessel;

        /// <summary>
        /// A copy of the module's vessel ID, in case vessel is null'd before OnDestroy fires.
        /// </summary>
        private Guid vesselId;

        /// <summary>
        /// Whether the vessel needs MASVC support (has at least one crew).
        /// </summary>
        private bool vesselActive;

        /// <summary>
        /// Returns the MASVesselComputer attached to the specified computer.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static MASVesselComputer Instance(Vessel v)
        {
            if (v != null)
            {
                return Instance(v.id);
            }
            else
            {
                Utility.LogErrorMessage("ASVesselComputer.Instance called with null vessel");
                return null;
            }
        }

        /// <summary>
        /// Return the vessel computer associated with the specified id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static MASVesselComputer Instance(Guid id)
        {
            if (knownModules.ContainsKey(id))
            {
                return knownModules[id];
            }
            else
            {
                Utility.LogErrorMessage("ASVesselComputer.Instance called with unrecognized vessel id {0}", id);
                return null;
            }
        }

        #region Monobehaviour
        /// <summary>
        /// Update per-Vessel fields.
        /// </summary>
        private void FixedUpdate()
        {
            if (vesselActive)
            {
                //Utility.LogMessage(this, "FixedUpdate for {0}", vessel.id);
            }
        }

        /// <summary>
        /// Startup: see if we're attached to a vessel, and if that vessel is
        /// one we should update (has crew).  Since the latter can change, we
        /// will idle this object (ignore updates) if the crew count is 0.
        /// </summary>
        public override void OnAwake()
        {
            vessel = GetComponent<Vessel>();

            if (vessel == null)
            {
                Utility.LogErrorMessage(this, "OnAwake: Failed to get a valid vessel");
                return;
            }

            vesselId = vessel.id;

            if (knownModules.ContainsKey(vesselId))
            {
                Utility.LogErrorMessage(this, "OnAwake called on a instance that's already in the database");
            }

            knownModules[vesselId] = this;

            vesselActive = (vessel.GetCrewCount() > 0);

            Utility.LogMessage(this, "OnAwake for {0}", vesselId);
        }

        /// <summary>
        /// This vessel is being scrapped.  Release modules.
        /// </summary>
        private void OnDestroy()
        {
            if (vesselId == Guid.Empty)
            {
                return; // early - we never configured.
            }

            Utility.LogMessage(this, "OnDestroy for {0}", vesselId);

            knownModules.Remove(vesselId);

            vesselId = Guid.Empty;
            vessel = null;
        }

        /// <summary>
        /// Load persistent variable data from the persistent files, and
        /// distribute that data to the MASFlightComputer modules.
        /// </summary>
        /// <param name="node"></param>
        public override void OnLoad(ConfigNode node)
        {
            if (vesselActive)
            {
                base.OnLoad(node);
                Utility.LogMessage(this, "OnLoad for {0}", vessel.id);

                List<MASFlightComputer> knownFc = new List<MASFlightComputer>();
                for (int partIdx = vessel.parts.Count - 1; partIdx >= 0; --partIdx)
                {
                    MASFlightComputer fc = MASFlightComputer.Instance(vessel.parts[partIdx]);
                    if (fc != null)
                    {
                        knownFc.Add(fc);
                    }
                }

                ConfigNode[] persistentNodes = node.GetNodes();
                Utility.LogMessage(this, "Found {0} child nodes and {1} fc", persistentNodes.Length, knownFc.Count);
                
                // Yes, this is a horribly inefficient nested loop.  Except that
                // it should be uncommon to have more than a small number of pods
                // in most configurations.
                for(int nodeIdx=persistentNodes.Length-1; nodeIdx>=0; --nodeIdx)
                {
                    for(int fcIdx = knownFc.Count-1; fcIdx >=0; --fcIdx)
                    {
                        if(knownFc[fcIdx] != null)
                        {
                            if (knownFc[fcIdx].LoadPersistents(persistentNodes[nodeIdx]))
                            {
                                knownFc[fcIdx] = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save this vessel's ASFlightComputer persistent vars.
        /// </summary>
        /// <param name="node"></param>
        public override void OnSave(ConfigNode node)
        {
            if (vesselActive)
            {
                base.OnSave(node);
                Utility.LogMessage(this, "OnSave for {0}", vessel.id);

                for (int partIdx = vessel.parts.Count-1; partIdx >=0 ; --partIdx)
                {
                    MASFlightComputer fc = MASFlightComputer.Instance(vessel.parts[partIdx]);
                    if (fc != null)
                    {
                        ConfigNode saveNode = fc.SavePersistents();
                        if(saveNode != null)
                        {
                            node.AddNode(saveNode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialize our state.
        /// </summary>
        private void Start()
        {
            if (vesselActive)
            {
                Utility.LogMessage(this, "Start for {0}", vessel.id);
            }
        }

        /// <summary>
        /// ???
        /// </summary>
        //private void Update()
        //{
        //    if (vesselActive)
        //    {
        //        Utility.LogMessage(this, "Update for {0}", vessel.id);
        //    }
        //}

        #endregion

        #region GameEvent Callbacks
        private void onVesselChange(Vessel who)
        {
            if (who.id == vesselId)
            {
                vesselActive = (vessel.GetCrewCount() > 0);
            }
        }

        private void onVesselWasModified(Vessel who)
        {
            if (who.id == vesselId)
            {
                vesselActive = (vessel.GetCrewCount() > 0);
            }
        }

        private void onVesselDestroy(Vessel who)
        {
            if (who.id == vesselId)
            {
                vesselActive = (vessel.GetCrewCount() > 0);
            }
        }

        private void onVesselCreate(Vessel who)
        {
            if (who.id == vesselId)
            {
                vesselActive = (vessel.GetCrewCount() > 0);
            }
        }

        private void onVesselCrewWasModified(Vessel who)
        {
            if (who.id == vessel.id)
            {
                vesselActive = (vessel.GetCrewCount() > 0);
            }
        }
        #endregion
    }
}